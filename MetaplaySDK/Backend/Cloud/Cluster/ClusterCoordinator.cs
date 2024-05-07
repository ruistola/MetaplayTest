// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

using Akka.Actor;
using Akka.Remote;
using Metaplay.Cloud.Entity;
using Metaplay.Cloud.Sharding;
using Metaplay.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static System.FormattableString;

namespace Metaplay.Cloud.Cluster
{
    /// <summary>
    /// Current lifecycle phase that the cluster is in.
    /// </summary>
    /// <remarks>The status only goes in one direction: Connecting -> Starting -> Running -> Stopping -> Terminated</remarks>
    public enum ClusterPhase
    {
        Connecting,     // Cluster is waiting for all nodes to report in
        Starting,       // Cluster entity/service shards are being started, new nodes can join instantly
        Running,        // Cluster is in a running state, shards on new nodes don't need to wait
        Stopping,       // Cluster services are being stopped
        Terminated,     // All cluster services have been stopped, ready to exit
    }

    /// <summary>
    /// Lifecycle phase of an <see cref="EntityShardGroup"/>. Used within the cluster to synchronize
    /// initialization and shutdown sequences.
    /// </summary>
    public enum EntityGroupPhase
    {
        Pending = 0,    // EntityShardGroup has not been created yet.
        Created,        // All EntityShards in the group have been created but no entities have been spawned.
        Running,        // All EntityShards in the group are up and running, with entities initialized.
        Stopped,        // All the EntityShards in the group have been stopped (entities should no longer exist).
        Terminated,     // All the EntityShards in the group have been terminated.
    }

    /// <summary>
    /// Inform cluster peers about our current cluster and service states.
    /// </summary>
    public class ClusterUpdateNodeState
    {
        public ClusterPhase         LocalPhase          { get; private set; }   // Lifecycle phase of the specific cluster member
        public ClusterPhase         TargetPhase         { get; private set; }   // Lifecycle phase of whole cluster (can only progress forward)
        public EntityGroupPhase[]   EntityGroupPhases   { get; private set; }   // Phase of the EntityGroups
        public string               Cookie              { get; private set; }   // Optional cluster cookie (only allow connections if cookies match), null means ignore

        public ClusterUpdateNodeState(ClusterPhase localPhase, ClusterPhase targetPhase, EntityGroupPhase[] entityGroupPhases, string cookie)
        {
            LocalPhase          = localPhase;
            TargetPhase         = targetPhase;
            EntityGroupPhases   = entityGroupPhases;
            Cookie              = cookie;
        }
    }

    /// <summary>
    /// Locally published event to inform other actors about cluster status changes.
    /// </summary>
    public class ClusterPhaseUpdateEvent
    {
        public readonly ClusterPhase Phase;

        public ClusterPhaseUpdateEvent(ClusterPhase status) => Phase = status;
    }

    /// <summary>
    /// Locally published event to inform other actors about lost cluster members.
    /// </summary>
    public class ClusterNodeLostEvent
    {
        public readonly ClusterNodeAddress Address;

        public ClusterNodeLostEvent(ClusterNodeAddress address) => Address = address;
    }

    /// <summary>
    /// Coordinates cluster start/shutdown sequence.
    /// </summary>
    public class ClusterCoordinatorActor : MetaReceiveActor
    {
        public class TryProgress
        {
            public static TryProgress Instance { get; } = new TryProgress();
        }

        static Prometheus.Gauge c_clusterPhase          = Prometheus.Metrics.CreateGauge("metaplay_cluster_phase", "Metaplay cluster phase", "phase");
        static Prometheus.Gauge c_clusterExpectedNodes  = Prometheus.Metrics.CreateGauge("metaplay_cluster_expected_nodes_current", "Number of expected nodes in the cluster");
        static Prometheus.Gauge c_clusterNodesConnected = Prometheus.Metrics.CreateGauge("metaplay_cluster_connected_nodes_current", "Number of connected-to nodes in the cluster");

        const string CoordinatorName = "cluster";

        readonly string                 _actorSystemName;
        readonly ClusterNodeAddress     _selfAddress;
        readonly ClusterState           _clusterState;
        readonly string                 _cookie;
        readonly EntitySharding         _entitySharding;
        readonly RuntimeNodeState       _selfState;
        ClusterPhase                    _clusterTargetPhase         = ClusterPhase.Connecting;

        OrderedDictionary<EntityShardGroup, EntityConfigBase[]>  _localEntityConfigs;

        static ManualResetEventSlim     s_clusterShutdownRequested  = new ManualResetEventSlim(false);
        static IActorRef                s_instance                  = null;

