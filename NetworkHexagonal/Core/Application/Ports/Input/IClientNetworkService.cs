namespace NetworkHexagonal.Core.Application.Ports.Input
{
    public interface IClientNetworkService : INetworkService
    {
        Task ConnectAsync(int timeoutMs = 5000);
        void Disconnect();
        event Action OnConnected;
        event Action<string> OnDisconnected;
        event Action<int>? OnPingReceived;
        event Action<int> OnPacketReceived;
    }
}
