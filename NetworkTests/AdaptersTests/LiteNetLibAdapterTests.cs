using LiteNetLib;
using Microsoft.Extensions.Logging;
using Moq;
using NetworkHexagonal.Adapters.Outbound.LiteNetLibAdapter;
using NetworkHexagonal.Adapters.Outbound.Networking.Packet;
using NetworkHexagonal.Adapters.Outbound.Networking.Serializer;
using NetworkHexagonal.Core.Application.Ports;
using NetworkHexagonal.Core.Application.Ports.Output;
using System.Net;

namespace NetworkHexagonal.Tests.AdaptersTests;

public class ClientToServerPacket : IPacket
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;

    public void Serialize(INetworkWriter writer)
    {
        writer.Write(Id);
        writer.Write(Message);
    }

    public void Deserialize(INetworkReader reader)
    {
        Id = reader.ReadInt();
        Message = reader.ReadString();
    }
}

public class ClientToServerPacket2 : IPacket
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;

    public void Serialize(INetworkWriter writer)
    {
        writer.Write(Id);
        writer.Write(Message);
    }

    public void Deserialize(INetworkReader reader)
    {
        Id = reader.ReadInt();
        Message = reader.ReadString();
    }
}

public class ServerToClientPacket : IPacket
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;

    public void Serialize(INetworkWriter writer)
    {
        writer.Write(Id);
        writer.Write(Message);
    }

    public void Deserialize(INetworkReader reader)
    {
        Id = reader.ReadInt();
        Message = reader.ReadString();
    }
}

public class ServerToClientPacket2 : IPacket
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;

    public void Serialize(INetworkWriter writer)
    {
        writer.Write(Id);
        writer.Write(Message);
    }

    public void Deserialize(INetworkReader reader)
    {
        Id = reader.ReadInt();
        Message = reader.ReadString();
    }
}

// Testes unitários para LiteNetLibAdapter
public class LiteNetLibAdapterUnitTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly INetworkSerializer _serializer;
    private readonly IPacketCallbacks _callbacks;
    private readonly INetworkConfiguration _configuration;

    public LiteNetLibAdapterUnitTests()
    {
        _loggerMock = new Mock<ILogger>();
        _serializer = new SerializerAdapter();
        _callbacks = new PacketCallback(_loggerMock.Object);
        _configuration = new NetworkConfiguration
        {
            Ip = "127.0.0.1",
            Port = 9050, // Porta diferente para evitar conflitos em testes
            ConnectionKey = "test_key",
            UpdateInterval = TimeSpan.FromMilliseconds(15),
            PingInterval = TimeSpan.FromMilliseconds(100),
            DisconnectTimeout = TimeSpan.FromMilliseconds(500),
            UnsyncedEvents = true
        };
    }

    [Fact]
    public void Initialize_ConfiguresAdapter_Correctly()
    {
        // Arrange & Act
        using var adapter = new LiteNetLibAdapter(_serializer, _callbacks, _configuration, _loggerMock.Object);
        adapter.Initialize();

        // Assert - se não lançar exceção, está inicializado corretamente
        Assert.NotNull(adapter);
    }

    [Fact]
    public void Register_AddsPacketToRegistry()
    {
        // Arrange
        using var adapter = new LiteNetLibAdapter(_serializer, _callbacks, _configuration, _loggerMock.Object);
        adapter.Initialize();

        // Act & Assert - se não lançar exceção, o registro foi realizado
        adapter.Register<ClientToServerPacket>((packet) => {  });
    }

    [Fact]
    public void SerializePacket_ThrowsException_WhenPacketNotRegistered()
    {
        // Arrange
        using var adapter = new LiteNetLibAdapter(_serializer, _callbacks, _configuration, _loggerMock.Object);
        adapter.Initialize();
        var packet = new ClientToServerPacket { Id = 1, Message = "Test" };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => adapter.SendToAll(packet, NetworkDelivery.Reliable, 0));
    }
}

