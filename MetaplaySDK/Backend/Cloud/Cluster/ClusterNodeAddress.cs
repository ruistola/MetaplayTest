// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

using System;
using static System.FormattableString;

namespace Metaplay.Cloud.Cluster
{
    /// <summary>
    /// Physical address of a cluster node (hostname and port).
    /// </summary>
    public class ClusterNodeAddress : IEquatable<ClusterNodeAddress>
    {
        /// <summary>
        /// IP address, hostname, relative domain name, or FQDN.
        /// </summary>
        public readonly string  HostName;
        public readonly int     Port;

        public ClusterNodeAddress(string hostName, int port)
        {
            HostName = hostName;
            Port = port;
        }

        public bool Equals(ClusterNodeAddress other) => this == other;
        public static bool operator ==(ClusterNodeAddress a, ClusterNodeAddress b)
        {
            if (ReferenceEquals(a, b))
                return true;
            else if (a is null || b is null)
                return false;
            else
                return (a.HostName == b.HostName) && (a.Port == b.Port);
        }
        public static bool operator !=(ClusterNodeAddress a, ClusterNodeAddress b) => !(a == b);

        public override int     GetHashCode ()              => HostName.GetHashCode() + Port;
        public override bool    Equals      (object obj)    => (obj is ClusterNodeAddress addr) ? Equals(addr) : false;
        public override string  ToString    ()              => Invariant($"{HostName}:{Port}");
    }
}
