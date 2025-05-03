using System;
using NetworkHexagonal.Core.Domain.Events;

namespace NetworkHexagonal.Core.Application.Ports.Inbound;

public interface IClientApp
    {
        event Action<ConnectionEvent> OnConnected;
        event Action<DisconnectionEvent> OnDisconnected;
        event Action<NetworkErrorEvent> OnError;
        Task ConnectAsync(string serverAddress, int port, int timeoutMs = 5000);
        void Disconnect();
        void Dispose();
        void Initialize();
        void Update();
    }
