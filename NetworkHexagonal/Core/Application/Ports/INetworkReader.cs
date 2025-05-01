namespace NetworkHexagonal.Core.Application.Ports
{
    public interface INetworkReader
    {
        int ReadInt();
        long ReadLong();
        short ReadShort();
        ushort ReadUShort();
        uint ReadUInt();
        ulong ReadULong();
        float ReadFloat();
        double ReadDouble();
        decimal ReadDecimal();
        char ReadChar();
        bool ReadBool();
        string ReadString();
        byte[] ReadBytes(int length);
        int[] ReadIntArray();
        float[] ReadFloatArray();
        long[] ReadLongArray();
        double[] ReadDoubleArray();
        string[] ReadStringArray();
        // Adicione outros métodos conforme necessidade
    }
}
