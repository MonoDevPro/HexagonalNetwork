using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Network.Core.Application.Ports.Inbound;
using Network.Core.Application.Ports.Outbound;
using Network.Core.Domain.Events;
using Network.Core.Domain.Models;
using NUnit.Framework;
using NetworkHexagonal.Infrastructure.DependencyInjection;

namespace NetworkTests.AdaptersTests.Network
{
    [TestFixture]
    public class NetworkIntegrationTest : IDisposable
    {
        private ServiceProvider _serviceProvider;
        private IServerNetworkService _serverService;
        private IClientNetworkService _clientService;
        private IPacketRegistry _packetRegistry;
        private IPacketSender _packetSender;
        private INetworkEventBus _eventBus;
        
        private TaskCompletionSource<bool> _serverReceivedPacket;
        private TaskCompletionSource<bool> _clientReceivedPacket;
        
        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            
            // Adiciona logging
            services.AddLogging(builder => {
                builder.AddConsole();
            });

            // Configura o serviço para usar UnsyncedEvents = true para testes
            services.AddSingleton<INetworkConfiguration>(provider => new NetworkConfiguration
            {
                UpdateIntervalMs = 15,
                DisconnectTimeoutMs = 5000,
                ConnectionKey = "TestConnectionKey",
                UseUnsyncedEvents = true
            });
            
            // Adiciona serviços de rede
            services.AddNetworking();
            
            _serviceProvider = services.BuildServiceProvider();
            
            // Obtém serviços necessários
            _serverService = _serviceProvider.GetRequiredService<IServerNetworkService>();
            _clientService = _serviceProvider.GetRequiredService<IClientNetworkService>();
            _packetRegistry = _serviceProvider.GetRequiredService<IPacketRegistry>();
            _packetSender = _serviceProvider.GetRequiredService<IPacketSender>();
            _eventBus = _serviceProvider.GetRequiredService<INetworkEventBus>();
            
            // Configura event sources para testes assíncronos
            _serverReceivedPacket = new TaskCompletionSource<bool>();
            _clientReceivedPacket = new TaskCompletionSource<bool>();
        }
        
        [TearDown]
        public void TearDown()
        {
            _clientService.Disconnect();
            _serverService.Stop();
            _serviceProvider?.Dispose();
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
        
        [Test]
        public async Task TestClientServerCommunication()
        {
            // Inicia o servidor
            bool serverStarted = _serverService.Start(9050);
            NUnit.Framework.Assert.That(serverStarted, Is.True, "Server should start successfully");
            
            int connectedPeerId = -1;

            // Configura handlers de eventos
            _eventBus.Subscribe<ConnectionEvent>(e => {
                connectedPeerId = e.PeerId;
            });
            
            // Registra manipuladores de pacotes
            _packetRegistry.RegisterHandler<TestPacket>((packet, context) => {
                Console.WriteLine($"Server received: {packet.TestMessage}");
                _serverReceivedPacket.TrySetResult(true);
            });
            
            _packetRegistry.RegisterHandler<ServerResponsePacket>((packet, context) => {
                Console.WriteLine($"Client received: {packet.ResponseMessage}");
                _clientReceivedPacket.TrySetResult(true);
            });
            
            // Conecta o cliente
            var result = await _clientService.ConnectAsync("localhost", 9050);
            NUnit.Framework.Assert.That(result.Success, "Client should connect successfully");
            
            // Aguarda processamento inicial
            await Task.Delay(100);
            
            // Cliente envia pacote para servidor
            var testPacket = new TestPacket { TestMessage = "Hello Server!" };
            _packetSender.SendPacket(0, testPacket); // 0 é o ID do servidor no cliente
            
            // Aguarda o servidor receber o pacote
            var serverReceived = await Task.WhenAny(_serverReceivedPacket.Task, Task.Delay(1000));
            NUnit.Framework.Assert.That(serverReceived, Is.EqualTo(_serverReceivedPacket.Task), "Server should receive packet within timeout");
            
            // Servidor responde para o cliente
            var responsePacket = new ServerResponsePacket { ResponseMessage = "Hello Client!" };
            _packetSender.SendPacket(connectedPeerId, responsePacket);
            
            // Aguarda o cliente receber a resposta
            var clientReceived = await Task.WhenAny(_clientReceivedPacket.Task, Task.Delay(1000));
            NUnit.Framework.Assert.That(clientReceived, Is.EqualTo(_clientReceivedPacket.Task), "Client should receive response packet within timeout");
        }
        
        // Classes de teste - adicionando inicialização de propriedades para resolver warnings de nullability
        public class TestPacket : IPacket, ISerializable
        {
            public string TestMessage { get; set; } = string.Empty;
            
            public void Serialize(INetworkWriter writer)
            {
                writer.WriteString(TestMessage);
            }
            
            public void Deserialize(INetworkReader reader)
            {
                TestMessage = reader.ReadString();
            }
        }
        
        public class ServerResponsePacket : IPacket, ISerializable
        {
            public string ResponseMessage { get; set; } = string.Empty;
            
            public void Serialize(INetworkWriter writer)
            {
                writer.WriteString(ResponseMessage);
            }
            
            public void Deserialize(INetworkReader reader)
            {
                ResponseMessage = reader.ReadString();
            }
        }
    }
}