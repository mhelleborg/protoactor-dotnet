using System;
using JetBrains.Annotations;

namespace Proto.Cluster.Data
{
    [PublicAPI]
    public class LeaderInfo
    {
        public LeaderInfo(Guid memberId, string host, int port, string[] bannedMembers)
        {
            MemberId = memberId;
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Port = port;
            BannedMembers = bannedMembers;
        }

        public string[] BannedMembers { get; }

        public string Address => Host + ":" + Port;
        public Guid MemberId { get; }
        public string Host { get; }
        public int Port { get; }

        public override string ToString() => $"LeaderStatus Address:{Address} ID:{MemberId}";
    }
}