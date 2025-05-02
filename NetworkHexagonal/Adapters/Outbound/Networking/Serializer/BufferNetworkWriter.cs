using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using LiteNetLib.Utils;
using NetworkHexagonal.Adapters.Outbound.Networking.Util;
using NetworkHexagonal.Core.Application.Ports;

namespace NetworkHexagonal.Adapters.Outbound.Networking.Serializer
{
    public class BufferNetworkWriter : INetworkWriter
    {
        protected byte[] _data;
        protected int _position;
        private const int InitialSize = 64;
        private readonly bool _autoResize;

        private static ObjectPool<BufferNetworkWriter> WriterPool 
            = new ObjectPool<BufferNetworkWriter>(() => new BufferNetworkWriter(), writer => writer.Reset(), 0, 10);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static BufferNetworkWriter Get()
        {
            return WriterPool.Get();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Return(BufferNetworkWriter writer)
        {
            WriterPool.Return(writer);
        }
        
        public int Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _position;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _position = value;
        }
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _data.Length;
        }
        public byte[] Data
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _data;
        }
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _position;
        }

        public static readonly ThreadLocal<UTF8Encoding> uTF8Encoding = new ThreadLocal<UTF8Encoding>(() => new UTF8Encoding(false, true));

        public BufferNetworkWriter() : this(true, InitialSize)
        {
        }

        public BufferNetworkWriter(bool autoResize) : this(autoResize, InitialSize)
        {
        }

        public BufferNetworkWriter(bool autoResize, int initialSize)
        {
            _data = new byte[initialSize];
            _autoResize = autoResize;
        }

        /// <summary>
        /// Creates NetDataWriter from existing ByteArray
        /// </summary>
        /// <param name="bytes">Source byte array</param>
        /// <param name="copy">Copy array to new location or use existing</param>
        public static BufferNetworkWriter FromBytes(byte[] bytes, bool copy)
        {
            if (copy)
            {
                var netDataWriter = new BufferNetworkWriter(true, bytes.Length);
                netDataWriter.Write(bytes);
                return netDataWriter;
            }
            return new BufferNetworkWriter(true, 0) {_data = bytes, _position = bytes.Length};
        }

        /// <summary>
        /// Creates NetDataWriter from existing ByteArray (always copied data)
        /// </summary>
        /// <param name="bytes">Source byte array</param>
        /// <param name="offset">Offset of array</param>
        /// <param name="length">Length of array</param>
        public static BufferNetworkWriter FromBytes(byte[] bytes, int offset, int length)
        {
            var netDataWriter = new BufferNetworkWriter(true, bytes.Length);
            netDataWriter.Write(bytes, offset, length);
            return netDataWriter;
        }

        public static BufferNetworkWriter FromString(string value)
        {
            var netDataWriter = new BufferNetworkWriter();
            netDataWriter.Write(value);
            return netDataWriter;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResizeIfNeed(int newSize)
        {
            if (_data.Length < newSize)
            {
                Array.Resize(ref _data, Math.Max(newSize, _data.Length * 2));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureFit(int additionalSize)
        {
            if (_data.Length < _position + additionalSize)
            {
                Array.Resize(ref _data, Math.Max(_position + additionalSize, _data.Length * 2));
            }
        }

        public void Reset(int size)
        {
            ResizeIfNeed(size);
            _position = 0;
        }

        public void Reset()
        {
            _position = 0;
        }

        public byte[] CopyData()
        {
            byte[] resultData = new byte[_position];
            Buffer.BlockCopy(_data, 0, resultData, 0, _position);
            return resultData;
        }

        /// <summary>
        /// Sets position of NetDataWriter to rewrite previous values
        /// </summary>
        /// <param name="position">new byte position</param>
        /// <returns>previous position of data writer</returns>
        public int SetPosition(int position)
        {
            int prevPosition = _position;
            _position = position;
            return prevPosition;
        }

        public void Write(float value)
        {
            if (_autoResize)
                ResizeIfNeed(_position + 4);
            FastBitConverter.GetBytes(_data, _position, value);
            _position += 4;
        }

        public void Write(double value)
        {
            if (_autoResize)
                ResizeIfNeed(_position + 8);
            FastBitConverter.GetBytes(_data, _position, value);
            _position += 8;
        }

        public void Write(long value)
        {
            if (_autoResize)
                ResizeIfNeed(_position + 8);
            FastBitConverter.GetBytes(_data, _position, value);
            _position += 8;
        }

        public void Write(ulong value)
        {
            if (_autoResize)
                ResizeIfNeed(_position + 8);
            FastBitConverter.GetBytes(_data, _position, value);
            _position += 8;
        }

        public void Write(int value)
        {
            if (_autoResize)
                ResizeIfNeed(_position + 4);
            FastBitConverter.GetBytes(_data, _position, value);
            _position += 4;
        }

        public void Write(uint value)
        {
            if (_autoResize)
                ResizeIfNeed(_position + 4);
            FastBitConverter.GetBytes(_data, _position, value);
            _position += 4;
        }

        public void Write(char value)
        {
            Write((ushort)value);
        }

        public void Write(ushort value)
        {
            if (_autoResize)
                ResizeIfNeed(_position + 2);
            FastBitConverter.GetBytes(_data, _position, value);
            _position += 2;
        }

        public void Write(short value)
        {
            if (_autoResize)
                ResizeIfNeed(_position + 2);
            FastBitConverter.GetBytes(_data, _position, value);
            _position += 2;
        }

        public void Write(sbyte value)
        {
            if (_autoResize)
                ResizeIfNeed(_position + 1);
            _data[_position] = (byte)value;
            _position++;
        }

        public void Write(byte value)
        {
            if (_autoResize)
                ResizeIfNeed(_position + 1);
            _data[_position] = value;
            _position++;
        }

        public void Write(byte[] data, int offset, int length)
        {
            if (_autoResize)
                ResizeIfNeed(_position + length);
            Buffer.BlockCopy(data, offset, _data, _position, length);
            _position += length;
        }

        public void Write(byte[] data)
        {
            if (_autoResize)
                ResizeIfNeed(_position + data.Length);
            Buffer.BlockCopy(data, 0, _data, _position, data.Length);
            _position += data.Length;
        }

        public void WriteSBytesWithLength(sbyte[] data, int offset, ushort length)
        {
            if (_autoResize)
                ResizeIfNeed(_position + 2 + length);
            FastBitConverter.GetBytes(_data, _position, length);
            Buffer.BlockCopy(data, offset, _data, _position + 2, length);
            _position += 2 + length;
        }

        public void WriteSBytesWithLength(sbyte[] data)
        {
            WriteArray(data, 1);
        }

        public void WriteBytesWithLength(byte[] data, int offset, ushort length)
        {
            if (_autoResize)
                ResizeIfNeed(_position + 2 + length);
            FastBitConverter.GetBytes(_data, _position, length);
            Buffer.BlockCopy(data, offset, _data, _position + 2, length);
            _position += 2 + length;
        }

        public void WriteBytesArray()
        {
            if (_autoResize)
                ResizeIfNeed(_position + 2);
            FastBitConverter.GetBytes(_data, _position, (ushort)0);
            _position += 2;
        }

        public void WriteArray(byte[] data)
        {
            WriteArray(data, 1);
        }

        public void Write(bool value)
        {
            Write((byte)(value ? 1 : 0));
        }

        public void WriteArray(Array arr, int sz)
        {
            ushort length = arr == null ? (ushort) 0 : (ushort)arr.Length;
            sz *= length;
            if (_autoResize)
                ResizeIfNeed(_position + sz + 2);
            FastBitConverter.GetBytes(_data, _position, length);
            if (arr != null)
                Buffer.BlockCopy(arr, 0, _data, _position + 2, sz);
            _position += sz + 2;
        }

        public void WriteArray(float[] value)
        {
            WriteArray(value, 4);
        }

        public void WriteArray(double[] value)
        {
            WriteArray(value, 8);
        }

        public void WriteArray(long[] value)
        {
            WriteArray(value, 8);
        }

        public void WriteArray(ulong[] value)
        {
            WriteArray(value, 8);
        }

        public void WriteArray(int[] value)
        {
            WriteArray(value, 4);
        }

        public void WriteArray(uint[] value)
        {
            WriteArray(value, 4);
        }

        public void WriteArray(ushort[] value)
        {
            WriteArray(value, 2);
        }

        public void WriteArray(short[] value)
        {
            WriteArray(value, 2);
        }

        public void WriteArray(bool[] value)
        {
            WriteArray(value, 1);
        }

        public void WriteArray(string[] value)
        {
            ushort strArrayLength = value == null ? (ushort)0 : (ushort)value.Length;
            Write(strArrayLength);
            for (int i = 0; i < strArrayLength; i++)
                Write(value[i]);
        }

        public void WriteArray(string[] value, int strMaxLength)
        {
            ushort strArrayLength = value == null ? (ushort)0 : (ushort)value.Length;
            Write(strArrayLength);
            for (int i = 0; i < strArrayLength; i++)
                Write(value[i], strMaxLength);
        }

        public void WriteArray<T>(T[] value) where T : INetworkSerializable, new()
        {
            ushort strArrayLength = (ushort)(value?.Length ?? 0);
            Write(strArrayLength);
            for (int i = 0; i < strArrayLength; i++)
                value[i].Serialize(this);
        }

        public void Write(IPEndPoint endPoint)
        {
            Write(endPoint.Address.ToString());
            Write(endPoint.Port);
        }

        public void WriteLargeString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                Write(0);
                return;
            }
            int size = uTF8Encoding.Value.GetByteCount(value);
            if (size == 0)
            {
                Write(0);
                return;
            }
            Write(size);
            if (_autoResize)
                ResizeIfNeed(_position + size);
            uTF8Encoding.Value.GetBytes(value, 0, size, _data, _position);
            _position += size;
        }

        public void Write(string value)
        {
            Write(value, 0);
        }

        /// <summary>
        /// Note that "maxLength" only limits the number of characters in a string, not its size in bytes.
        /// </summary>
        public void Write(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                Write((ushort)0);
                return;
            }

            int length = maxLength > 0 && value.Length > maxLength ? maxLength : value.Length;
            int maxSize = uTF8Encoding.Value.GetMaxByteCount(length);
            if (_autoResize)
                ResizeIfNeed(_position + maxSize + sizeof(ushort));
            int size = uTF8Encoding.Value.GetBytes(value, 0, length, _data, _position + sizeof(ushort));
            if (size == 0)
            {
                Write((ushort)0);
                return;
            }
            Write(checked((ushort)(size + 1)));
            _position += size;
        }

        public void Write<T>(T obj) where T : INetworkSerializable
        {
            obj.Serialize(this);
        }
    }
}