        static volatile ClusterPhase    s_phase                     = ClusterPhase.Connecting;
        public static ClusterPhase      Phase                       => s_phase;

        static readonly TimeSpan        TickInterval                = TimeSpan.FromMilliseconds(5_000);
        ICancelable                     _cancelUpdateTimer          = new Cancelable(Context.System.Scheduler);

        class InitiateShutdownRequest { public static readonly InitiateShutdownRequest Instance = new InitiateShutdownRequest(); }

        public class ClusterStateRequest { public static ClusterStateRequest Instance => new ClusterStateRequest(); }
        public class ClusterStateResponse
        {
            public ClusterPhase ClusterPhase        { get; private set; }
            public int          NumTotalNodes       { get; private set; }
            public int          NumNodesConnected   { get; private set; }

            public ClusterStateResponse(ClusterPhase clusterPhase, int numTotalNodes, int numNodesConnected)
            {
                ClusterPhase        = clusterPhase;
                NumTotalNodes       = numTotalNodes;
                NumNodesConnected   = numNodesConnected;
            }
        }

        public ClusterCoordinatorActor(string actorSystemName, ClusterNodeAddress selfAddress, ClusterConfig config, string cookie, EntitySharding entitySharding)
        {
            _actorSystemName    = actorSystemName;
            _selfAddress        = selfAddress;
            _clusterState       = new ClusterState(config, selfAddress);
            _cookie             = cookie;
            _entitySharding     = entitySharding;

            // Initialize own state
            _selfState = _clusterState.NodeSets.SelectMany(nodeSet => nodeSet.NodeStates).Single(nodeState => nodeState.IsSelf);
            UpdateClusterMemberState(_selfState, s_phase, new EntityGroupPhase[(int)EntityShardGroup.Last]);

            // Resolve local EntityShards by EntityGroup
            _localEntityConfigs = new OrderedDictionary<EntityShardGroup, EntityConfigBase[]>();
            for (EntityShardGroup entityGroup = 0; entityGroup < EntityShardGroup.Last; entityGroup++)
            {
                List<EntityConfigBase> matching = new List<EntityConfigBase>();
                foreach (EntityConfigBase entityConfig in EntityConfigRegistry.Instance.TypeToEntityConfig.Values)
                {
                    if (entityConfig.EntityShardGroup == entityGroup)
                    {
                        bool isOwnedShard = _clusterState.Config.ResolveNodeShardIndex(entityConfig.EntityKind, _selfAddress, out int _);
                        if (isOwnedShard)
                            matching.Add(entityConfig);
                    }
                }
                _localEntityConfigs.Add(entityGroup, matching.ToArray());
            }

            RegisterHandlers();
        }

        public static bool IsReady()
        {
            return s_phase == ClusterPhase.Running;
        }

        public static void RequestClusterShutdown()
        {
            // Mark shutdown as requested (required in case actor is not yet up)
            s_clusterShutdownRequested.Set();

            // If instance is already up, send request to start shutdown sequence
            if (s_instance != null)
                s_instance.Tell(InitiateShutdownRequest.Instance);
        }

        public static async Task<ClusterStateResponse> RequestStatusAsync(IActorRef actor)
        {
            return await actor.Ask<ClusterStateResponse>(ClusterStateRequest.Instance);
        }

        public static async Task WaitForClusterConnectedAsync(IActorRef actor, IMetaLogger logger)
        {
            DateTime timeoutAt = DateTime.UtcNow + TimeSpan.FromMinutes(5);
            for (int iter = 0; ; iter++)
            {
                ClusterStateResponse state = await RequestStatusAsync(actor);
                if (state.ClusterPhase != ClusterPhase.Connecting)
                {
                    logger.Information("Cluster state={Status} with {NumNodesConnected}/{NumTotalNodes} nodes connected!", state.ClusterPhase, state.NumNodesConnected, state.NumTotalNodes);
                    return;
                }

                // Log progress every second & wait a bit
                if (iter % 100 == 0)
                    logger.Debug("Waiting for cluster nodes to connect ({NumNodesConnected}/{NumTotalNodes} connected)..", state.NumNodesConnected, state.NumTotalNodes);
                await Task.Delay(10);

                // Check for timeout
                if (DateTime.UtcNow >= timeoutAt)
                    throw new TimeoutException("Timeout while waiting for cluster to spin up, giving up!");
            }
        }

        void UpdateClusterTargetPhase(ClusterPhase newPhase)
        {
            // Use the latest state of any member in the cluster.
            _clusterTargetPhase = (ClusterPhase)Math.Max((int)_clusterTargetPhase, (int)newPhase);
        }

