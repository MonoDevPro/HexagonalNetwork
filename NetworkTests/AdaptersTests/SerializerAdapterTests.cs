using NetworkHexagonal.Adapters.Outbound.Networking.Serializer;
using NetworkHexagonal.Core.Application.Ports;
using System.Text;

namespace NetworkHexagonal.Tests.AdaptersTests;

public class TestPacket : IPacket, INetworkSerializable
{
    public int IntValue { get; set; }
    public string StringValue { get; set; } = string.Empty;
    public float FloatValue { get; set; }
    public bool BoolValue { get; set; }
    public int[] ArrayValue { get; set; } = Array.Empty<int>();

    public void Serialize(INetworkWriter writer)
    {
        writer.Write(IntValue);
        writer.Write(StringValue);
        writer.Write(FloatValue);
        writer.Write(BoolValue);
        writer.WriteArray(ArrayValue);
    }

    public void Deserialize(INetworkReader reader)
    {
        IntValue = reader.ReadInt();
        StringValue = reader.ReadString();
        FloatValue = reader.ReadFloat();
        BoolValue = reader.ReadBool();
        ArrayValue = reader.ReadIntArray();
    }
}

public class SerializerAdapterTests
{
    private readonly SerializerAdapter _serializer;

    public SerializerAdapterTests()
    {
        _serializer = new SerializerAdapter();
    }

    [Fact]
    public void Serialize_Deserialize_SimpleValues_Works()
    {
        // Arrange
        var packet = new TestPacket
        {
            IntValue = 42,
            StringValue = "Test String",
            FloatValue = 3.14f,
            BoolValue = true
        };

        // Act
        var bytes = _serializer.Serialize(packet);
        var deserialized = _serializer.Deserialize<TestPacket>(bytes);

        // Assert
        Assert.Equal(packet.IntValue, deserialized.IntValue);
        Assert.Equal(packet.StringValue, deserialized.StringValue);
        Assert.Equal(packet.FloatValue, deserialized.FloatValue);
        Assert.Equal(packet.BoolValue, deserialized.BoolValue);
    }

    [Fact]
    public void Serialize_Deserialize_Arrays_Works()
    {
        // Arrange
        var packet = new TestPacket
        {
            IntValue = 123,
            StringValue = "Array Test",
            FloatValue = 2.71f,
            BoolValue = false,
            ArrayValue = new[] { 1, 2, 3, 4, 5 }
        };

        // Act
        var bytes = _serializer.Serialize(packet);
        var deserialized = _serializer.Deserialize<TestPacket>(bytes);

        // Assert
        Assert.Equal(packet.ArrayValue.Length, deserialized.ArrayValue.Length);
        for (int i = 0; i < packet.ArrayValue.Length; i++)
        {
            Assert.Equal(packet.ArrayValue[i], deserialized.ArrayValue[i]);
        }
    }

    [Fact]
    public void Serialize_Deserialize_EmptyValues_Works()
    {
        // Arrange
        var packet = new TestPacket
        {
            IntValue = 0,
            StringValue = string.Empty,
            FloatValue = 0f,
            BoolValue = false,
            ArrayValue = Array.Empty<int>()
        };

        // Act
        var bytes = _serializer.Serialize(packet);
        var deserialized = _serializer.Deserialize<TestPacket>(bytes);

        // Assert
        Assert.Equal(packet.IntValue, deserialized.IntValue);
        Assert.Equal(packet.StringValue, deserialized.StringValue);
        Assert.Equal(packet.FloatValue, deserialized.FloatValue);
        Assert.Equal(packet.BoolValue, deserialized.BoolValue);
        Assert.Empty(deserialized.ArrayValue);
    }

    [Fact]
    public void Serialize_Deserialize_LargeString_Works()
    {
        // Arrange
        var sb = new StringBuilder();
        for (int i = 0; i < 1000; i++)
        {
            sb.Append($"String content {i}, ");
        }
        
        var packet = new TestPacket
        {
            IntValue = 999,
            StringValue = sb.ToString(),
            FloatValue = 999.999f,
            BoolValue = true
        };

        // Act
        var bytes = _serializer.Serialize(packet);
        var deserialized = _serializer.Deserialize<TestPacket>(bytes);

        // Assert
        Assert.Equal(packet.StringValue, deserialized.StringValue);
    }
}