// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

using Akka.Actor;
using Akka.Configuration;
using Metaplay.Cloud.Cluster;
using Metaplay.Cloud.Entity;
using Metaplay.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.FormattableString;

namespace Metaplay.Cloud.Sharding
{
    public static class IActorRefExtensions
    {
        // \todo [petri] this should be in Akka core?
        public static IActorRef GetOrElse(this IActorRef actorRef, Func<IActorRef> elseValue)
        {
            return actorRef.IsNobody() ? elseValue() : actorRef;
        }
    }

    public class EntityShardingCoordinator : MetaReceiveActor
    {
        // Response is the IActorRef
        public class CreateShard
        {
            public readonly EntityShardConfig ShardConfig;

            public CreateShard(EntityShardConfig shardConfig)
            {
                ShardConfig = shardConfig;
            }
        }

        public EntityShardingCoordinator()
        {
            Receive<CreateShard>(ReceiveCreateShard);
            Receive<ShutdownSync>(ReceiveShutdownSync);
        }

        void ReceiveCreateShard(CreateShard createShard)
        {
            // Start the EntityShard actor
            EntityShardConfig   shardConfig     = createShard.ShardConfig;
            EntityConfigBase    entityConfig    = EntityConfigRegistry.Instance.GetConfig(shardConfig.EntityKind);
            string              childName       = shardConfig.EntityKind.ToString();
            Type                entityShardType = entityConfig.EntityShardType;
            Props               shardProps      = Props.Create(entityShardType, shardConfig);
            IActorRef           shardActor      = Context.Child(childName).GetOrElse(() => Context.ActorOf(shardProps, childName));

            // Respond with the EntityShard's IActorRef
            Sender.Tell(shardActor);
        }

        void ReceiveShutdownSync(ShutdownSync shutdown)
        {
            _self.Tell(PoisonPill.Instance);
            Sender.Tell(ShutdownComplete.Instance);
        }
    }

    // \todo [petri] #dynamiccluster: Get rid of sharding strategies, move ShardActors ownership to ClusterCoordinator?
    public class EntityKindShardState
    {
        public readonly IShardingStrategy   Strategy;
        public readonly IActorRef[]         ShardActors;

        public EntityKindShardState(IShardingStrategy strategy, IActorRef[] actors)
        {
            Strategy = strategy;
            ShardActors = actors;
        }
    }

    /// <summary>
    /// Akka.NET extension for managing <see cref="EntityShard"/>.
    /// </summary>
    public class EntitySharding : IExtension
    {
        ExtendedActorSystem                             _actorSystem;
        IActorRef                                       _shardingCoordinator;

        object                                          _shardsLock     = new object();
        Dictionary<EntityKind, EntityKindShardState>    _shardStates    = new Dictionary<EntityKind, EntityKindShardState>();

        public static Config DefaultConfiguration()
        {
            // \note Not using HOCON configuration
            return new Config();
        }

        public static EntitySharding Get(ActorSystem system)
        {
            return system.WithExtension<EntitySharding, EntityShardingProvider>();
        }

        public EntitySharding(ExtendedActorSystem system)
        {
            _actorSystem = system ?? throw new ArgumentNullException(nameof(system));
            _shardingCoordinator = system.ActorOf(Props.Create<EntityShardingCoordinator>(), "shard");
        }

        /// <summary>
        /// Create the <see cref="EntityShard"/> instance, but don't yet initialize it.
        /// </summary>
        /// <param name="shardConfig"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<IActorRef> CreateShardAsync(EntityShardConfig shardConfig)
        {
            // Spawn the EntityShard actor, initialization happens later
            return await _shardingCoordinator.Ask<IActorRef>(new EntityShardingCoordinator.CreateShard(shardConfig));
        }

        public async Task ShutdownAsync(TimeSpan timeout)
        {
            await _shardingCoordinator.Ask<ShutdownComplete>(ShutdownSync.Instance, timeout);
        }

