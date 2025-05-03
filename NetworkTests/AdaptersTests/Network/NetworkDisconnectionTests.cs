using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using NetworkHexagonal.Core.Application.Ports.Outbound;
using NetworkHexagonal.Core.Domain.Events;
using NetworkHexagonal.Core.Domain.Models;
using NetworkHexagonal.Infrastructure.DependencyInjection;

namespace NetworkTests.AdaptersTests.Network
{
    [TestFixture]
    public class NetworkDisconnectionTests : IDisposable
    {
        private ServiceProvider _serviceProvider;
        private IServerNetworkService _serverService;
        private IClientNetworkService _clientService;
        private IPacketRegistry _packetRegistry;
        private IPacketSender _packetSender;
        
        private TaskCompletionSource<bool> _clientDisconnectedTcs;
        private TaskCompletionSource<bool> _serverDetectedDisconnectTcs;

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
                DisconnectTimeoutMs = 1000, // Tempo de timeout menor para os testes
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
            
            // Configura event sources para testes assíncronos
            _clientDisconnectedTcs = new TaskCompletionSource<bool>();
            _serverDetectedDisconnectTcs = new TaskCompletionSource<bool>();
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
        public async Task TestServerForcedDisconnection()
        {
            // Inicia o servidor
            bool serverStarted = _serverService.Start(9051);
            Assert.That(serverStarted, Is.True, "Server should start successfully");
            
            // Armazena o ID do cliente conectado
            int connectedPeerId = -1;
            
            // Configura handlers para o evento de conexão do servidor
            _serverService.OnPeerConnected += (ConnectionEvent e) => {
                connectedPeerId = e.PeerId;
                Console.WriteLine($"Cliente conectado ao servidor com ID: {e.PeerId}");
            };
            
            // Configura handler para evento de desconexão do cliente
            _clientService.OnDisconnected += (DisconnectionEvent e) => {
                Console.WriteLine($"Cliente detectou desconexão. Razão: {e.Reason}");
                _clientDisconnectedTcs.TrySetResult(true);
            };
            
            // Configura handler para evento de desconexão do servidor
            _serverService.OnPeerDisconnected += (DisconnectionEvent e) => {
                Console.WriteLine($"Servidor detectou desconexão do peer {e.PeerId}. Razão: {e.Reason}");
                _serverDetectedDisconnectTcs.TrySetResult(true);
            };
            
            // Conecta o cliente
            var result = await _clientService.ConnectAsync("localhost", 9051);
            Assert.That(result.Success, Is.True, "Client should connect successfully");
            
            // Espera um pouco para garantir que o evento de conexão foi processado
            await Task.Delay(100);
            
            // Verifica se o cliente foi conectado
            Assert.That(connectedPeerId, Is.GreaterThanOrEqualTo(0), 
                "Server should have assigned a valid peer ID");
            
            // Servidor desconecta o cliente forçadamente
            _serverService.DisconnectPeer(connectedPeerId);
            Console.WriteLine($"Servidor forçou desconexão do cliente {connectedPeerId}");
            
            // Aguarda o cliente detectar a desconexão com timeout de 2 segundos
            var clientTask = await Task.WhenAny(_clientDisconnectedTcs.Task, Task.Delay(2000));
            Assert.That(clientTask, Is.EqualTo(_clientDisconnectedTcs.Task), 
                "Client should detect disconnection within timeout");
            
            // Aguarda o servidor registrar a desconexão com timeout de 2 segundos
            var serverTask = await Task.WhenAny(_serverDetectedDisconnectTcs.Task, Task.Delay(2000));
            Assert.That(serverTask, Is.EqualTo(_serverDetectedDisconnectTcs.Task), 
                "Server should register the disconnection within timeout");
        }
    }
}