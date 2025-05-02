
// using System.Collections.Concurrent;
// using Microsoft.Extensions.Logging;
// using NetworkHexagonal.Adapters.Outbound.LiteNetLibAdapter;
// using NetworkHexagonal.Adapters.Outbound.Networking.Serializer;
// using NetworkHexagonal.Core.Application.Ports;
// using NetworkHexagonal.Core.Application.Ports.Input;
// using NetworkHexagonal.Core.Application.Ports.Output;

public static class Program
{
//     private static ILogger<LiteNetLibIntegrationPacket> _logger;
//     private static INetworkSerializer _serializer;
//     private static INetworkConfiguration _config;

//     private static LiteNetLibAdapter _serverAdapter;
//     private static LiteNetLibAdapter _clientAdapter;

//     private static IServerNetworkService _server;
//     private static IClientNetworkService _client;

//     private static Task? _updateTask;
//     private static int _updateCounts = 0;
//     private static ConcurrentBag<INetworkService> _updateTasksBag = new ConcurrentBag<INetworkService>();
//     private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

     public static async Task Main(string[] args)
     {
//         _logger = LoggerFactory.Create(
//             builder => builder
//                 .AddConsole()
//                 .SetMinimumLevel(LogLevel.Debug))
//                 .CreateLogger<LiteNetLibIntegrationPacket>();

//         _serializer = new SerializerAdapter();

//         _config = new NetworkConfiguration
//         {
//             Ip = "127.0.0.1",
//             Port = 8090,
//             ConnectionKey = "test",
//             UpdateInterval = TimeSpan.FromMilliseconds(15),
//             PingInterval = TimeSpan.FromSeconds(1),
//             DisconnectTimeout = TimeSpan.FromSeconds(5),
//             UnsyncedEvents = true
//         };

//         var serverLog = LoggerFactory.Create(
//             builder => builder
//                 .AddConsole()
//                 .SetMinimumLevel(LogLevel.Information))
//                 .CreateLogger<IServerNetworkService>();
//         var clientLog = LoggerFactory.Create(
//             builder => builder
//                 .AddConsole()
//                 .SetMinimumLevel(LogLevel.Information))
//                 .CreateLogger<IClientNetworkService>();
//         _serverAdapter = new LiteNetLibAdapter(_serializer, _config, serverLog);
//         _clientAdapter = new LiteNetLibAdapter(_serializer, _config, clientLog);

//         _updateTask = Task.Run(async() =>
//         {
//             int updateInterval = 15;
//             int ticksPerLog = (int)Math.Round(1000.0 / updateInterval);
//             _updateCounts = 0;

//             while (!_cancellationTokenSource.Token.IsCancellationRequested)
//             {
//                 foreach (var task in _updateTasksBag)
//                     task.Update();

//                 _updateCounts++;

//                 if (_updateCounts % ticksPerLog == 0)
//                 {
//                     // _logger.LogInformation(
//                     // "Services Count: {servicesCount}\n" +
//                     // "Ticks: {ticks}\n" +
//                     // "Update Counts: {count}",
//                     // _updateTasksBag.Count,
//                     // _updateCounts,
//                     // _updateCounts);
//                 }
//                 await Task.Delay(updateInterval, _cancellationTokenSource.Token);
//             }

//             _logger.LogInformation("Update loop stopped.");
//         }, _cancellationTokenSource.Token);

//         _server = _serverAdapter;
//         _client = _clientAdapter;

//         _server.Initialize();
//         _client.Initialize();

//         // Usando o adapter, pois ainda não separamos o registro de pacotes conforme o nosso domínio.
//         _serverAdapter.Register<LiteNetLibIntegrationPacket>();
//         _clientAdapter.Register<LiteNetLibIntegrationPacket>();

//         // Inicia o servidor
//         _server.Start();
//         _updateTasksBag.Add(_serverAdapter);

//         _server.OnConnectionRequest += (success, endPoint) =>
//         {
//             _logger.LogInformation("Connection request result: {sucess} from {endPoint}", success, endPoint);
//         };
//         _server.OnPeerConnected += (peerId) =>
//         {
//             _logger.LogInformation("Peer connected: {peerId}", peerId);
//         };
//         _server.OnPeerDisconnected += (peerId) =>
//         {
//             _logger.LogInformation("Peer disconnected: {peerId}", peerId);
//         };
//         _server.OnPingReceivedFromPeer += (peerId, ping) =>
//         {
//             _logger.LogInformation("Ping received from peer: {peerId} - {ping}", peerId, ping);
//         };

//         // Inicia o cliente
//         // Teste de Conexão -> Conectar o cliente ao servidor
//         bool connected = false;
//         _client.OnConnected += () =>
//         {
//             connected = true;
//             _logger.LogInformation("Client connected to server.");
//             _updateTasksBag.Add(_clientAdapter);
//         };

//         _client.OnDisconnected += (reason) =>
//         {
//             connected = false;
//             _logger.LogInformation($"Client disconnected from server. ({reason})");
//         };

//         _client.OnPingReceived += (ping) =>
//         {
//             _logger.LogInformation("Ping received from server: {ping}", ping);
//         };

//          await _client.ConnectAsync(5000);

//          if (!connected)
//          {
//              _logger.LogError("Falha ao conectar ao servidor.");
//              return;
//          }

//         // Pressione qualquer tecla para encerrar
//         await Task.Delay(-1, _cancellationTokenSource.Token);
     }
}

// public class LiteNetLibIntegrationPacket : IPacket
// {
//     public string Message { get; set; } = string.Empty;
//     public void Serialize(INetworkWriter writer) => writer.Write(Message);
//     public void Deserialize(INetworkReader reader) => Message = reader.ReadString();
// }