using NetworkHexagonal.Core.Application.Ports;
using NetworkHexagonal.Core.Application.Ports.Output;

namespace NetworkHexagonal.Adapters.Outbound.Networking.Serializer
{
    public class SerializerAdapter : INetworkSerializer
    {
        public byte[] Serialize<T>(T obj) where T : INetworkSerializable
        {
            using (var ms = new MemoryStream())
            using (var writer = new BufferNetworkWriter(ms))
            {
                obj.Serialize(writer);
                return ms.ToArray();
            }
        }

        public T Deserialize<T>(byte[] data) where T : INetworkSerializable, new()
        {
            using (var ms = new MemoryStream(data))
            using (var reader = new BufferNetworkReader(ms))
            {
                var obj = new T();
                obj.Deserialize(reader);
                return obj;
            }
        }
    }
}
