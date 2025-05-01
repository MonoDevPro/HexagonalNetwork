using LiteNetLib;
using LiteNetLib.Utils;
using NetworkHexagonal.Adapters.Outbound.Networking.Serializer;
using NetworkHexagonal.Core.Application.Ports;
using NetworkHexagonal.Core.Application.Ports.Input;
using NetworkHexagonal.Core.Application.Ports.Output;

namespace NetworkHexagonal.Adapters.Outbound.LiteNetLibAdapter
{
    public class LiteNetLibAdapter : INetworkService, IPacketSender, IPacketRegistry, IClientNetworkService, IServerNetworkService
    {
        private readonly INetworkSerializer _serializer;
        private readonly INetworkConfiguration _configuration;
        private NetManager? _netManager;
        private EventBasedNetListener? _listener;
        private readonly Dictionary<Type, ushort> _packetTypeToId = new();
        private readonly Dictionary<ushort, Func<IPacket>> _idToPacketFactory = new();
        private ushort _nextPacketId = 1;

        // Eventos de Client
        public event Action? OnConnected;
        public event Action? OnDisconnected;
        public event Action<IPacket>? OnPacketReceived;
        // Eventos de Server
        public event Action<int>? OnPeerConnected;
        public event Action<int>? OnPeerDisconnected;
        public event Action<int, IPacket> OnPacketReceivedFromPeer;

        public LiteNetLibAdapter(INetworkSerializer serializer, INetworkConfiguration configuration)
        {
            _serializer = serializer;
            _configuration = configuration;
        }

        // INetworkService
        public void Initialize()
        {
            _listener = new EventBasedNetListener();
            _netManager = new NetManager(_listener)
            {
                DisconnectTimeout = (int)_configuration.DisconnectTimeout.TotalMilliseconds,
                UpdateTime = (int)_configuration.UpdateInterval.TotalMilliseconds,
                PingInterval = (int)_configuration.PingInterval.TotalMilliseconds
            };

            // Eventos de conexão, recebimento, etc. podem ser registrados aqui
        }

        public void Configure(INetworkConfiguration config)
        {
            // Atualiza configurações em runtime (hot-reload)
            // Exemplo: _netManager.DisconnectTimeout = (int)config.DisconnectTimeout.TotalMilliseconds;
        }

        public void Dispose()
        {
            _netManager?.Stop();
            _netManager = null;
            _listener = null;
        }

        // IPacketSender
        public void SendTo(int peerId, IPacket packet, NetworkDelivery method, byte channel)
        {
            var peer = _netManager?.ConnectedPeerList?.Find(p => p.Id == peerId);
            if (peer == null) return;
            var data = SerializePacket(packet);
            peer.Send(data, (DeliveryMethod)method);
        }

        public void SendToAll(IPacket packet, NetworkDelivery method, byte channel)
        {
            var data = SerializePacket(packet);
            foreach (var peer in _netManager.ConnectedPeerList)
                peer.Send(data, (LiteNetLib.DeliveryMethod)method);
        }

        public void SendToAllExcept(int excludedPeerId, IPacket packet, NetworkDelivery method, byte channel)
        {
            var data = SerializePacket(packet);
            foreach (var peer in _netManager.ConnectedPeerList)
                if (peer.Id != excludedPeerId)
                    peer.Send(data, (LiteNetLib.DeliveryMethod)method);
        }

        // IPacketRegistry
        public void Register<TPacket>() where TPacket : IPacket, new()
        {
            var type = typeof(TPacket);
            if (_packetTypeToId.ContainsKey(type)) return;
            var id = _nextPacketId++;
            _packetTypeToId[type] = id;
            _idToPacketFactory[id] = () => new TPacket();
        }

        private byte[] SerializePacket(IPacket packet)
        {
            var type = packet.GetType();
            if (!_packetTypeToId.TryGetValue(type, out var id))
                throw new InvalidOperationException($"Packet type {type.Name} not registered.");
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(id);
            var payload = _serializer.Serialize(packet);
            bw.Write(payload.Length);
            bw.Write(payload);
            return ms.ToArray();
        }

        private (ushort id, byte[] payload) DeserializePacketHeader(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var br = new BinaryReader(ms);
            var id = br.ReadUInt16();
            var length = br.ReadInt32();
            var payload = br.ReadBytes(length);
            return (id, payload);
        }

        public async Task ConnectAsync(int timeoutMs = 5000)
        {
            if (_netManager == null || _listener == null)
                throw new InvalidOperationException("Adapter not initialized");
            var tcs = new TaskCompletionSource<bool>();
            _listener.PeerConnectedEvent += (peer) =>
            {
                OnConnected?.Invoke();
                tcs.TrySetResult(true);
            };
            _listener.NetworkErrorEvent += (endPoint, error) =>
            {
                OnDisconnected?.Invoke();
                tcs.TrySetResult(false);
            };
            _netManager.Start();
            _netManager.Connect(_configuration.Ip, _configuration.Port, _configuration.ConnectionKey);
            await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
        }

        public void Disconnect()
        {
            _netManager?.DisconnectAll();
            OnDisconnected?.Invoke();
        }

        public void Start()
        {
            if (_netManager == null || _listener == null)
                throw new InvalidOperationException("Adapter not initialized");
            _listener.PeerConnectedEvent += (peer) =>
            {
                OnPeerConnected?.Invoke(peer.Id);
            };
            _listener.PeerDisconnectedEvent += (peer, info) =>
            {
                OnPeerDisconnected?.Invoke(peer.Id);
            };
            _listener.NetworkReceiveEvent += (peer, reader, method) =>
            {
                var data = reader.GetRemainingBytes();
                var (id, payload) = DeserializePacketHeader(data);
                if (_idToPacketFactory.TryGetValue(id, out var factory))
                {
                    var packet = factory();
                    // Crie um INetworkReader apenas para o payload
                    using (var ms = new MemoryStream(payload))
                    using (var networkReader = new BufferNetworkReader(ms))
                    {
                        packet.Deserialize(networkReader);
                    }
                    OnPacketReceivedFromPeer?.Invoke(peer.Id, packet); // Server
                    OnPacketReceived?.Invoke(packet); // Client
                }
            };
            _netManager.Start(_configuration.Port);
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
            _netManager?.PollEvents();
        }

        // Métodos e eventos auxiliares para integração com LiteNetLib podem ser adicionados aqui
    
    
    private class LiteNetLibReaderAdapter : INetworkReader, IDisposable
    {
        private readonly NetDataReader _reader;

        public LiteNetLibReaderAdapter(NetDataReader netDataReader)
        {
            _reader = netDataReader;
        }

        public void Dispose()
        {
            _reader.Clear();
        }

            public bool ReadBool()
            {
                return _reader.GetBool();
            }

            public byte[] ReadBytes(int length)
            {
                return _reader.GetBytesWithLength();
            }

            public char ReadChar()
            {
                return _reader.GetChar();
            }

            public decimal ReadDecimal()
            {
                return decimal.Zero; // TODO: LiteNetLib não suporta decimal diretamente
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

            // Implementação dos métodos de INetworkReader

        }
    }
}