        void SetClusterPhase(ClusterPhase newPhase, string reason)
        {
            // If status changed, publish event
            ClusterPhase oldPhase = s_phase;
            if (newPhase != oldPhase)
            {
                _log.Information("Switching cluster phase from {OldPhase} to {NewPhase} (reason={Reason})", oldPhase, newPhase, reason);
                s_phase = newPhase;

                // Update cluster target phase
                UpdateClusterTargetPhase(newPhase);

                // Send notifications to listeners
                Context.System.EventStream.Publish(new ClusterPhaseUpdateEvent(newPhase));

                // Inform peers of phase changes
                SendStateToPeers();
            }
        }

        void SendStateToPeers()
        {
            //_log.Debug("Sending cluster state to peers: EntityGroups={EntityGroupPhases}", PrettyPrint.Compact(_selfState.EntityGroupPhases));

            // Send own state to connected peers
            foreach (RuntimeNodeSetState nodeSetState in _clusterState.NodeSets)
            {
                foreach (RuntimeNodeState nodeState in nodeSetState.NodeStates)
                {
                    if (!nodeState.IsSelf && nodeState.IsConnected)
                    {
                        //_log.Debug("Sending cluster state to peer {PeerAddress}", nodeState.Address);
                        SendStatusMessage(nodeState, includeCookie: false);
                    }
                }
            }

            // Try to make more progress locally: This method is called only from ProgressClusterStateAsync(). If we make progress,
            // we inform peers, i.e. end up here. And since we have made progress in ProgressClusterStateAsync(), we want to continue
            // and try progress again even further.
            _self.Tell(TryProgress.Instance);
        }

        void RegisterHandlers()
        {
            Receive<AssociatedEvent>(ReceiveAssociatedEvent);
            Receive<DisassociatedEvent>(ReceiveDisassociatedEvent);
            Receive<RemotingLifecycleEvent>(ReceiveRemotingLifecycleEvent);
            ReceiveAsync<ActorTick>(ReceiveActorTick);
            ReceiveAsync<TryProgress>(ReceiveTryProgress);
            ReceiveAsync<InitiateShutdownRequest>(ReceiveInitiateShutdownRequest);
            ReceiveAsync<ClusterUpdateNodeState>(ReceiveClusterUpdateNodeState);
            Receive<ClusterStateRequest>(ReceiveClusterStateRequest);
        }

        void ReceiveAssociatedEvent(AssociatedEvent associated)
        {
            // Try to immediately connect to remote node
            Address address = associated.RemoteAddress;
            _log.Information("Associated with {RemoteAddress}: {Event}", address, associated);
            RuntimeNodeState nodeState = _clusterState.GetNodeState(new ClusterNodeAddress(address.Host, address.Port.Value));
            TryConnectToPeer(nodeState);
        }

        void ReceiveDisassociatedEvent(DisassociatedEvent disassociated)
        {
            // Handle node disconnects
            _log.Warning("DisassociatedEvent for {RemoteAddress}: {Event}", disassociated.RemoteAddress, disassociated);
            ClusterNodeAddress clusterAddress = new ClusterNodeAddress(disassociated.RemoteAddress.Host, disassociated.RemoteAddress.Port.Value);
            RuntimeNodeState nodeState = _clusterState.GetNodeState(clusterAddress);
            if (nodeState.IsConnected)
            {
                // Mark node as disconnected
                nodeState.SetDisconnected();

                // Send cluster event
                Context.System.EventStream.Publish(new ClusterNodeLostEvent(clusterAddress));
            }

            if (nodeState.IsSelf)
                _log.Error("Received DisassociatedEvent for self node: clusterAddress={ClusterAddress}, remoteAddress={RemoteAddress}", clusterAddress, disassociated.RemoteAddress);
        }

        void ReceiveRemotingLifecycleEvent(RemotingLifecycleEvent lifecycle)
        {
            _log.Information("********** Lifecycle event {EventType}: {Event}", lifecycle.GetType().Name, lifecycle.ToString());
        }

        async Task ReceiveActorTick(ActorTick tick)
        {
            // Report metrics
            int numConnectedNodes = _clusterState.GetNumConnectedNodes();
            int numTotalNodes = _clusterState.GetNumTotalNodes();
            _log.Information("Tick: phase={Phase}, {NumConnectedNodes}/{NumTotalNodes} nodes connected", s_phase, numConnectedNodes, numTotalNodes);
            foreach (ClusterPhase phase in EnumUtil.GetValues<ClusterPhase>())
                c_clusterPhase.WithLabels(phase.ToString()).Set(s_phase == phase ? 1.0 : 0.0);
            c_clusterExpectedNodes.Set(numTotalNodes);
            c_clusterNodesConnected.Set(numConnectedNodes);

            // Update connections to peers (except if already terminated)
            if (s_phase != ClusterPhase.Terminated)
                TryConnectToAllPeers();

            // Try to progress cluster lifecycle
            await ProgressClusterStateAsync();
        }

