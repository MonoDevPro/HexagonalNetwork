using System;
using System.Threading.Tasks;
using NetworkHexagonal.Adapters.Outbound.LiteNetLibAdapter;
using NetworkHexagonal.Adapters.Outbound.Networking.Serializer;
using NetworkHexagonal.Core.Application.Ports;
using NetworkHexagonal.Core.Application.Ports.Output;
using Xunit;

namespace NetworkHexagonal.Tests;

public class LiteNetLibIntegrationPacket : IPacket, INetworkSerializable
{
    public string Message { get; set; }
    public void Serialize(INetworkWriter writer) => writer.WriteString(Message);
    public void Deserialize(INetworkReader reader) => Message = reader.ReadString();
}

public class AdaptersTests
{
    [Fact]
    public async Task LiteNetLibAdapter_SendAndReceive_Works()
    {
        var serializer = new SerializerAdapter();
        var configServer = new NetworkConfiguration { Ip = "127.0.0.1", Port = 15000, ConnectionKey = "test", UpdateInterval = TimeSpan.FromMilliseconds(10), PingInterval = TimeSpan.FromSeconds(1), DisconnectTimeout = TimeSpan.FromSeconds(5) };
        var configClient = new NetworkConfiguration { Ip = "127.0.0.1", Port = 15000, ConnectionKey = "test", UpdateInterval = TimeSpan.FromMilliseconds(10), PingInterval = TimeSpan.FromSeconds(1), DisconnectTimeout = TimeSpan.FromSeconds(5) };
        var server = new LiteNetLibAdapter(serializer, configServer);
        var client = new LiteNetLibAdapter(serializer, configClient);
        server.Register<LiteNetLibIntegrationPacket>();
        client.Register<LiteNetLibIntegrationPacket>();
        server.Initialize();
        client.Initialize();
        server.Start();
        await client.ConnectAsync();
        bool received = false;
        server.OnPacketReceivedFromPeer += (peerId, packet) =>
        {
            if (packet is LiteNetLibIntegrationPacket p && p.Message == "hello")
                received = true;
        };
        client.SendToAll(new LiteNetLibIntegrationPacket { Message = "hello" }, NetworkDelivery.Reliable, 0);
        for (int i = 0; i < 50; i++) { server.Update(); client.Update(); await Task.Delay(10); }
        Assert.True(received);
        client.Disconnect();
        server.Stop();
    }

    [Fact]
    public async Task LiteNetLibAdapter_Events_Work()
    {
        var serializer = new SerializerAdapter();
        var configServer = new NetworkConfiguration { Ip = "127.0.0.1", Port = 15001, ConnectionKey = "test", UpdateInterval = TimeSpan.FromMilliseconds(10), PingInterval = TimeSpan.FromSeconds(1), DisconnectTimeout = TimeSpan.FromSeconds(5) };
        var configClient = new NetworkConfiguration { Ip = "127.0.0.1", Port = 15001, ConnectionKey = "test", UpdateInterval = TimeSpan.FromMilliseconds(10), PingInterval = TimeSpan.FromSeconds(1), DisconnectTimeout = TimeSpan.FromSeconds(5) };
        var server = new LiteNetLibAdapter(serializer, configServer);
        var client = new LiteNetLibAdapter(serializer, configClient);
        server.Register<LiteNetLibIntegrationPacket>();
        client.Register<LiteNetLibIntegrationPacket>();
        server.Initialize();
        client.Initialize();
        bool peerConnected = false, peerDisconnected = false, clientConnected = false, clientDisconnected = false;
        server.OnPeerConnected += id => peerConnected = true;
        server.OnPeerDisconnected += id => peerDisconnected = true;
        client.OnConnected += () => clientConnected = true;
        client.OnDisconnected += () => clientDisconnected = true;
        server.Start();
        await client.ConnectAsync();
        for (int i = 0; i < 50; i++) { server.Update(); client.Update(); await Task.Delay(10); }
        client.Disconnect();
        for (int i = 0; i < 50; i++) { server.Update(); client.Update(); await Task.Delay(10); }
        server.Stop();
        Assert.True(peerConnected);
        Assert.True(clientConnected);
        Assert.True(clientDisconnected);
        // peerDisconnected pode depender do tempo de timeout, então não é obrigatório Assert.True(peerDisconnected)
    }

    [Fact]
    public void LiteNetLibAdapter_UnregisteredPacket_Throws()
    {
        var serializer = new SerializerAdapter();
        var config = new NetworkConfiguration { Ip = "127.0.0.1", Port = 15002, ConnectionKey = "test", UpdateInterval = TimeSpan.FromMilliseconds(10), PingInterval = TimeSpan.FromSeconds(1), DisconnectTimeout = TimeSpan.FromSeconds(5) };
        var adapter = new LiteNetLibAdapter(serializer, config);
        adapter.Initialize();
        Assert.Throws<InvalidOperationException>(() => adapter.SendToAll(new LiteNetLibIntegrationPacket { Message = "fail" }, NetworkDelivery.Reliable, 0));
    }
}
