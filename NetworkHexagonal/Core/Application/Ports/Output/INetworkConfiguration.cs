namespace NetworkHexagonal.Core.Application.Ports.Output
{
    public interface INetworkConfiguration
    {
        string Ip { get; }
        int Port { get; }
        string ConnectionKey { get; }
        TimeSpan UpdateInterval { get; }
        TimeSpan PingInterval { get; }
        TimeSpan DisconnectTimeout { get; }
        bool UnsyncedEvents { get; }
    }
}