        async Task ReceiveTryProgress(TryProgress _)
        {
            // Try to progress cluster lifecycle
            await ProgressClusterStateAsync();
        }

        async Task ReceiveInitiateShutdownRequest(InitiateShutdownRequest shutdown)
        {
            // Switch target phase to Terminated
            UpdateClusterTargetPhase(ClusterPhase.Terminated);

            // Start shutting down services
            await ProgressClusterStateAsync();
        }

        void UpdateClusterMemberState(RuntimeNodeState nodeState, ClusterPhase localPhase, EntityGroupPhase[] entityGroupPhases)
        {
            // Trigger any state updates based on state change
            for (EntityShardGroup entityGroup = 0; entityGroup < EntityShardGroup.Last; entityGroup++)
            {
                // Check if group has progressed
                EntityGroupPhase newPhase = entityGroupPhases[(int)entityGroup];
                EntityGroupPhase oldPhase = nodeState.EntityGroupPhases[(int)entityGroup];
                if (newPhase > oldPhase)
                {
                    //_log.Information("Progressing node {Address} EntityShardGroup {EntityShardGroup} from phase {OldPhase} to {NewPhase}", nodeState.Address, entityGroup, oldPhase, newPhase);

                    // If was created, resolve the ShardActor references
                    if (oldPhase == EntityGroupPhase.Pending)
                    {
                        _log.Information("Resolve EntityShard actors for EntityShardGroup {EntityGroup} on {Address}", entityGroup, nodeState.Address);
                        _entitySharding.ResolveEntityShardActorsForNode(_clusterState.Config, nodeState, entityGroup);
                    }

                    // If was terminated, forget the ShardActor references
                    if (newPhase >= EntityGroupPhase.Terminated)
                    {
                        _log.Information("Forget EntityShard actors for EntityShardGroup {EntityGroup} on {Address}", entityGroup, nodeState.Address);
                        _entitySharding.ClearEntityShardActorsForNode(_clusterState.Config, nodeState, entityGroup);
                    }
                }
            }

            nodeState.UpdateState(localPhase, entityGroupPhases);
        }

        async Task ReceiveClusterUpdateNodeState(ClusterUpdateNodeState peerState)
        {
            // Check if newly discovered node
            Address address = Sender.Path.Address;
            RuntimeNodeState nodeState = _clusterState.GetNodeState(new ClusterNodeAddress(address.Host, address.Port.Value));
            if (!nodeState.IsConnected)
            {
                _log.Information("New peer connected: {Node}, LocalPhase={LocalPhase}, TargetPhase={TargetPhase}, Cookie={Cookie}", Sender, peerState.LocalPhase, peerState.TargetPhase, peerState.Cookie);

                // Check for cookie match
                if (!string.IsNullOrEmpty(peerState.Cookie) && peerState.Cookie != _cookie)
                {
                    _log.Warning("Peer node {Node} connected with mismatched cookie (used {PeerCookie}, own {OwnCookie}), ignoring peer", Sender, peerState.Cookie, _cookie);
                    return;
                }
            }
            else
                _log.Information("Received peer update from {Node}: LocalPhase={LocalPhase}, TargetPhase={TargetPhase}, EntityGroupPhases={EntityGroupPhases}", Sender, peerState.LocalPhase, peerState.TargetPhase, string.Join(", ", peerState.EntityGroupPhases));

            // If they included cookie, it's a new connection, so respond immediately
            if (peerState.Cookie != null)
                SendStatusMessage(nodeState, includeCookie: false);

            // Store node state (also sets IsConnected)
            UpdateClusterMemberState(nodeState, peerState.LocalPhase, peerState.EntityGroupPhases);

            // Update cluster target phase
            UpdateClusterTargetPhase(peerState.TargetPhase);

            // Try to make lifecycle progress
            await ProgressClusterStateAsync();
        }

        void ReceiveClusterStateRequest(ClusterStateRequest req)
        {
            Sender.Tell(new ClusterStateResponse(s_phase, _clusterState.GetNumTotalNodes(), _clusterState.GetNumConnectedNodes()));
        }

        protected override void PreStart()
        {
            base.PreStart();

            // Store global instance
            if (s_instance != null)
                throw new InvalidOperationException($"Singleton instance of ClusterCoordinatorActor already registered!");
            s_instance = _self;

            // Start update timer
            Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(1), TickInterval, _self, ActorTick.Instance, ActorRefs.NoSender, _cancelUpdateTimer);

