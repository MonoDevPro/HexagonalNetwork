namespace NetworkHexagonal.Core.Application.Ports
{
    /// <summary>
    /// Interface for serializable objects in the network layer.
    /// This interface is used to define the contract for objects that can be serialized and deserialized
    /// for network communication.
    /// </summary>
    public interface INetworkSerializable
    {
        void Serialize(INetworkWriter writer);
        void Deserialize(INetworkReader reader);
    }
}