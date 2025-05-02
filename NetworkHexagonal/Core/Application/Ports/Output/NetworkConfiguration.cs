using System;

namespace NetworkHexagonal.Core.Application.Ports.Output
{
    public class NetworkConfiguration : INetworkConfiguration
    {
        public string Ip { get; set; } = "locallhost";
        public int Port { get; set; } = 8090;
        public string ConnectionKey { get; set; } = "default";
        public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromMilliseconds(15);
        public TimeSpan PingInterval { get; set; } = TimeSpan.FromSeconds(1);
        public TimeSpan DisconnectTimeout { get; set; } = TimeSpan.FromSeconds(5);
        public bool UnsyncedEvents { get; set; } = true;

        public NetworkConfiguration() { }

        public NetworkConfiguration(
            string ip, 
        int port, string connectionKey, 
        TimeSpan updateInterval, 
        TimeSpan pingInterval, 
        TimeSpan disconnectTimeout, 
        bool unsyncedEvents)
        {
            Ip = ip;
            Port = port;
            ConnectionKey = connectionKey;
            UpdateInterval = updateInterval;
            PingInterval = pingInterval;
            DisconnectTimeout = disconnectTimeout;
            UnsyncedEvents = unsyncedEvents;
        }
    }
}