            // Subscribe to Akka.Remote Association events (Associated, Disassociated, AssociationError)
            Context.System.EventStream.Subscribe(_self, typeof(RemotingLifecycleEvent));

            // Register all EntityKinds
            // \todo [petri] #dynamiccluster State should be owned by us?
            _entitySharding.InitializeEntityShardStates(_clusterState.Config);
        }

        protected override void PostStop()
        {
            // Cancel updater
            _cancelUpdateTimer.Cancel();

            // Unsubscribe from Akka.Remote Association events (Associated, Disassociated, AssociationError)
            Context.System.EventStream.Unsubscribe(_self, typeof(RemotingLifecycleEvent));

            // Clear global instance
            s_instance = null;

            base.PostStop();
            _log.Information("Cluster coordinator stopped");
        }

        // \todo [petri] resolve ActorRef ?
        public ActorSelection GetRemoteCoordinatorActorSelection(ClusterNodeAddress address) =>
            Context.System.ActorSelection(Invariant($"akka.tcp://{_actorSystemName}@{address.HostName}:{address.Port}/user/{CoordinatorName}"));

        void SendStatusMessage(RuntimeNodeState targetNode, bool includeCookie)
        {
            if (targetNode.IsSelf)
                throw new InvalidOperationException($"Trying to send status to self!");
            ActorSelection selection = GetRemoteCoordinatorActorSelection(targetNode.Address);
            selection.Tell(new ClusterUpdateNodeState(s_phase, _clusterTargetPhase, (EntityGroupPhase[])_selfState.EntityGroupPhases.Clone(), includeCookie ? _cookie : null));
        }

        async Task StopEntityShardAsync(EntityKind entityKind)
        {
            // \note If shard doesn't exist on this node, it's never started
            EntityConfigBase entityConfig = EntityConfigRegistry.Instance.GetConfig(entityKind);
            bool isOwnedShard = _clusterState.Config.ResolveNodeShardIndex(entityKind, _selfAddress, out int selfShardIndex);
            MetaDebug.Assert(isOwnedShard, $"Trying to stop non-local EntityShard {entityKind}");
            EntityShardId selfShardId = new EntityShardId(entityKind, selfShardIndex);

            try
            {
                TimeSpan                shutdownTimeout = entityConfig.ShardShutdownTimeout;
                DateTime                stopAt          = DateTime.UtcNow + shutdownTimeout;
                IActorRef               shardActor      = _entitySharding.GetShardActor(selfShardId);
                Task<ShutdownComplete>  askTask         = shardActor.Ask<ShutdownComplete>(ShutdownSync.Instance, shutdownTimeout);

                while (true)
                {
                    Task completed = await Task.WhenAny(askTask, Task.Delay(1000));
                    if (completed == askTask)
                    {
                        await askTask; // raise exceptions
                        return;
                    }

                    if (DateTime.UtcNow > stopAt)
                        throw new TimeoutException($"Timeout while shutting down EntityShard {entityKind}");

                    _log.Information("Shutting down EntityShard {ShardName}..", entityKind);
                }
            }
            catch (Exception ex)
            {
                // Warn on errors (eg, timeout)
                _log.Warning("Exception while shutting down EntityShard {ShardName}: {Exception}", entityKind, ex);
            }
        }

        async Task CreateEntityShardGroupAsync(EntityShardGroup entityGroup)
        {
            // Create all local EntityShards in the entityGroup (in arbitrary order)
            EntityConfigBase[] groupEntityConfigs = _localEntityConfigs[entityGroup];
            _log.Information("Creating local EntityShardGroup {EntityGroup} with EntityShards: {EntityShards}", entityGroup, PrettyPrint.Compact(groupEntityConfigs.Select(cfg => cfg.EntityKind).ToArray()));
            foreach (EntityConfigBase entityConfig in groupEntityConfigs)
            {
                // If the EntityShard should be running locally, spawn & register it
                EntityKind entityKind = entityConfig.EntityKind;
                bool isOwnedShard = _clusterState.Config.ResolveNodeShardIndex(entityKind, _selfAddress, out int selfShardIndex);
                MetaDebug.Assert(isOwnedShard, $"Trying to start non-local EntityShard {entityKind}");

                EntityShardConfig shardConfig = new EntityShardConfig(entityKind, _clusterState.Config, selfShardIndex);
                IActorRef shardActor = await _entitySharding.CreateShardAsync(shardConfig);
                _log.Information("EntityShard {ShardName} started on actor {ShardActor}", entityKind, shardActor);
            }
        }

