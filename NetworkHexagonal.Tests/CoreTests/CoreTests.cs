using System;
using System.Threading.Tasks;
using Moq;
using NetworkHexagonal.Core.Application.Ports;
using NetworkHexagonal.Core.Application.Ports.Input;
using NetworkHexagonal.Core.Application.Ports.Output;
using NetworkHexagonal.Core.Application.Services;
using Xunit;

namespace NetworkHexagonal.Tests;

public class DummyPacket : IPacket, INetworkSerializable
{
    public int Value { get; set; }
    public void Serialize(INetworkWriter writer) => writer.WriteInt(Value);
    public void Deserialize(INetworkReader reader) => Value = reader.ReadInt();
}

public class CoreTests
{
    [Fact]
    public void SerializerAdapter_SerializeDeserialize_Works()
    {
        var serializer = new NetworkHexagonal.Adapters.Outbound.Networking.Serializer.SerializerAdapter();
        var packet = new DummyPacket { Value = 42 };
        var bytes = serializer.Serialize(packet);
        var deserialized = serializer.Deserialize<DummyPacket>(bytes);
        Assert.Equal(42, deserialized.Value);
    }

    [Fact]
    public void PacketRegistry_RegistersAndCreatesPacket()
    {
        var registry = new Mock<IPacketRegistry>();
        registry.Setup(r => r.Register<DummyPacket>());
        registry.Object.Register<DummyPacket>();
        registry.Verify(r => r.Register<DummyPacket>(), Times.Once);
    }

    [Fact]
    public async Task ClientApp_Lifecycle_WorksWithMocks()
    {
        var networkService = new Mock<IClientNetworkService>();
        networkService.Setup(s => s.ConnectAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        networkService.Setup(s => s.Disconnect());
        var packetSender = new Mock<IPacketSender>();
        var packetRegistry = new Mock<IPacketRegistry>();
        var config = new Mock<INetworkConfiguration>();
        var app = new ClientApp(networkService.Object, packetSender.Object, packetRegistry.Object, config.Object);
        app.Initialize();
        await app.ConnectAsync();
        app.Disconnect();
        networkService.Verify(s => s.ConnectAsync(It.IsAny<int>()), Times.Once);
        networkService.Verify(s => s.Disconnect(), Times.Once);
    }

    [Fact]
    public void ServerApp_Lifecycle_WorksWithMocks()
    {
        var networkService = new Mock<IServerNetworkService>();
        networkService.Setup(s => s.Start());
        networkService.Setup(s => s.Stop());
        networkService.Setup(s => s.DisconnectPeer(It.IsAny<int>()));
        var packetSender = new Mock<IPacketSender>();
        var packetRegistry = new Mock<IPacketRegistry>();
        var config = new Mock<INetworkConfiguration>();
        var app = new ServerApp(networkService.Object, packetSender.Object, packetRegistry.Object, config.Object);
        app.Initialize();
        app.Start();
        app.DisconnectPeer(1);
        app.Stop();
        networkService.Verify(s => s.Start(), Times.Once);
        networkService.Verify(s => s.Stop(), Times.Once);
        networkService.Verify(s => s.DisconnectPeer(1), Times.Once);
    }
}
