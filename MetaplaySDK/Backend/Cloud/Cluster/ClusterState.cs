// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

using Metaplay.Cloud.Entity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metaplay.Cloud.Cluster
{
    public class RuntimeNodeState
    {
        public readonly RuntimeNodeSetState NodeSet;
        public readonly ClusterNodeAddress  Address;
        public readonly bool                IsSelf;

        public bool                 IsConnected         { get; private set; }
        public ClusterPhase         ClusterPhase        { get; private set; }
        public EntityGroupPhase[]   EntityGroupPhases   { get; private set; }

        public RuntimeNodeState(RuntimeNodeSetState nodeSet, ClusterNodeAddress address, bool isSelf)
        {
            // Configuration
            NodeSet         = nodeSet;
            Address         = address;
            IsSelf          = isSelf;

            // Runtime state
            IsConnected         = isSelf;
            ClusterPhase        = ClusterPhase.Connecting;
            EntityGroupPhases   = new EntityGroupPhase[(int)EntityShardGroup.Last];
        }

        public void SetDisconnected()
        {
            // Reset runtime state
            IsConnected         = false;
            ClusterPhase        = ClusterPhase.Connecting;
            EntityGroupPhases   = new EntityGroupPhase[(int)EntityShardGroup.Last];
        }

        public void UpdateState(ClusterPhase localPhase, EntityGroupPhase[] entityGroupPhases)
        {
            IsConnected = true;
            ClusterPhase = localPhase;
            EntityGroupPhases = entityGroupPhases;
        }
    }

    public class RuntimeNodeSetState
    {
        public readonly NodeSetConfig           Config;
        public readonly List<RuntimeNodeState>  NodeStates  = new List<RuntimeNodeState>();

        public RuntimeNodeSetState(ClusterConfig clusterConfig, NodeSetConfig nodeSetConfig, ClusterNodeAddress selfAddress)
        {
            Config = nodeSetConfig;

            for (int nodeNdx = 0; nodeNdx < nodeSetConfig.NodeCount; nodeNdx++)
            {
                ClusterNodeAddress address = nodeSetConfig.ResolveNodeAddress(nodeNdx);
                NodeStates.Add(new RuntimeNodeState(this, address, isSelf: address == selfAddress));
            }
        }
    }

    public class ClusterState
    {
        public readonly ClusterConfig               Config;
        public readonly List<RuntimeNodeSetState>   NodeSets = new List<RuntimeNodeSetState>();

        public ClusterState(ClusterConfig config, ClusterNodeAddress selfAddress)
        {
            Config = config;

            foreach (NodeSetConfig nodeSetConfig in config.NodeSets)
                NodeSets.Add(new RuntimeNodeSetState(config, nodeSetConfig, selfAddress));
        }

        public RuntimeNodeState GetNodeState(ClusterNodeAddress address)
        {
            foreach (RuntimeNodeSetState nodeSet in NodeSets)
            {
                if (nodeSet.Config.ResolveShardIndex(address, out int shardIndex))
                    return nodeSet.NodeStates[shardIndex];
            }

            throw new InvalidOperationException($"GetNodeState(): invalid address {address}");
        }

        public int GetNumTotalNodes()
        {
            return NodeSets.Sum(nodeSet => nodeSet.NodeStates.Count);
        }

        public int GetNumConnectedNodes()
        {
            return NodeSets.Sum(nodeSet => nodeSet.NodeStates.Sum(nodeState => nodeState.IsConnected ? 1 : 0));
        }
    }
}
