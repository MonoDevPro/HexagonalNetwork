namespace NetworkHexagonal.Core.Application.Ports.Input
{
    public interface IClientNetworkService : INetworkService
    {
        Task ConnectAsync(int timeoutMs = 5000);
        void Disconnect();
        event Action OnConnected;
        event Action OnDisconnected;
        event Action<IPacket> OnPacketReceived;
    }
}
