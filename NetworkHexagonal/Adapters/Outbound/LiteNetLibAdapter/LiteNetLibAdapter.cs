using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using NetworkHexagonal.Adapters.Outbound.Networking.Packet;
using NetworkHexagonal.Adapters.Outbound.Networking.Serializer;
using NetworkHexagonal.Core.Application.Ports;
using NetworkHexagonal.Core.Application.Ports.Input;
using NetworkHexagonal.Core.Application.Ports.Output;

namespace NetworkHexagonal.Adapters.Outbound.LiteNetLibAdapter
{
    public class LiteNetLibAdapter : 
    INetworkService, IPacketSender, IPacketRegistry, 
    IClientNetworkService, IServerNetworkService
    {
        private readonly ILogger _logger;
        private readonly INetworkSerializer _serializer;
        private readonly IPacketCallbacks _packetCallbacks;
        private INetworkConfiguration _configuration;
        private NetManager? _netManager;
        private EventBasedNetListener? _listener;

        // Eventos de Client
        public event Action? OnConnected;
        public event Action<string>? OnDisconnected;
        public event Action<int>? OnPingReceived;
        public event Action<int>? OnPacketReceived;
        // Eventos de Server
        public event Action<bool, IPEndPoint>? OnConnectionRequest;
        public event Action<int>? OnPeerConnected;
        public event Action<int>? OnPeerDisconnected;
        public event Action<int, int>? OnPingReceivedFromPeer;
        public event Action<int>? OnPacketReceivedFromPeer;

        public LiteNetLibAdapter(
            INetworkSerializer serializer,
            IPacketCallbacks packetCallbacks,
            INetworkConfiguration configuration,
            ILogger logger)
        {
            _serializer = serializer;
            _packetCallbacks = packetCallbacks;
            _configuration = configuration;
            _logger = logger;

            // Redireciona logs internos da LiteNetLib para o logger do projeto
            LiteNetLib.NetDebug.Logger = new LiteNetLibLoggerAdapter(_logger);
        }

        // INetworkService
        public void Initialize()
        {
            _listener = new EventBasedNetListener();
            _netManager = new NetManager(_listener);

            RegisterEventHandlers();

            _logger.LogTrace("LiteNetLibAdapter initialized.");

            Configure(_configuration);
        }

        public void Configure(INetworkConfiguration config)
        {
            if (_netManager == null || _listener == null)
                throw new InvalidOperationException("Adapter not initialized");

            // Atualiza configurações em runtime (hot-reload)
            if (config != _configuration) 
                _configuration = config;

            _netManager.DisconnectTimeout = (int)config.DisconnectTimeout.TotalMilliseconds;
            _netManager.UpdateTime = (int)config.UpdateInterval.TotalMilliseconds;
            _netManager.PingInterval = (int)config.PingInterval.TotalMilliseconds;
            _netManager.UnsyncedEvents = config.UnsyncedEvents;

            _logger.LogTrace("LiteNetLibAdapter configured with IP: {Ip}, Port: {Port}, ConnectionKey: {ConnectionKey}",
                config.Ip, config.Port, config.ConnectionKey);
        }

        public void Dispose()
        {
            _netManager?.Stop();
            _netManager = null;
            _listener = null;
            _logger.LogTrace("LiteNetLibAdapter disposed.");
        }

        // IPacketSender
        public void SendTo(int peerId, IPacket packet, NetworkDelivery method, byte channel)
        {
            var peer = _netManager?.ConnectedPeerList?.Find(p => p.Id == peerId);
            if (peer == null) return;
            var data = _serializer.SerializePacket(packet);
            peer.Send(data, (DeliveryMethod)method);
            _logger.LogTrace("Packet sent to peer {PeerId} with method {Method}", peerId, method);
        }

        public void SendToAll(IPacket packet, NetworkDelivery method, byte channel)
        {
            var data = _serializer.SerializePacket(packet);
            foreach (var peer in _netManager?.ConnectedPeerList ?? Enumerable.Empty<NetPeer>())
                peer.Send(data, (LiteNetLib.DeliveryMethod)method);

            _logger.LogTrace("Packet sent to all peers with method {Method}", method);
        }

        public void SendToAllExcept(int excludedPeerId, IPacket packet, NetworkDelivery method, byte channel)
        {
            var data = _serializer.SerializePacket(packet);
            foreach (var peer in _netManager?.ConnectedPeerList ?? Enumerable.Empty<NetPeer>())
                if (peer.Id != excludedPeerId)
                    peer.Send(data, (LiteNetLib.DeliveryMethod)method);

            _logger.LogTrace("Packet sent to all peers except {ExcludedPeerId} with method {Method}", excludedPeerId, method);
        }

        // IPacketRegistry
        public void Register<TPacket>(Action<TPacket> callback) 
        where TPacket : IPacket, new()
        {
            _packetCallbacks.RegisterCallback<TPacket>(callback);
            _logger.LogTrace("Packet type {PacketType} registered", typeof(TPacket));
        }

        public void Register<TPacket, TUserData>(Action<TPacket, TUserData> callback) where TPacket : IPacket, new()
        {
            _packetCallbacks.RegisterCallback<TPacket, TUserData>(callback);

            _logger.LogTrace("Packet type {PacketType} registered", typeof(TPacket));
        }

        public async Task ConnectAsync(int timeoutMs = 5000)
        {
            if (_netManager == null || _listener == null)
                throw new InvalidOperationException("Adapter not initialized");

            if (!_netManager.IsRunning)
                _netManager.Start(); // <-- ESSENCIAL para o client!

            _logger.LogTrace("Attempting to connect to server at {Ip}:{Port} with key {ConnectionKey }",
                _configuration.Ip, _configuration.Port, _configuration.ConnectionKey);
            
            var tcs = new TaskCompletionSource<bool>();
            void OnConnect()
            {
                tcs.TrySetResult(true);
                _logger.LogTrace("Async Connected to server.");
                Unregister();
            }
            void OnDisconnect(string reason)
            {
                tcs.TrySetResult(false);
                _logger.LogWarning("Async Disconnected from server.");
                Unregister();
            }
            OnConnected += OnConnect;
            OnDisconnected += OnDisconnect;

            _netManager.Connect(_configuration.Ip, _configuration.Port, _configuration.ConnectionKey);
            await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
            
            _logger.LogTrace("Async Connection attempt completed.");

            return;

            void Unregister()
            {
                OnConnected -= OnConnect;
                OnDisconnected -= OnDisconnect;
            }
        }

        public void Disconnect()
        {
            _netManager?.DisconnectAll();
            OnDisconnected?.Invoke("Disconnected by user");
        }

        public void Start()
        {
            _logger.LogTrace("Starting LiteNetLibAdapter on port {Port}", _configuration.Port);

            if (_netManager == null || _listener == null)
                throw new InvalidOperationException("Adapter not initialized");
            
            var startListenerResult = _netManager.Start(_configuration.Port);

            if (!startListenerResult)
            {
                _logger.LogError("Failed to start LiteNetLibAdapter on port {Port}", _configuration.Port);
                throw new InvalidOperationException($"Failed to start LiteNetLibAdapter on port {_configuration.Port}");
            }
        }

        public void Stop()
        {
            _netManager?.Stop();
        }

        public void DisconnectPeer(int peerId)
        {
            var peer = _netManager?.ConnectedPeerList?.Find(p => p.Id == peerId);
            peer?.Disconnect();
        }

        public void Update()
        {
            if (_netManager == null || !_netManager.IsRunning)
                return;

            _netManager.PollEvents();
        }

        private void RegisterEventHandlers()
        {
            if (_listener == null){
                _logger.LogError("Listener is not initialized. Cannot register event handlers.");
                return;
            }
            
            // Evita múltiplos registros
            _listener.ConnectionRequestEvent -= HandleConnectionRequest;
            _listener.PeerConnectedEvent -= HandlePeerConnected;
            _listener.PeerDisconnectedEvent -= HandlePeerDisconnected;
            _listener.NetworkLatencyUpdateEvent -= HandlePingReceived;
            _listener.NetworkReceiveEvent -= HandleNetworkReceive;
            _listener.NetworkErrorEvent -= HandleNetworkError;

            _listener.ConnectionRequestEvent += HandleConnectionRequest;
            _listener.PeerConnectedEvent += HandlePeerConnected;
            _listener.PeerDisconnectedEvent += HandlePeerDisconnected;
            _listener.NetworkLatencyUpdateEvent += HandlePingReceived;
            _listener.NetworkReceiveEvent += HandleNetworkReceive;
            _listener.NetworkErrorEvent += HandleNetworkError;
        }

        private void HandleConnectionRequest(ConnectionRequest connectionRequest)
        {
            _logger.LogTrace("Connection request from {RemoteEndPoint}", connectionRequest.RemoteEndPoint);

            bool sucess;
            var peerRequest = connectionRequest.AcceptIfKey(_configuration.ConnectionKey);
            if (peerRequest == null)
            {
                sucess = false;
                connectionRequest.Reject();
                OnConnectionRequest?.Invoke(sucess, connectionRequest.RemoteEndPoint);
                return;
            }
            sucess = true;
            OnConnectionRequest?.Invoke(sucess, connectionRequest.RemoteEndPoint);
        }

        private void HandlePeerConnected(NetPeer peer)
        {
            OnPeerConnected?.Invoke(peer.Id);
            OnConnected?.Invoke();
        }

        private void HandlePeerDisconnected(NetPeer peer, DisconnectInfo info)
        {
            OnPeerDisconnected?.Invoke(peer.Id);
            OnDisconnected?.Invoke(info.Reason.ToString());
        }

        private void HandlePingReceived(NetPeer peer, int ping)
        {
            OnPingReceivedFromPeer?.Invoke(peer.Id, ping);
            OnPingReceived?.Invoke(ping);
        }

        private void HandleNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod method)
        {
            var data = reader.GetRemainingBytes();
            var bufferReader = _serializer.DeserializePacket(data);

            _packetCallbacks.InvokeCallback(bufferReader, peer.Id);

            OnPacketReceivedFromPeer?.Invoke(peer.Id);
            OnPacketReceived?.Invoke(peer.Id);
            
            reader.Recycle(); // Sempre recicle o reader, mesmo se o pacote não for reconhecido
        }

