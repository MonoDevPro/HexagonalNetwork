namespace NetworkHexagonal.Core.Application.Ports
{
    public interface INetworkWriter
    {
        public void Write<T>(T obj) where T : INetworkSerializable;
        void Write(int value);
        void Write(long value);
        void Write(short value);
        void Write(ushort value);
        void Write(uint value);
        void Write(ulong value);
        void Write(float value);
        void Write(double value);
        void Write(char value);
        void Write(bool value);
        void Write(string value);
        void WriteArray(byte[] data);
        void WriteArray(int[] values);
        void WriteArray(float[] values);
        void WriteArray(long[] values);
        void WriteArray(double[] values);
        void WriteArray(string[] values);
        // Adicione outros métodos conforme necessidade
    }
}