        async Task StartEntityShardGroupAsync(EntityShardGroup entityGroup)
        {
            EntityConfigBase[] groupEntityConfigs = _localEntityConfigs[entityGroup];
            _log.Information("Starting local EntityShardGroup {EntityGroup}: {EntityShards}", entityGroup, PrettyPrint.Compact(groupEntityConfigs.Select(cfg => cfg.EntityKind).ToArray()));

            // Start all local EntityShards in the group (in arbitrary order)
            foreach (EntityConfigBase entityConfig in groupEntityConfigs)
            {
                // Check that shard is a local one
                EntityKind entityKind = entityConfig.EntityKind;
                bool isOwnedShard = _clusterState.Config.ResolveNodeShardIndex(entityKind, _selfAddress, out int selfShardIndex);
                MetaDebug.Assert(isOwnedShard, $"Trying to start non-local EntityShard {entityKind}");

                // Tell the EntityShard to start itself
                EntityShardId selfShardId = new EntityShardId(entityKind, selfShardIndex);
                IActorRef shardActor = _entitySharding.GetShardActor(selfShardId);
                shardActor.Tell(new EntityShard.StartShard());
            }

            // Wait for all EntityShards in the group to be ready
            await WaitUntilEntityShardsReadyAsync(entityGroup, timeout: TimeSpan.FromMinutes(5));

            _log.Information("Local EntityShardGroup {EntityGroup} is up and running", entityGroup);
        }

        async Task WaitUntilEntityShardsReadyAsync(EntityShardGroup entityGroup, TimeSpan timeout)
        {
            EntityConfigBase[] groupEntityConfigs = _localEntityConfigs[entityGroup];

            IActorRef[] shardActors = groupEntityConfigs.Select(entityConfig =>
            {
                EntityKind entityKind = entityConfig.EntityKind;
                _ = _clusterState.Config.ResolveNodeShardIndex(entityKind, _selfAddress, out int selfShardIndex);
                EntityShardId selfShardId = new EntityShardId(entityKind, selfShardIndex);
                IActorRef shardActor = _entitySharding.GetShardActor(selfShardId);
                return shardActor;
            }).ToArray();

            DateTime timeoutAt      = DateTime.UtcNow + timeout;
            DateTime nextWaitLogAt  = DateTime.UtcNow + TimeSpan.FromSeconds(1);

            // Wait until the shard actor reports it's ready or failed to start. Wait until the timeout while printing status messages periodically.
            int             numShards               = shardActors.Length;
            Task<bool>[]    waitUntilRunningQueries = new Task<bool>[numShards];
            bool[]          isShardRunning          = new bool[numShards];
            for (; ; )
            {
                // Timeout deadline is strict. No last checks.
                if (DateTime.UtcNow > timeoutAt)
                    throw new TimeoutException($"Timeout while starting EntityShardGroup {entityGroup}");

                // Ensure we have a query to ping each shard
                for (int shardNdx = 0; shardNdx < numShards; shardNdx++)
                {
                    if (waitUntilRunningQueries[shardNdx] == null)
                        waitUntilRunningQueries[shardNdx] = EntityShard.TryWaitUntilRunning(shardActors[shardNdx]);
                }

                // Periodic print to show something is happening
                if (DateTime.UtcNow > nextWaitLogAt)
                {
                    nextWaitLogAt = DateTime.UtcNow + TimeSpan.FromSeconds(1);
                    EntityKind[] pendingShardKinds =
                        Enumerable.Range(0, numShards)
                        .Where(shardNdx => !isShardRunning[shardNdx])
                        .Select(shardNdx => groupEntityConfigs[shardNdx].EntityKind)
                        .ToArray();
                    _log.Information("Waiting for EntityShardGroup {ShardGroupName} to be ready, pending: {PendingShardKinds}", entityGroup, PrettyPrint.Compact(pendingShardKinds));
                }

                // Poll all shard queries
                try
                {
                    // Wait a bit for the all the queries to make progress
                    await Task.WhenAny(Task.WhenAll(waitUntilRunningQueries), Task.Delay(TimeSpan.FromMilliseconds(100)));

                    // Update shard statuses & reset any queries that returned false (not yet running)
                    for (int shardNdx = 0; shardNdx < numShards; shardNdx++)
                    {
                        if (waitUntilRunningQueries[shardNdx].IsCompleted)
                        {
                            bool isRunning = await waitUntilRunningQueries[shardNdx];
                            isShardRunning[shardNdx] = isRunning;

                            if (!isRunning)
                                waitUntilRunningQueries[shardNdx] = null;
                        }
                    }

                    // If all shards are running, we're done
                    if (isShardRunning.All(isRunning => isRunning))
                        break;
                }
                catch (EntityShardStartException ex)
                {
                    _log.Error("Failed to start EntityShard {ShardId}: {Error}", ex.ShardId, ex);

                    // Exit immediately and wait for process to get restarted
                    Application.Application.ForceTerminate(exitCode: 101, reason: Invariant($"Failed to start EntityShard {ex.ShardId}"));
                }
            }
        }

