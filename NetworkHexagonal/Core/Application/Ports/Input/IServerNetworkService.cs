using System.Net;

namespace NetworkHexagonal.Core.Application.Ports.Input
{
    public interface IServerNetworkService : INetworkService
    {
        void Start();
        void Stop();
        void DisconnectPeer(int peerId);
        event Action<bool, IPEndPoint> OnConnectionRequest;
        event Action<int> OnPeerConnected;
        event Action<int> OnPeerDisconnected;
        event Action<int, int>? OnPingReceivedFromPeer;
        event Action<int> OnPacketReceivedFromPeer;
    }
}
