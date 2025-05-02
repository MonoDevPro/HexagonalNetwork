namespace NetworkHexagonal.Core.Application.Ports
{
    public interface IPacket
    {
        void Serialize(INetworkWriter writer);
        void Deserialize(INetworkReader reader);
    }
}