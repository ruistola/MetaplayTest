// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

using Akka.Actor;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Metaplay.Cloud;
using Metaplay.Cloud.Analytics;
using Metaplay.Cloud.Application;
using Metaplay.Cloud.Cluster;
using Metaplay.Cloud.Entity;
using Metaplay.Cloud.Metrics;
using Metaplay.Cloud.RuntimeOptions;
using Metaplay.Cloud.Services;
using Metaplay.Cloud.Services.Geolocation;
using Metaplay.Cloud.Utility;
using Metaplay.Core;
using Metaplay.Core.Config;
using Metaplay.Core.Serialization;
using Metaplay.Server.Database;
using Metaplay.Server.EntityArchive;
using Metaplay.Server.Forms;
using Metaplay.Server.GameConfig;
using Metaplay.Server.MaintenanceJob;
using Metaplay.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Metaplay.Server
{
    public abstract class ServerMainBase : Application
    {
        // Node-global actors
        IActorRef   _actorMessageCollector;
        IActorRef   _analyticsDispatcher;

        protected ServerMainBase()
        {
            // \note These are rather opinionated changes and we might want to allow the application to override these.

            // Force worker threads to at least 2*numCPUs to see if that affects ThreadPool starvation
            ThreadPool.GetMinThreads(out int _workerThreads, out int completionPortThreads);
            ThreadPool.SetMinThreads(2 * Environment.ProcessorCount, completionPortThreads);
        }

        public async Task<int> RunServerAsync(string[] cmdLineArgs)
        {
            // Note: EFCore6 `dotnet ef migrations add` start the application with arguments: "--applicationName Server, ...",
            //       in which case, we exit immediately.
            if (cmdLineArgs.Length >= 1 && cmdLineArgs[0] == "--applicationName")
            {
                Console.WriteLine("Detected EFCore run due to: cmdLineArgs = '{0}', exiting..", string.Join(" ", cmdLineArgs));
                await RunDummyIHostAsync();
                return 0;
            }

            if (cmdLineArgs.Contains("--MetaplayValidateDatabaseModelChanges"))
            {
                return ValidateDatabaseModelChanges();
            }

            if (cmdLineArgs.Length > 0 && cmdLineArgs[0] == "--MetaplayValidateGameConfig")
            {
                // Throws on errors
                await ValidateGameConfig(cmdLineArgs[1]);
                return 0;
            }

            return await RunApplicationMainAsync(applicationSymbolicName: "Server", cmdLineArgs);
        }

        // Check there are no missing EFCore migrations for the current tables.
        // We do this by checking the Model Snapshot in the assembly and comparing it to the current model.
        // If the models have differences, it means the user hasn't run `dotnet ef migrations add`.
        static int ValidateDatabaseModelChanges()
        {
            MetaDbContext context = IntegrationRegistry.Create<MetaDbContext>();

            IMigrationsModelDiffer modelDiffer = context.GetService<IMigrationsModelDiffer>();
            IMigrationsAssembly migrationsAssembly = context.GetService<IMigrationsAssembly>();
            IModelRuntimeInitializer modelInitializer = context.GetService<IModelRuntimeInitializer>();
            IModel snapshotModel = migrationsAssembly.ModelSnapshot?.Model;
            if (snapshotModel is IMutableModel mutableModel)
                snapshotModel = mutableModel.FinalizeModel();
            if (snapshotModel is not null)
                snapshotModel = modelInitializer.Initialize(snapshotModel);
            IDesignTimeModel designTimeModel = context.GetService<IDesignTimeModel>();

            bool pendingModelChanges = modelDiffer.HasDifferences(
                snapshotModel?.GetRelationalModel(),
                designTimeModel.Model.GetRelationalModel());

            if (!pendingModelChanges)
                return 0;

            Console.WriteLine("The database model has changes that there are no migrations for, please add an EFCore migration!");
            Console.WriteLine("A migration can be added with 'dotnet ef migrations add MigrationName'.");
            return 1;
        }

        async Task ValidateGameConfig(string archivePath)
        {
            // Init serialization system
            Type serializerType = GenerateSerializer("Server");
            MetaSerialization.Initialize(serializerType, null);

            // Try to load GameConfig from archive
            byte[] archiveBytes = await FileUtil.ReadAllBytesAsync(archivePath);
            ConfigArchive archive = ConfigArchive.FromBytes(archiveBytes);
            GameConfigManager.TestStaticGameConfigArchive(archive);
        }

        async Task InitializeFirebasePushNotificationsAsync()
        {
            // Initialize FCM
            PushNotificationOptions pushOpts = RuntimeOptionsRegistry.Instance.GetCurrent<PushNotificationOptions>();
            if (pushOpts.Enabled)
            {
                // Resolve credentials (from Secrets Manager or file)
                _logger.Information("Initializing Firebase with credentials from {FirebaseCredentialsPath}", pushOpts.FirebaseCredentialsPath);
                string firebaseCredentials = await SecretUtil.ResolveSecretAsync(_logger, pushOpts.FirebaseCredentialsPath).ConfigureAwait(false);

                // Initialize Firebase
                FirebaseApp _ = FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromJson(firebaseCredentials)
                });
            }
        }

        async Task InitializePublicBlobStoreConnectivityTests()
        {
            try
            {
                using (IBlobStorage storage = RuntimeOptionsRegistry.Instance.GetCurrent<BlobStorageOptions>().CreatePublicBlobStorage("Connectivity"))
                {
                    await storage.PutAsync("connectivity-test", Encoding.UTF8.GetBytes("y"));
                }
            }
            catch (Exception ex)
            {
                // Failure is not critical, tolerate.
                _logger.Error("Failed while writing connectivity-test file: {Exception}", ex);
            }
        }

        protected override sealed async Task StartCoreServicesAsync()
        {
            // Initialize GameConfig providers
            ServerConfigDataProvider.Initialize();

            // Initialize database
            MetaDatabaseBase.Initialize();

            // Figure out if we're the cluster leader
            ClusteringOptions clusterOpts = RuntimeOptionsRegistry.Instance.GetCurrent<ClusteringOptions>();
            bool isLeader = clusterOpts.IsCurrentNodeClusterLeader;
            _logger.Information("ClusterRole = {Role}", isLeader ? "Leader" : "Follower");

            // Ensure database is migrated to latest schema (only on service pod)
            if (isLeader)
            {
                DatabaseMigrator migrator = new DatabaseMigrator();
                await migrator.EnsureMigratedAsync();
            }

            // Initialize Firebase push notifications
            await InitializeFirebasePushNotificationsAsync().ConfigureAwait(false);

            // Start Akka.net actor message metrics collector
            _actorMessageCollector = _actorSystem.ActorOf(Props.Create<ActorMessageCollector>(), "actor-message-collector");

            // Start analytics dispatcher (with enabled sinks)
            IEnumerable<AnalyticsDispatcherSinkBase> analyticsSinks = await IntegrationRegistry.Get<AnalyticsDispatcherSinkFactory>().CreateSinksAsync();
            _analyticsDispatcher = _actorSystem.ActorOf(Props.Create<AnalyticsDispatcherActor>(analyticsSinks), "analytics-dispatcher");

            // Start Google sign-in keycache autoupdater
            GooglePlayStoreOptions playStoreOpts = RuntimeOptionsRegistry.Instance.GetCurrent<GooglePlayStoreOptions>();
            if (playStoreOpts.EnableGoogleAuthentication)
                GoogleOAuth2PublicKeyCache.Instance.RenewAutomatically();

            // Start Apple sign-in keycache autoupdater
            AppleStoreOptions appStoreOpts = RuntimeOptionsRegistry.Instance.GetCurrent<AppleStoreOptions>();
            if (appStoreOpts.EnableAppleAuthentication)
                AppleSignInPublicKeyCache.Instance.RenewAutomatically();

            // Fetch Facebook login access token and keycache autoupdater
            FacebookOptions facebookOptions = RuntimeOptionsRegistry.Instance.GetCurrent<FacebookOptions>();
            if (facebookOptions.LoginEnabled)
            {
                FacebookAppService.Instance.PrefetchAppAccessToken();
                FacebookLoginPublicKeyCache.Instance.RenewAutomatically();
            }

            // Start AndroidPublisherService instance
            if (playStoreOpts.EnableAndroidPublisherApi)
                AndroidPublisherServiceSingleton.Initialize();

            // Initialize geolocation.
            // Loads initial geolocation database if available, and starts database auto-updater.
            await Geolocation.InitializeAsync(
                replicaBlobStorage: RuntimeOptionsRegistry.Instance.GetCurrent<BlobStorageOptions>().CreatePrivateBlobStorage("Geolocation"),
                isLeader:           isLeader)
                .ConfigureAwait(false);

            // Prepare Public BlobStore
            if (isLeader)
            {
                await InitializePublicBlobStoreConnectivityTests();
            }

            // On leader node, periodically purge old PlayerIncidents (doing this fairly frequently to avoid purging huge swathes of reports at a time)
            // \todo [petri] refactor this to a better place, wrap inside an actor?
            if (isLeader)
            {
                _ = Task.Run(async () =>
                {
                    while (true)
                    {
                        // When DB goes to null, the local service is dead
                        SystemOptions systemOpts = RuntimeOptionsRegistry.Instance.GetCurrent<SystemOptions>();
                        MetaDatabase db = MetaDatabase.Get(QueryPriority.Lowest);
                        if (db == null)
                        {
                            _logger.Information("Player incidents clean-up sweeper stopping.");
                            return;
                        }

                        // Purge reports older than retention period
                        try
                        {
                            Stopwatch sw = Stopwatch.StartNew();
                            DateTime removeUntil = DateTime.UtcNow - systemOpts.IncidentReportRetentionPeriod;
                            int numPurged = await db.PurgePlayerIncidentsAsync(removeUntil, systemOpts.IncidentReportPurgeMaxItems).ConfigureAwait(false);
                            if (numPurged > 0)
                                _logger.Information("Purged {NumPurgedReports} player incident reports ({Duration:0.00}s elapsed)", numPurged, sw.ElapsedMilliseconds / 1000.0);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error("Failed to purge PlayerIncidents: {Exception}", ex);
                        }

                        // Purge audit log events older than retention period
                        try
                        {
                            MetaDuration retentionPeriod = MetaDuration.FromTimeSpan(systemOpts.AuditLogRetentionPeriod);
                            int numPurged = await db.PurgeAuditLogEventsAsync(MetaTime.Now - retentionPeriod).ConfigureAwait(false);
                            if (numPurged > 0)
                                _logger.Information("Purged {NumPurgedEvents} audit log events", numPurged);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error("Failed to purge AuditLogEvents: {Exception}", ex);
                        }

                        // Wait a while until purging again
                        await Task.Delay(TimeSpan.FromMinutes(1)).ConfigureAwait(false);
                    }
                });
            }
        }

        protected override sealed async Task StopCoreServicesAsync()
        {
            await _analyticsDispatcher.Ask<ShutdownComplete>(ShutdownSync.Instance, TimeSpan.FromSeconds(60));
            _actorMessageCollector.Tell(PoisonPill.Instance);
        }

        protected override Task PreInitializeAsync()
        {
            // Setup logged error tracking
            MetaLogger.LogEventLogged += StatsCollectorProxy.IncrementLoggedErrorCount;

            // Initialize Maintenance Jobs
            EntityMaintenanceJobRegistry.Initialize();

            // Initialize EntityArchiveRegistry
            EntityArchiveUtils.Initialize();

            // Initialize MetaFormTypeRegistry
            MetaFormTypeRegistry.Initialize();

            return Task.CompletedTask;
        }
    }
}
