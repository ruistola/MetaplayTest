// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

using Metaplay.Core;
using System;
using System.Collections.Generic;

namespace Metaplay.Cloud.Sharding
{
    /// <summary>
    /// Provides mapping from a <see cref="EntityId"/> to the responsible <see cref="EntityShard"/> and the responsible <see cref="EntityId"/>
    /// within it. The concept of "responsible" is dependent on vantage point -- for example, EntityId of a service could be mapped to the
    /// Local shard and the Local service within it.
    /// </summary>
    public interface IShardingStrategy
    {
        /// <summary>
        /// For a given entity, returns the responsible <see cref="EntityShard"/> and the responsible <see cref="EntityId"/>
        /// within it.
        /// </summary>
        EntityShardId ResolveShardId(EntityId entityId);

        /// <summary>
        /// Resolve which <see cref="EntityId"/>s should be auto-spawned on a given <see cref="EntityShardId"/>.
        /// </summary>
        /// <param name="shardId"></param>
        /// <returns></returns>
        IReadOnlyList<EntityId> GetAutoSpawnEntities(EntityShardId shardId);
    }

    /// <summary>
    /// Distributes entities uniformly to shards based on <c>EntityId</c>. The entities are sharded across the pods/nodes
    /// in round-robin fashion using modulo arithmetic: <c>shardId = entityId.Value % numShards</c>.
    /// </summary>
    public class StaticModuloShardingStrategy : IShardingStrategy
    {
        public StaticModuloShardingStrategy()
        {
        }

        public EntityShardId ResolveShardId(EntityId entityId)
        {
            int numShards = Application.Application.Instance.ClusterConfig.GetNodeCountForEntityKind(entityId.Kind);
            if (numShards <= 0)
                throw new ArgumentException($"Invalid amount ({numShards}) of shard instances defined for {entityId.Kind} shard in ClusterConfig. There must be at least one instance.");

            return new EntityShardId(entityId.Kind, (int)(entityId.Value % (uint)numShards));
        }

        public IReadOnlyList<EntityId> GetAutoSpawnEntities(EntityShardId shardId) => null;
    }

    /// <summary>
    /// Automatically spawns an entity on each <c>EntityShard</c> matching the <c>EntityKind</c>.
    /// </summary>
    public class StaticServiceShardingStrategy : IShardingStrategy
    {
        public readonly bool IsSingleton;

        public StaticServiceShardingStrategy(bool isSingleton)
        {
            IsSingleton = isSingleton;
        }

        public EntityShardId ResolveShardId(EntityId entityId)
        {
            int numShards = Application.Application.Instance.ClusterConfig.GetNodeCountForEntityKind(entityId.Kind);
            if (numShards <= 0)
                throw new ArgumentException($"Invalid amount ({numShards}) of shard instances defined for {entityId.Kind} shard in ClusterConfig. There must be at least one instance.");

            // \todo Consider using this instead of modulo logic in the future -- we should aim for a more direct mapping from EntityId to ShardId
            //if (entityId.Value >= (ulong)numShards)
            //    throw new ArgumentException($"EntityId.Value ({entityId.Value}) is greater-or-equal to numShards ({numShards}).");
            //return new EntityShardId(entityId.Kind, entityId.Value);

            return new EntityShardId(entityId.Kind, (int)(entityId.Value % (uint)numShards));
        }

        public IReadOnlyList<EntityId> GetAutoSpawnEntities(EntityShardId shardId)
        {
            // For singleton services, only allow spawning on the first shard
            if (IsSingleton && shardId.Value != 0)
                return null;

            // Otherwise, spawn a service on this shard
            return new EntityId[] { EntityId.Create(shardId.Kind, (uint)shardId.Value) };
        }
    }

    /// <summary>
    /// Shards entities based on routing decision payload encoded on EntityId. This allows for setting routing rules at runtime, but
    /// requires that all <see cref="EntityId"/>s are created with <see cref="CreateEntityId"/>.
    /// </summary>
    public class ManualShardingStrategy : IShardingStrategy
    {
        const int   ShardIndexBits  = 16;                                   // Number of bits to reserve for shardIndex
        const int   ShardIndexShift = EntityId.KindShift - ShardIndexBits;  // Use highest N bits for shardIndex
        const uint  ShardIndexMask  = (1 << ShardIndexBits) - 1;            // Mask for extracting shardIndex
        const ulong ValueMask       = (1ul << ShardIndexShift) - 1;

        public ManualShardingStrategy()
        {
        }

        public EntityShardId ResolveShardId(EntityId entityId)
        {
            return new EntityShardId(entityId.Kind, (int)(entityId.Value >> ShardIndexShift));
        }

        public static EntityId CreateEntityId(EntityShardId shardId, ulong runningId)
        {
            MetaDebug.Assert(shardId.Value >= 0 && shardId.Value <= ShardIndexMask, "Invalid shardId {0}, must be between 0 and {1}", shardId, ShardIndexMask);
            MetaDebug.Assert(runningId >= 0 && runningId <= ValueMask, "Invalid runningId for {0}: {1}", shardId.Kind, runningId);
            ulong value = ((ulong)(uint)shardId.Value << ShardIndexShift) | runningId;
            return EntityId.Create(shardId.Kind, value);
        }

        public IReadOnlyList<EntityId> GetAutoSpawnEntities(EntityShardId shardId) => null;
    }

    /// <summary>
    /// Helper class for creating various <see cref="IShardingStrategy"/> instances.
    /// </summary>
    public static class ShardingStrategies
    {
        /// <summary>
        /// Create a <see cref="StaticModuloShardingStrategy"/> for a given <see cref="EntityKind"/>.
        /// </summary>
        public static IShardingStrategy CreateStaticSharded() => new StaticModuloShardingStrategy();

        /// <summary>
        /// Create a service strategy. An instance of the entity is automatically spawned on each pod/node
        /// that matches the <c>EntityKind</c>.
        /// </summary>
        /// <returns></returns>
        public static IShardingStrategy CreateService() => new StaticServiceShardingStrategy(isSingleton: false);

        /// <summary>
        /// Global singleton service. One instance of the service is automatically started.
        /// </summary>
        public static IShardingStrategy CreateSingletonService() => new StaticServiceShardingStrategy(isSingleton: true);

        /// <summary>
        /// Create <see cref="ManualShardingStrategy"/>. The <c>EntityIds</c> are manually generated with the
        /// <c>ShardId</c> embedded into the <c>EntityId</c> such that the <c>ShardId</c> can be extracted back
        /// from the <c>EntityId</c>.
        /// </summary>
        /// <returns></returns>
        public static IShardingStrategy CreateManual() => new ManualShardingStrategy();
    }
}
