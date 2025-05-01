namespace NetworkHexagonal.Core.Application.Ports
{
    public interface INetworkWriter
    {
        void WriteInt(int value);
        void WriteLong(long value);
        void WriteShort(short value);
        void WriteUShort(ushort value);
        void WriteUInt(uint value);
        void WriteULong(ulong value);
        void WriteFloat(float value);
        void WriteDouble(double value);
        void WriteDecimal(decimal value);
        void WriteChar(char value);
        void WriteBool(bool value);
        void WriteString(string value);
        void WriteBytes(byte[] data);
        void WriteIntArray(int[] values);
        void WriteFloatArray(float[] values);
        void WriteLongArray(long[] values);
        void WriteDoubleArray(double[] values);
        void WriteStringArray(string[] values);
        // Adicione outros métodos conforme necessidade
    }
}