        async Task StopEntityShardGroupAsync(EntityShardGroup entityGroup)
        {
            // Stop all local EntityShards in the group (in arbitrary order)
            // \todo [petri] Stop all shards in parallel?
            _log.Information("Stopping EntityShardGroup {EntityGroup}: {EntityShards}", entityGroup, PrettyPrint.Compact(_localEntityConfigs[entityGroup].Select(cfg => cfg.EntityKind).ToArray()));
            foreach (EntityConfigBase entityConfig in _localEntityConfigs[entityGroup])
            {
                EntityKind entityKind = entityConfig.EntityKind;
                bool isOwnedShard = _clusterState.Config.ResolveNodeShardIndex(entityKind, _selfAddress, out int selfShardIndex);
                MetaDebug.Assert(isOwnedShard, $"Trying to stop non-local EntityShard {entityKind}");

                try
                {
                    await StopEntityShardAsync(entityKind);
                }
                catch (Exception ex)
                {
                    // Don't care about failures, we're terminating anyway
                    _log.Warning("Failed to stop EntityShard {EntityKind}, ignoring: {Exception}", entityKind, ex);
                }
            }
        }

        Task TerminateEntityShardGroupAsync(EntityShardGroup entityGroup)
        {
            _log.Information("Terminating EntityShardGroup {EntityGroup}: {EntityShards}", entityGroup, PrettyPrint.Compact(_localEntityConfigs[entityGroup].Select(cfg => cfg.EntityKind).ToArray()));
            // \todo [petri] actually terminate EntityShards

            return Task.CompletedTask;
        }

