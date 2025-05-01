using System;
using NetworkHexagonal.Core.Application.Ports;

namespace NetworkHexagonal.Adapters.Outbound.Networking.Serializer;

public class BufferNetworkWriter : INetworkWriter, IDisposable
        {
            private readonly BinaryWriter _writer;

            public BufferNetworkWriter(Stream stream)
            {
                _writer = new BinaryWriter(stream);
            }

            public void Dispose()
            {
                _writer.Dispose();
            }

            // Implementação dos métodos de INetworkWriter
            public void WriteInt(int value) => _writer.Write(value);
            public void WriteLong(long value) => _writer.Write(value);
            public void WriteShort(short value) => _writer.Write(value);
            public void WriteUShort(ushort value) => _writer.Write(value);
            public void WriteUInt(uint value) => _writer.Write(value);
            public void WriteULong(ulong value) => _writer.Write(value);
            public void WriteFloat(float value) => _writer.Write(value);
            public void WriteDouble(double value) => _writer.Write(value);
            public void WriteDecimal(decimal value) => _writer.Write(value);
            public void WriteChar(char value) => _writer.Write(value);
            public void WriteBool(bool value) => _writer.Write(value);
            public void WriteString(string value) => _writer.Write(value ?? string.Empty);
            public void WriteBytes(byte[] data) => _writer.Write(data.Length == 0 ? Array.Empty<byte>() : data);
            public void WriteIntArray(int[] values)
            {
                _writer.Write(values?.Length ?? 0);
                if (values != null)
                    foreach (var value in values)
                        _writer.Write(value);
            }

            public void WriteFloatArray(float[] values)
            {
                _writer.Write(values?.Length ?? 0);
                if (values != null)
                    foreach (var value in values)
                        _writer.Write(value);
            }

            public void WriteLongArray(long[] values)
            {
                _writer.Write(values?.Length ?? 0);
                if (values != null)
                    foreach (var value in values)
                        _writer.Write(value);
            }

            public void WriteDoubleArray(double[] values)
            {
                _writer.Write(values?.Length ?? 0);
                if (values != null)
                    foreach (var value in values)
                        _writer.Write(value);
            }

            public void WriteStringArray(string[] values)
            {
                _writer.Write(values?.Length ?? 0);
                if (values != null)
                    foreach (var s in values)
                        _writer.Write(s ?? string.Empty);
            }
        }