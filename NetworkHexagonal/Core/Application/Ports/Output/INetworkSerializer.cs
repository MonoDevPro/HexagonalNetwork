
using NetworkHexagonal.Adapters.Outbound.Networking.Serializer;

namespace NetworkHexagonal.Core.Application.Ports.Output
{
    public interface INetworkSerializer
    {
        byte[] Serialize<T>(T obj) where T : INetworkSerializable;
        T Deserialize<T>(byte[] data) where T : INetworkSerializable, new();

        byte[] SerializePacket<T>(T obj) where T : IPacket;
        BufferNetworkReader DeserializePacket(byte[] data);
    }
}