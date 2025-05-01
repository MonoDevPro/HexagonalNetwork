namespace NetworkHexagonal.Core.Application.Ports.Input
{
    public interface IServerNetworkService : INetworkService
    {
        void Start();
        void Stop();
        void DisconnectPeer(int peerId);
        event Action<int> OnPeerConnected;
        event Action<int> OnPeerDisconnected;
        event Action<int, IPacket> OnPacketReceivedFromPeer;
    }
}