        async Task ProgressClusterStateAsync()
        {
            switch (s_phase)
            {
                case ClusterPhase.Connecting:
                    // When established connection to all peers, switch to Starting phase
                    if (_clusterState.GetNumConnectedNodes() == _clusterState.GetNumTotalNodes())
                    {
                        _log.Information("Connection established to all cluster peers");
                        SetClusterPhase(ClusterPhase.Starting, reason: "PeersConnected");
                    }
                    break;

                case ClusterPhase.Starting:
                    if (_selfState.EntityGroupPhases.All(phase => phase == EntityGroupPhase.Running))
                    {
                        _log.Information("All EntityGroups started, switching to ClusterPhase.Running!");
                        SetClusterPhase(ClusterPhase.Running, reason: "EntityGroups started");
                    }
                    else
                    {
                        // Figure out which EntityShardGroup we're progressing by finding the first ESG in non-Running phase.
                        // Note that this will skip without a barrier from Running phase of an ESG to creating the next ESG
                        // while the other nodes may still be in Starting phase. This is benign (and saves a barrier) as creating
                        // an ESG does not trigger any other behavior to happen; the entities on it only get initialized in the
                        // Starting phase (which isn't entered until all peers have been created).
                        int                             groupNdx        = Array.FindIndex(_selfState.EntityGroupPhases, phase => phase != EntityGroupPhase.Running);
                        EntityShardGroup                entityGroup     = (EntityShardGroup)groupNdx;
                        EntityGroupPhase                localPhase      = _selfState.EntityGroupPhases[groupNdx];
                        IEnumerable<EntityGroupPhase>   allPhases       = _clusterState.NodeSets.SelectMany(nodeSet => nodeSet.NodeStates).Select(node => node.EntityGroupPhases[groupNdx]);
                        bool                            allowProgress   = allPhases.All(phase => phase >= localPhase);

                        _log.Information("EntityShardGroup {EntityGroup} in phase {GroupPhase}: allowProgress={AllowProgress} (cluster members: {AllPhases})", entityGroup, localPhase, allowProgress, PrettyPrint.Compact(allPhases));

                        if (allowProgress)
                        {
                            if (localPhase == EntityGroupPhase.Pending)
                                await CreateEntityShardGroupAsync(entityGroup);
                            else if (localPhase == EntityGroupPhase.Created)
                                await StartEntityShardGroupAsync(entityGroup);
                            else
                                throw new InvalidOperationException($"EntityShardGroup {entityGroup} in invalid phase {localPhase}");

                            // Progress the local phase (including triggers)
                            // \note Doing this convoluted update to trigger the EntityShard registrations in UpdateClusterMemberState()
                            EntityGroupPhase[] newPhases = (EntityGroupPhase[])_selfState.EntityGroupPhases.Clone();
                            newPhases[groupNdx] = localPhase + 1;
                            _log.Information("Switching EntityShardGroup {EntityGroup} from phase {SourcePhase} to {TargetPhase}", entityGroup, localPhase, newPhases[groupNdx]);
                            UpdateClusterMemberState(_selfState, _selfState.ClusterPhase, newPhases);

                            // Inform peers
                            SendStateToPeers();
                        }
                    }
                    break;

                case ClusterPhase.Running:
                    // Handle shutdown request
                    if (s_clusterShutdownRequested.IsSet || _clusterTargetPhase > ClusterPhase.Running)
                    {
                        // Force phase to shutting down (propagates to all peers)
                        SetClusterPhase(ClusterPhase.Stopping, reason: "ShutdownRequested");
                    }
                    break;

                case ClusterPhase.Stopping:
                    if (_selfState.EntityGroupPhases.All(phase => phase == EntityGroupPhase.Terminated))
                    {
                        _log.Information("All EntityGroups fully terminated!");
                        SetClusterPhase(ClusterPhase.Terminated, reason: "EntityGroups terminated");
                    }
                    else
                    {
                        // \todo [petri] mostly duplicate code with Starting -- refactor
                        int                             groupNdx        = Array.FindLastIndex(_selfState.EntityGroupPhases, phase => phase != EntityGroupPhase.Terminated);
                        EntityShardGroup                entityGroup     = (EntityShardGroup)groupNdx;
                        EntityGroupPhase                localPhase      = _selfState.EntityGroupPhases[groupNdx];
                        IEnumerable<EntityGroupPhase>   allPhases       = _clusterState.NodeSets.SelectMany(nodeSet => nodeSet.NodeStates).Select(node => node.EntityGroupPhases[groupNdx]);
                        bool                            allowProgress   = allPhases.All(phase => phase >= localPhase);

                        _log.Information("EntityShardGroup {EntityGroup} in phase {GroupPhase}: allowProgress={AllowProgress} (cluster members: {AllPhases})", entityGroup, localPhase, allowProgress, PrettyPrint.Compact(allPhases));

                        if (allowProgress)
                        {
                            if (localPhase == EntityGroupPhase.Running)
                                await StopEntityShardGroupAsync(entityGroup);
                            else if (localPhase == EntityGroupPhase.Stopped)
                            {
                                // \todo [petri] implement termination -- do we need it really?
                                //await TerminateEntityShardGroupAsync(entityGroup);
                            }
                            else
                                throw new InvalidOperationException($"EntityShardGroup {entityGroup} in invalid phase {localPhase}");

                            // Progress the local phase (including triggers)
                            // \note Doing this convoluted update to trigger the EntityShard registrations in UpdateClusterMemberState()
                            EntityGroupPhase[] newPhases = (EntityGroupPhase[])_selfState.EntityGroupPhases.Clone();
                            newPhases[groupNdx] = localPhase + 1;
                            UpdateClusterMemberState(_selfState, _selfState.ClusterPhase, newPhases);

                            // Inform peers
                            SendStateToPeers();
                        }
                    }
                    break;

                case ClusterPhase.Terminated:
                    // nothing to do
                    break;

                default:
                    throw new InvalidOperationException($"Invalid ClusterPhase {s_phase}");
            }
        }

        void TryConnectToPeer(RuntimeNodeState node)
        {
            if (node.IsSelf)
                throw new InvalidOperationException("Trying to connect to self");

            if (!node.IsConnected)
            {
                _log.Debug("Connecting to {NodeAddress}", node.Address);

                // Fetch IP addresses of host (to verify DNS is working)
                try
                {
                    // Resolve address first (Kubernetes StatefulSets take a while to propagate DNS changes)
                    //IPHostEntry hostEntry = Dns.GetHostEntry(node.Address.HostName);
                    //_log.Debug("GetHostEntry({0}) = {1}, addresses: {2}", node.Address.HostName, hostEntry.HostName, string.Join(", ", hostEntry.AddressList.Select(ip => ip.ToString())));

                    //_log.Debug("Sending status update to {0} (self={1})", node.Address, _selfAddress);
                    SendStatusMessage(node, includeCookie: true);
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.HostNotFound)
                        _log.Debug("GetHostEntry({HostName}): host not found", node.Address.HostName);
                    else
                        _log.Warning("GetHostEntry({HostName}) failed with code {SocketErrorCode}: {Exception}", node.Address.HostName, e.SocketErrorCode, e);
                }
            }
        }

        void TryConnectToAllPeers()
        {
            // Try to connect to all peer nodes
            foreach (RuntimeNodeSetState nodeSet in _clusterState.NodeSets)
            {
                foreach (RuntimeNodeState node in nodeSet.NodeStates)
                {
                    if (!node.IsSelf)
                        TryConnectToPeer(node);
                }
            }
        }
    }
}