// Testes de integração para o LiteNetLibAdapter (cliente/servidor)
public class LiteNetLibAdapterIntegrationTests : IDisposable
{
    private readonly INetworkSerializer _serializer;
    private readonly INetworkConfiguration _serverConfig;
    private readonly INetworkConfiguration _clientConfig;
    private readonly ILogger _logger;
    private LiteNetLibAdapter _serverAdapter;
    private LiteNetLibAdapter _clientAdapter;
    private LiteNetLibAdapter _clientAdapter2; // Segundo cliente para testes de múltiplos clientes
    private readonly AutoResetEvent _connectionEvent;
    private readonly AutoResetEvent _packetReceivedEvent;
    private readonly AutoResetEvent _serverPacketReceivedEvent;
    private readonly AutoResetEvent _clientPacketReceivedEvent;
    private readonly AutoResetEvent _client2PacketReceivedEvent;
    private ServerToClientPacket _lastServerReceivedPacket;
    private ServerToClientPacket2 _lastServerReceivedPacket2;
    private ClientToServerPacket _lastClientReceivedPacket;
    private ClientToServerPacket2 _lastClient2ReceivedPacket;
    private int _lastPeerId;
    private int _connectedClientsCount = 0;

    public LiteNetLibAdapterIntegrationTests()
    {
        // Configuração para os testes
        _serializer = new SerializerAdapter();
        var factory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = factory.CreateLogger<LiteNetLibAdapterIntegrationTests>();

        int testPort = 9060; // Porta específica para testes
        
        _serverConfig = new NetworkConfiguration
        {
            Ip = "127.0.0.1",
            Port = testPort,
            ConnectionKey = "integration_test_key",
            UpdateInterval = TimeSpan.FromMilliseconds(10),
            PingInterval = TimeSpan.FromMilliseconds(100),
            DisconnectTimeout = TimeSpan.FromMilliseconds(500),
            UnsyncedEvents = true
        };

        _clientConfig = new NetworkConfiguration
        {
            Ip = "127.0.0.1",
            Port = testPort,
            ConnectionKey = "integration_test_key",
            UpdateInterval = TimeSpan.FromMilliseconds(10),
            PingInterval = TimeSpan.FromMilliseconds(100),
            DisconnectTimeout = TimeSpan.FromMilliseconds(500),
            UnsyncedEvents = true
        };

        _connectionEvent = new AutoResetEvent(false);
        _packetReceivedEvent = new AutoResetEvent(false);
        _serverPacketReceivedEvent = new AutoResetEvent(false);
        _clientPacketReceivedEvent = new AutoResetEvent(false);
        _client2PacketReceivedEvent = new AutoResetEvent(false);
    }

    [Fact]
    public async Task ClientServer_CanConnectAndSendPackets()
    {
        // Arrange
        SetupServerAdapter();
        SetupClientAdapter();
        _serverAdapter.Register<ClientToServerPacket>( serverPacketReceived);
        _clientAdapter.Register<ServerToClientPacket>(clientPacketReceived);

        void serverPacketReceived(ClientToServerPacket packet)
        {
            _lastClientReceivedPacket = packet;
            _serverPacketReceivedEvent.Set();
        }

        void clientPacketReceived(ServerToClientPacket packet)
        {
            _lastServerReceivedPacket = packet;
            _clientPacketReceivedEvent.Set();
        }

        // Start server
        _serverAdapter.Start();

        // Act
        await _clientAdapter.ConnectAsync(1000);
        
        // Espera a conexão ser estabelecida
        bool connected = _connectionEvent.WaitOne(1000);
        Assert.True(connected, "Client failed to connect to server");

        // Envia um pacote do cliente para o servidor
        var testPacket = new ClientToServerPacket { Id = 42, Message = "Integration Test" };
        _clientAdapter.SendToAll(testPacket, NetworkDelivery.Reliable, 0);
        
        // Espera o pacote ser recebido
        bool received = _serverPacketReceivedEvent.WaitOne(1000);
        
        // Assert
        Assert.True(received, "Server did not receive the packet");
        Assert.NotNull(_lastServerReceivedPacket);
        Assert.Equal(42, _lastServerReceivedPacket.Id);
        Assert.Equal("Integration Test", _lastServerReceivedPacket.Message);
    }
    
