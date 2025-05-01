namespace NetworkHexagonal.Core.Application.Ports.Output
{
    public enum NetworkDelivery
    {
        Reliable,
        Unreliable,
        Sequenced
        // Outros métodos conforme necessidade
    }

    public interface IPacketSender
    {
        void SendTo(int peerId, IPacket packet, NetworkDelivery method, byte channel);
        void SendToAll(IPacket packet, NetworkDelivery method, byte channel);
        void SendToAllExcept(int excludedPeerId, IPacket packet, NetworkDelivery method, byte channel);
    }
}