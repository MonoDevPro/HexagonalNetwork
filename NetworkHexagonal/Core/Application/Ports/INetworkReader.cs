namespace NetworkHexagonal.Core.Application.Ports
{
    public interface INetworkReader
    {
        bool AtEndOfStream { get; }
        long BytesLeft { get; }
        void Read<T>(out T result) where T : INetworkSerializable, new();
        byte ReadByte();
        sbyte ReadSByte();
        int ReadInt();
        long ReadLong();
        short ReadShort();
        ushort ReadUShort();
        uint ReadUInt();
        ulong ReadULong();
        float ReadFloat();
        double ReadDouble();
        char ReadChar();
        bool ReadBool();
        byte[] ReadByteArray();
        string ReadString();
        int[] ReadIntArray();
        float[] ReadFloatArray();
        long[] ReadLongArray();
        double[] ReadDoubleArray();
        string[] ReadStringArray();
        // Adicione outros métodos conforme necessidade
    }
}