    [Fact]
    public async Task BidirectionalCommunication_Works()
    {
        // Arrange
        SetupServerAdapter();
        SetupClientAdapter();
        _serverAdapter.Register<ClientToServerPacket>(serverPacketReceived);
        _clientAdapter.Register<ServerToClientPacket>(clientPacketReceived);

        void serverPacketReceived(ClientToServerPacket packet)
        {
            _lastClientReceivedPacket = packet;
            _serverPacketReceivedEvent.Set();
        }

        void clientPacketReceived(ServerToClientPacket packet)
        {
            _lastServerReceivedPacket = packet;
            _clientPacketReceivedEvent.Set();
        }

        // Start server
        _serverAdapter.Start();

        // Connect client
        await _clientAdapter.ConnectAsync(1000);
        bool connected = _connectionEvent.WaitOne(1000);
        Assert.True(connected, "Client failed to connect to server");
        
        // Envia um pacote do cliente para o servidor
        var clientPacket = new ClientToServerPacket { Id = 100, Message = "Client to Server" };
        _clientAdapter.SendToAll(clientPacket, NetworkDelivery.Reliable, 0);
        
        // Verifica se o servidor recebe
        bool serverReceived = _serverPacketReceivedEvent.WaitOne(1000);
        Assert.True(serverReceived, "Server did not receive client packet");
        Assert.Equal(100, _lastServerReceivedPacket.Id);
        Assert.Equal("Client to Server", _lastServerReceivedPacket.Message);
        
        // Envia resposta do servidor para o cliente
        var serverPacket = new ServerToClientPacket { Id = 200, Message = "Server to Client" };
        _serverAdapter.SendTo(_lastPeerId, serverPacket, NetworkDelivery.Reliable, 0);
        
        // Verifica se o cliente recebe
        bool clientReceived = _clientPacketReceivedEvent.WaitOne(1000);
        Assert.True(clientReceived, "Client did not receive server packet");
        Assert.Equal(200, _lastClientReceivedPacket.Id);
        Assert.Equal("Server to Client", _lastClientReceivedPacket.Message);
    }
    
    [Fact]
    public async Task MultipleClients_CanConnectAndCommunicate()
    {
        // Arrange
        SetupServerAdapter();
        SetupClientAdapter();
        SetupClientAdapter2();
        _serverAdapter.Register<ClientToServerPacket>(serverPacketReceived);
        _serverAdapter.Register<ClientToServerPacket2>(serverPacketReceived2);
        _clientAdapter.Register<ServerToClientPacket>(clientPacketReceived);
        _clientAdapter2.Register<ServerToClientPacket2>(client2PacketReceived);

        void serverPacketReceived(ClientToServerPacket packet)
        {
            _lastClientReceivedPacket = packet;
            _serverPacketReceivedEvent.Set();
        }
        void serverPacketReceived2(ClientToServerPacket2 packet)
        {
            _lastClient2ReceivedPacket = packet;
            _serverPacketReceivedEvent.Set();
        }

        void clientPacketReceived(ServerToClientPacket packet)
        {
            _lastServerReceivedPacket = packet;
            _clientPacketReceivedEvent.Set();
        }

        void client2PacketReceived(ServerToClientPacket2 packet)
        {
            _lastServerReceivedPacket2 = packet;
            _client2PacketReceivedEvent.Set();
        }
        
        // Reset count for this test
        _connectedClientsCount = 0;
        
        // Start server
        _serverAdapter.Start();
        
        // Connect first client
        await _clientAdapter.ConnectAsync(1000);
        // Connect second client
        await _clientAdapter2.ConnectAsync(1000);
        
        // Espera ambos os clientes se conectarem
        bool allConnected = await Task.Run(() => {
            int timeout = 0;
            while (_connectedClientsCount < 2 && timeout < 200)
            {
                Thread.Sleep(10);
                timeout++;
            }
            return _connectedClientsCount >= 2;
        });
        
        Assert.True(allConnected, "Failed to connect multiple clients");
        
        // Envia mensagem de broadcast do servidor para todos
        var broadcastPacket = new ServerToClientPacket { Id = 300, Message = "Server Broadcast" };
        _serverAdapter.SendToAll(broadcastPacket, NetworkDelivery.Reliable, 0);
        
        // Verifica se ambos os clientes receberam
        bool client1Received = _clientPacketReceivedEvent.WaitOne(1000);
        Assert.True(client1Received, "Client 1 did not receive broadcast");
        Assert.Equal("Server Broadcast", _lastClientReceivedPacket.Message);
        
        bool client2Received = _client2PacketReceivedEvent.WaitOne(1000);
        Assert.True(client2Received, "Client 2 did not receive broadcast");
        Assert.Equal("Server Broadcast", _lastClient2ReceivedPacket.Message);
    }

