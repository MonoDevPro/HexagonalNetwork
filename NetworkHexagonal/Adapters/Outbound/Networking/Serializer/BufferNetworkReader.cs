using System;
using NetworkHexagonal.Core.Application.Ports;

namespace NetworkHexagonal.Adapters.Outbound.Networking.Serializer;

public class BufferNetworkReader : INetworkReader, IDisposable
        {
            private readonly BinaryReader _reader;

            public BufferNetworkReader(Stream stream)
            {
                _reader = new BinaryReader(stream);
            }
            
            public void Dispose()
            {
                _reader.Dispose();
            }


            // Implementação dos métodos de INetworkReader
            public int ReadInt() => _reader.ReadInt32();
            public long ReadLong() => _reader.ReadInt64();
            public short ReadShort() => _reader.ReadInt16();
            public ushort ReadUShort() => _reader.ReadUInt16();
            public uint ReadUInt() => _reader.ReadUInt32();
            public ulong ReadULong() => _reader.ReadUInt64();
            public float ReadFloat() => _reader.ReadSingle();
            public double ReadDouble() => _reader.ReadDouble();
            public decimal ReadDecimal() => _reader.ReadDecimal();
            public char ReadChar() => _reader.ReadChar();
            public bool ReadBool() => _reader.ReadBoolean();
            public string ReadString() => _reader.ReadString();
            public byte[] ReadBytes(int length) => _reader.ReadBytes(length);
            public int[] ReadIntArray()
            {
                int len = _reader.ReadInt32();
                var arr = new int[len];
                for (int i = 0; i < len; i++) arr[i] = _reader.ReadInt32();
                return arr;
            }
            public float[] ReadFloatArray()
            {
                int len = _reader.ReadInt32();
                var arr = new float[len];
                for (int i = 0; i < len; i++) arr[i] = _reader.ReadSingle();
                return arr;
            }
            public long[] ReadLongArray()
            {
                int len = _reader.ReadInt32();
                var arr = new long[len];
                for (int i = 0; i < len; i++) arr[i] = _reader.ReadInt64();
                return arr;
            }
            public double[] ReadDoubleArray()
            {
                int len = _reader.ReadInt32();
                var arr = new double[len];
                for (int i = 0; i < len; i++) arr[i] = _reader.ReadDouble();
                return arr;
            }
            public string[] ReadStringArray()
            {
                int len = _reader.ReadInt32();
                var arr = new string[len];
                for (int i = 0; i < len; i++) arr[i] = _reader.ReadString();
                return arr;
            }
            // ...outros métodos conforme necessidade...
        }