        /// <summary>
        /// Initialize shard states for all EntityKinds for all cluster members.
        /// </summary>
        /// <param name="clusterConfig"></param>
        public void InitializeEntityShardStates(ClusterConfig clusterConfig)
        {
            lock (_shardsLock)
            {
                foreach (EntityConfigBase entityConfig in EntityConfigRegistry.Instance.TypeToEntityConfig.Values)
                {
                    // Initialize shard state (with pre-allocated array of ShardActors for all cluster members having the shard)
                    List<NodeSetConfig> nodeSets        = clusterConfig.GetNodeSetsForEntityKind(entityConfig.EntityKind);
                    int                 totalShardCount = nodeSets.Sum(nodeSet => nodeSet.NodeCount);
                    IActorRef[]         shardActors     = new IActorRef[totalShardCount];
                    _shardStates[entityConfig.EntityKind] = new EntityKindShardState(entityConfig.ShardingStrategy, shardActors);
                }
            }
        }

        public void ResolveEntityShardActorsForNode(ClusterConfig clusterConfig, RuntimeNodeState nodeState, EntityShardGroup entityGroup)
        {
            lock (_shardsLock)
            {
                IActorRefProvider provider = _actorSystem.Provider;

                // Resolve the ShardActor for each EntityConfig matching the given EntityShardGroup.
                // Handle both remote and local actor references.
                foreach (EntityConfigBase entityConfig in EntityConfigRegistry.Instance.TypeToEntityConfig.Values)
                {
                    if (entityConfig.EntityShardGroup == entityGroup)
                    {
                        // Resolve ShardActors for such peers that have the EntityShard for the give EntityKind
                        EntityKind entityKind = entityConfig.EntityKind;
                        ClusterNodeAddress address = nodeState.Address;
                        bool hasEntityShard = clusterConfig.ResolveNodeShardIndex(entityKind, address, out int shardIndex);
                        if (hasEntityShard)
                        {
                            // Resolve EntityShard ActorRef on the target node (local or remote)
                            string projectName = MetaplayCore.Options.ProjectName;
                            string remotePrefix = nodeState.IsSelf ? Invariant($"akka://{projectName}") : Invariant($"akka.tcp://{projectName}@{address.HostName}:{address.Port}");
                            string actorPath = Invariant($"{remotePrefix}/user/shard/{entityKind}");
                            IActorRef[] shardActors = _shardStates[entityKind].ShardActors;
                            shardActors[shardIndex] = provider.ResolveActorRef(actorPath);
                        }
                    }
                }
            }
        }

        public void ClearEntityShardActorsForNode(ClusterConfig clusterConfig, RuntimeNodeState nodeState, EntityShardGroup entityGroup)
        {
            lock (_shardsLock)
            {
                foreach (EntityConfigBase entityConfig in EntityConfigRegistry.Instance.TypeToEntityConfig.Values)
                {
                    if (entityConfig.EntityShardGroup == entityGroup)
                    {
                        // Resolve which NodeSets have an EntityShard for entityKind
                        EntityKind  entityKind      = entityConfig.EntityKind;
                        bool        hasEntityShard  = clusterConfig.ResolveNodeShardIndex(entityKind, nodeState.Address, out int shardIndex);

                        if (hasEntityShard)
                        {
                            // Clear EntityShard ActorRef for the target member
                            IActorRef[] shardActors = _shardStates[entityKind].ShardActors;
                            shardActors[shardIndex] = null;
                        }
                    }
                }
            }
        }

        // \note Only used when resolving the local ShardActor which can be guaranteed to be valid
        // \todo [petri] #shard-refactor: Handle invalid shardActors at call sites (can happen when connection to peer node is lost)
        public IActorRef GetShardActor(EntityShardId shardId)
        {
            if (shardId.Value < 0)
                throw new ArgumentException($"Invalid ShardId, value must be non-negative: {shardId}");

            lock (_shardsLock)
            {
                EntityKindShardState shardState = _shardStates[shardId.Kind];

                int numShards = shardState.ShardActors.Length;
                if (shardId.Value >= numShards)
                    throw new ArgumentException($"Invalid ShardId, value out of bounds: {shardId} (numShards={numShards})");

                IActorRef shardActorRef = shardState.ShardActors[shardId.Value];
                return shardActorRef;
            }
        }

        public EntityKindShardState GetShardStatesForKind(EntityKind entityKind)
        {
            lock (_shardsLock)
            {
                return _shardStates[entityKind];
            }
        }

        public EntityKindShardState TryGetShardStatesForKind(EntityKind entityKind)
        {
            lock (_shardsLock)
            {
                if (_shardStates.TryGetValue(entityKind, out EntityKindShardState state))
                    return state;
                else
                    return null;
            }
        }
    }
}