        private void HandleNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError error)
        {
            OnDisconnected?.Invoke(error.ToString());
        }

        // Métodos e eventos auxiliares para integração com LiteNetLib podem ser adicionados aqui
    
    
    private class LiteNetLibReaderAdapter : INetworkReader, IDisposable
    {
        private readonly NetDataReader _reader;

        public LiteNetLibReaderAdapter(NetDataReader netDataReader)
        {
            _reader = netDataReader;
        }

            public bool AtEndOfStream => _reader.EndOfData;

            public long BytesLeft => _reader.AvailableBytes;

            public void Dispose()
        {
            _reader.Clear();
        }

            public void Read<T>(out T result) where T : INetworkSerializable, new()
            {
                result = new T();
                result.Deserialize(this);
            }

            public bool ReadBool()
            {
                return _reader.GetBool();
            }

            public byte ReadByte()
            {
                return _reader.GetByte();
            }

            public byte[] ReadBytes(int length)
            {
                return _reader.GetBytesWithLength();
            }

            public char ReadChar()
            {
                return _reader.GetChar();
            }

            public double ReadDouble()
            {
                return _reader.GetDouble();
            }

            public double[] ReadDoubleArray()
            {
                return _reader.GetDoubleArray();
            }

            public float ReadFloat()
            {
                return _reader.GetFloat();
            }

            public float[] ReadFloatArray()
            {
                return _reader.GetFloatArray();
            }

            public int ReadInt()
            {
                return _reader.GetInt();
            }

            public int[] ReadIntArray()
            {
                return _reader.GetIntArray();
            }

            public long ReadLong()
            {
                return _reader.GetLong();
            }

            public long[] ReadLongArray()
            {
                return _reader.GetLongArray();
            }

            public sbyte ReadSByte()
            {
                return _reader.GetSByte();
            }

            public short ReadShort()
            {
                return _reader.GetShort();
            }

            public string ReadString()
            {
                return _reader.GetString();
            }

            public string[] ReadStringArray()
            {
                return _reader.GetStringArray();
            }

            public uint ReadUInt()
            {
                return _reader.GetUInt();
            }

            public ulong ReadULong()
            {
                return _reader.GetULong();
            }

            public ushort ReadUShort()
            {
                return _reader.GetUShort();
            }

            public byte[] ReadByteArray()
            {
                ushort length = ReadUShort();
                byte[] result = new byte[length];
                Buffer.BlockCopy(_reader.RawData, _reader.Position, result, 0, length);
                _reader.SetPosition(_reader.Position + length);
                return result;
            }

            byte[] INetworkReader.ReadByteArray()
            {
                throw new NotImplementedException();
            }

            // Implementação dos métodos de INetworkReader

        }

        private class LiteNetLibLoggerAdapter : LiteNetLib.INetLogger
        {
            private readonly ILogger _logger;
            public LiteNetLibLoggerAdapter(ILogger logger) => _logger = logger;
            public void WriteNet(NetLogLevel level, string str, params object[] args)
            {
                var prefix = "\u001b[31m{LiteNetLib - Internal}\u001b[0m: "; // Red color prefix
                var message = prefix + string.Format(str, args);

                switch (level)
                {
                    case NetLogLevel.Info:
                        _logger.LogInformation(message);
                        break;
                    case NetLogLevel.Warning:
                        _logger.LogWarning(message);
                        break;
                    case NetLogLevel.Error:
                        _logger.LogError(message);
                        break;
                    case NetLogLevel.Trace:
                    default:
                        _logger.LogDebug(message);
                        break;
                }
            }
        }
    }
}
