// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

using Metaplay.Cloud.Cluster;
using Metaplay.Cloud.Entity;
using Metaplay.Cloud.Options;
using Metaplay.Cloud.RuntimeOptions;
using Metaplay.Cloud.Sharding;
using Metaplay.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metaplay.Server.UdpPassthrough
{
    [EntityConfig]
    public class UdpPassthroughShardConfig : EphemeralEntityConfig
    {
        public override EntityKind          EntityKind              => EntityKindCloudCore.UdpPassthrough;
        public override bool                AllowEntitySpawn        => true;
        public override Type                EntityShardType         => typeof(UdpPassthroughShard);
        public override NodeSetPlacement    NodeSetPlacement        => NodeSetPlacement.Logic;
        public override IShardingStrategy   ShardingStrategy        => new ManualShardingStrategy();
        public override TimeSpan            ShardShutdownTimeout    => TimeSpan.FromSeconds(60);

        // \note: If disabled, use base actor to trick Registry error checks to pass. This shard will be disabled anyway so it's harmless.
        public override Type                EntityActorType         => UdpPassthroughShard.TryGetEntityActorTypeForConfig() ?? typeof(UdpPassthroughHostActorBase);
    }

    class UdpPassthroughShard : EntityShard
    {
        /// <summary>
        /// Returns the only concrete listener type if it is available. Otherwise null.
        /// </summary>
        internal static Type TryGetEntityActorTypeForConfig()
        {
            UdpPassthroughOptions opts = RuntimeOptionsRegistry.Instance.GetCurrent<UdpPassthroughOptions>();
            if (opts.UseDebugServer)
            {
                return typeof(UdpPassthroughDebugServerActor);
            }

            List<Type> listenerTypes = GetListenerTypes();
            if (listenerTypes.Count == 0)
                return null; // no implementation.
            if (listenerTypes.Count > 1)
                return null; // ambiguous implementation.
            return listenerTypes[0];
        }

        static List<Type> GetListenerTypes()
        {
            return new List<Type>(IntegrationRegistry.IntegrationClasses<UdpPassthroughHostActorBase>().Where(type => type != typeof(UdpPassthroughDebugServerActor)));
        }

        public UdpPassthroughShard(EntityShardConfig shardConfig) : base(shardConfig)
        {
            (UdpPassthroughOptions options, ClusteringOptions clusterOpts, DeploymentOptions deploymentOpts) = RuntimeOptionsRegistry.Instance.GetCurrent<UdpPassthroughOptions, ClusteringOptions, DeploymentOptions>();

            List<Type> listenerTypes = GetListenerTypes();
            Type listenerType;

            // Resolve the listener. This must be unambiguous.
            if (options.UseDebugServer)
            {
                listenerType = typeof(UdpPassthroughDebugServerActor);
            }
            else if (listenerTypes.Count > 1)
            {
                throw new InvalidOperationException($"Ambiguous implementation for UdpPassthroughHostActorBase. Could be any of {string.Join(" ,", listenerTypes.Select(type => type.ToNamespaceQualifiedTypeString()))}.");
            }
            else if (!options.Enabled)
            {
                if (listenerTypes.Count != 0)
                    _log.Warning("UDP passthrough listener is defined as {ActorName} but passthrough is disabled.", listenerTypes[0].ToNamespaceQualifiedTypeString());
                return;
            }
            else if (listenerTypes.Count == 0)
            {
                // Passthrough is enabled, but there is no implementation.
                // Since `Enabled` can be set in infra, we want to tolerate (but warn loudly) cases where the old non-supporting server could be deployed on udp-enabled infra.
                _log.Error("UDP passthrough is enabled but there is no implementation for UdpPassthroughHostActorBase. UDP passthrough is disabled.");
                return;
            }
            else
            {
                // Success
                listenerType = listenerTypes[0];
            }

            // UDP passthrough requires support from infrastructure
            DeploymentVersion requiredInfraVersion = new DeploymentVersion(0, 2, 6, null);
            if (deploymentOpts.InfrastructureVersion != null && DeploymentVersion.ParseFromString(deploymentOpts.InfrastructureVersion) < requiredInfraVersion)
                throw new InvalidOperationException($"UDP passthrough requires infrastructure version {requiredInfraVersion} but current version is {deploymentOpts.InfrastructureVersion}.");

            // Sanity checks
            EntityConfigBase entityConfig = EntityConfigRegistry.Instance.GetConfig(shardConfig.EntityKind);
            MetaDebug.Assert(listenerType == entityConfig.EntityActorType, "Internal error. Listener type resolution resolved into an inconsistent type.");

            // Spawn the host actor if this shard be active. Otherwise, do nothing.
            UdpPassthroughGateways.Gateway? currentNodeGateway = UdpPassthroughGateways.TryGetGatewayOnThisNode();
            if (currentNodeGateway.HasValue)
            {
                // Ensure that the relevant Entity is running
                EntityId localEntityId = currentNodeGateway.Value.AssociatedEntityId;

                // \hack: mutating directly
                _alwaysRunningEntities.Add(localEntityId);

                _log.Debug("Starting UDP passthrough for public port {PublicPort}", currentNodeGateway.Value.Port);
            }
            else
            {
                if (options.UseCloudPublicIp)
                {
                    _log.Debug("UDP passthrough not enabled on this node due to the missing public IP: {ShardIndex}", shardConfig.SelfIndex);
                }
                else if (UdpPassthroughOptions.IsCloudEnvironment)
                {
                    _log.Debug("UDP passthrough not enabled. No external port for this pod: {ShardIndex}", shardConfig.SelfIndex);
                }
                else
                {
                    _log.Debug("UDP passthrough not enabled. No local port for this shard: {ShardIndex}", shardConfig.SelfIndex);
                }
            }
        }
    }
}
