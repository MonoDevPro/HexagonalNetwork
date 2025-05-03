using System;
using NetworkHexagonal.Core.Domain.Events;

namespace NetworkHexagonal.Core.Application.Ports.Inbound;

public interface IServerApp
    {
        public event Action<ConnectionRequestEvent> OnConnectionRequest;
        public event Action<ConnectionEvent> OnPeerConnected;
        public event Action<DisconnectionEvent> OnPeerDisconnected;
        public event Action<NetworkErrorEvent> OnError;
        void DisconnectPeer(int peerId);
        void Dispose();
        void Initialize();
        void Start(int port);
        void Stop();
        void Update();
    }