    [Fact]
    public async Task Disconnection_WorksCorrectly()
    {
        // Arrange
        var disconnectionEvent = new AutoResetEvent(false);
        
        SetupServerAdapter();
        SetupClientAdapter();
        _serverAdapter.Register<ClientToServerPacket>(serverPacketReceived);
        _clientAdapter.Register<ServerToClientPacket>(serverPacketReceived2);

        void serverPacketReceived(ClientToServerPacket packet)
        {
            _lastClientReceivedPacket = packet;
            _serverPacketReceivedEvent.Set();
        }
        void serverPacketReceived2(ServerToClientPacket packet)
        {
            _lastServerReceivedPacket = packet;
            _serverPacketReceivedEvent.Set();
        }
        
        // Start server
        _serverAdapter.Start();
        
        // Connect client
        await _clientAdapter.ConnectAsync(1000);
        bool connected = _connectionEvent.WaitOne(1000);
        Assert.True(connected, "Client failed to connect to server");
        
        // Configura evento de desconexão
        _serverAdapter.OnPeerDisconnected += (peerId) => {
            disconnectionEvent.Set();
        };
        
        // Desconecta o cliente
        _clientAdapter.Disconnect();
        
        // Verifica se o servidor detecta a desconexão
        bool disconnected = disconnectionEvent.WaitOne(1000);
        Assert.True(disconnected, "Server did not detect client disconnection");
    }

    private void SetupServerAdapter()
    {
        var callbacks = new PacketCallback(_logger);

        _serverAdapter = new LiteNetLibAdapter(_serializer, callbacks, _serverConfig, _logger);
        _serverAdapter.Initialize();

        _serverAdapter.OnPeerConnected += (peerId) =>
        {
            _lastPeerId = peerId;
            _connectionEvent.Set();
            _connectedClientsCount++;
        };
    }

    private void SetupClientAdapter()
    {
        var callbacks = new PacketCallback(_logger);

        _clientAdapter = new LiteNetLibAdapter(_serializer, callbacks, _clientConfig, _logger);
        _clientAdapter.Initialize();
        
        _clientAdapter.OnConnected += () =>
        {
            _connectionEvent.Set();
        };
    }
    
    private void SetupClientAdapter2()
    {
        var callbacks = new PacketCallback(_logger);

        _clientAdapter2 = new LiteNetLibAdapter(_serializer, callbacks, _clientConfig, _logger);
        _clientAdapter2.Initialize();
    }

    public void Dispose()
    {
        _clientAdapter?.Disconnect();
        _clientAdapter2?.Disconnect();
        _serverAdapter?.Stop();
        _clientAdapter?.Dispose();
        _clientAdapter2?.Dispose();
        _serverAdapter?.Dispose();
        
        // Pequena pausa para garantir limpeza dos recursos
        Thread.Sleep(100);
    }
}