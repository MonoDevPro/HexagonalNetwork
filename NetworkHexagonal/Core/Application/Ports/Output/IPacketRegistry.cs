namespace NetworkHexagonal.Core.Application.Ports.Output
{
    public interface IPacketRegistry
    {
        void Register<TPacket>() where TPacket : IPacket, new();
    }
}