using System;

namespace NetworkHexagonal.Core.Application.Ports.Output
{
    public class NetworkConfiguration : INetworkConfiguration
    {
        public string Ip { get; set; }
        public int Port { get; set; }
        public string ConnectionKey { get; set; }
        public TimeSpan UpdateInterval { get; set; }
        public TimeSpan PingInterval { get; set; }
        public TimeSpan DisconnectTimeout { get; set; }

        public NetworkConfiguration() { }

        public NetworkConfiguration(string ip, int port, string connectionKey, TimeSpan updateInterval, TimeSpan pingInterval, TimeSpan disconnectTimeout)
        {
            Ip = ip;
            Port = port;
            ConnectionKey = connectionKey;
            UpdateInterval = updateInterval;
            PingInterval = pingInterval;
            DisconnectTimeout = disconnectTimeout;
        }
    }
}
