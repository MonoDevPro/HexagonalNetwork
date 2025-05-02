using System.Runtime.CompilerServices;
using LiteNetLib.Utils;
using NetworkHexagonal.Adapters.Outbound.Networking.Util;
using NetworkHexagonal.Core.Application.Ports;

namespace NetworkHexagonal.Adapters.Outbound.Networking.Serializer
{
    public class BufferNetworkReader : INetworkReader
    {
        protected byte[] _data;
        protected int _position;
        protected int _dataSize;
        private int _offset;

        private static ObjectPool<BufferNetworkReader> ReaderPool 
            = new ObjectPool<BufferNetworkReader>(() => new BufferNetworkReader(), reader => reader.Clear(), 0, 10);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static BufferNetworkReader Get()
        {
            return ReaderPool.Get();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static BufferNetworkReader Get(byte[] data)
        {
            var reader = Get();
            reader.SetSource(data);
            return reader;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Return(BufferNetworkReader reader)
        {
            ReaderPool.Return(reader);
        }

        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _data == null;
        }
        public bool AtEndOfStream
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _position == _dataSize;
        }
        public long BytesLeft
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _dataSize - _position;
        }
        public void SetSource(BufferNetworkWriter dataWriter)
        {
            _data = dataWriter.Data;
            _position = 0;
            _offset = 0;
            _dataSize = dataWriter.Length;
        }

        public void SetSource(byte[] source)
        {
            _data = source;
            _position = 0;
            _offset = 0;
            _dataSize = source.Length;
        }

        public void SetSource(byte[] source, int offset, int maxSize)
        {
            _data = source;
            _position = offset;
            _offset = offset;
            _dataSize = maxSize;
        }

        public BufferNetworkReader()
        {

        }

        public BufferNetworkReader(BufferNetworkWriter writer)
        {
            SetSource(writer);
        }

        public BufferNetworkReader(byte[] source)
        {
            SetSource(source);
        }

        public BufferNetworkReader(byte[] source, int offset, int maxSize)
        {
            SetSource(source, offset, maxSize);
        }

        #region GetMethods

        public void Read<T>(out T result) where T : INetworkSerializable, new()
        {
            result = new T();
            result.Deserialize(this);
        }

        public byte ReadByte()
        {
            byte res = _data[_position];
            _position++;
            return res;
        }

        public sbyte ReadSByte()
        {
            return (sbyte)ReadByte();
        }

        public T[] ReadArray<T>(ushort size)
        {
            ushort length = BitConverter.ToUInt16(_data, _position);
            _position += 2;
            T[] result = new T[length];
            length *= size;
            Buffer.BlockCopy(_data, _position, result, 0, length);
            _position += length;
            return result;
        }

        public T[] ReadArray<T>() where T : INetworkSerializable, new()
        {
            ushort length = BitConverter.ToUInt16(_data, _position);
            _position += 2;
            T[] result = new T[length];
            for (int i = 0; i < length; i++)
            {
                var item = new T();
                item.Deserialize(this);
                result[i] = item;
            }
            return result;
        }
        
        public bool[] ReadBoolArray()
        {
            return ReadArray<bool>(1);
        }

        public ushort[] ReadUShortArray()
        {
            return ReadArray<ushort>(2);
        }

        public short[] ReadShortArray()
        {
            return ReadArray<short>(2);
        }

        public int[] ReadIntArray()
        {
            return ReadArray<int>(4);
        }

        public uint[] ReadUIntArray()
        {
            return ReadArray<uint>(4);
        }

        public float[] ReadFloatArray()
        {
            return ReadArray<float>(4);
        }

        public double[] ReadDoubleArray()
        {
            return ReadArray<double>(8);
        }

        public long[] ReadLongArray()
        {
            return ReadArray<long>(8);
        }

        public ulong[] ReadULongArray()
        {
            return ReadArray<ulong>(8);
        }

        public string[] ReadStringArray()
        {
            ushort length = ReadUShort();
            string[] arr = new string[length];
            for (int i = 0; i < length; i++)
            {
                arr[i] = ReadString();
            }
            return arr;
        }

        /// <summary>
        /// Note that "maxStringLength" only limits the number of characters in a string, not its size in bytes.
        /// Strings that exceed this parameter are returned as empty
        /// </summary>
        public string[] ReadStringArray(int maxStringLength)
        {
            ushort length = ReadUShort();
            string[] arr = new string[length];
            for (int i = 0; i < length; i++)
            {
                arr[i] = ReadString(maxStringLength);
            }
            return arr;
        }

        public bool ReadBool()
        {
            return ReadByte() == 1;
        }

        public char ReadChar()
        {
            return (char)ReadUShort();
        }

        public ushort ReadUShort()
        {
            ushort result = BitConverter.ToUInt16(_data, _position);
            _position += 2;
            return result;
        }

        public short ReadShort()
        {
            short result = BitConverter.ToInt16(_data, _position);
            _position += 2;
            return result;
        }

        public long ReadLong()
        {
            long result = BitConverter.ToInt64(_data, _position);
            _position += 8;
            return result;
        }

        public ulong ReadULong()
        {
            ulong result = BitConverter.ToUInt64(_data, _position);
            _position += 8;
            return result;
        }

        public int ReadInt()
        {
            int result = BitConverter.ToInt32(_data, _position);
            _position += 4;
            return result;
        }

        public uint ReadUInt()
        {
            uint result = BitConverter.ToUInt32(_data, _position);
            _position += 4;
            return result;
        }

        public float ReadFloat()
        {
            float result = BitConverter.ToSingle(_data, _position);
            _position += 4;
            return result;
        }

        public double ReadDouble()
        {
            double result = BitConverter.ToDouble(_data, _position);
            _position += 8;
            return result;
        }

        /// <summary>
        /// Note that "maxLength" only limits the number of characters in a string, not its size in bytes.
        /// </summary>
        /// <returns>"string.Empty" if value > "maxLength"</returns>
        public string ReadString(int maxLength)
        {
            ushort size = ReadUShort();
            if (size == 0)
                return string.Empty;
            
            int actualSize = size - 1;
            string result = maxLength > 0 && BufferNetworkWriter.uTF8Encoding.Value.GetCharCount(_data, _position, actualSize) > maxLength ?
                string.Empty :
                BufferNetworkWriter.uTF8Encoding.Value.GetString(_data, _position, actualSize);
            _position += actualSize;
            return result;
        }

        public string ReadString()
        {
            ushort size = ReadUShort();
            if (size == 0)
                return string.Empty;
            
            int actualSize = size - 1;
            string result = BufferNetworkWriter.uTF8Encoding.Value.GetString(_data, _position, actualSize);
            _position += actualSize;
            return result;
        }

        public string ReadLargeString()
        {
            int size = ReadInt();
            if (size <= 0)
                return string.Empty;
            string result = NetDataWriter.uTF8Encoding.Value.GetString(_data, _position, size);
            _position += size;
            return result;
        }

        public ArraySegment<byte> ReadBytesSegment(int count)
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(_data, _position, count);
            _position += count;
            return segment;
        }

        public ArraySegment<byte> ReadRemainingBytesSegment()
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(_data, _position, (int)BytesLeft);
            _position = _data.Length;
            return segment;
        }

        public byte[] ReadRemainingBytes()
        {
            byte[] outgoingData = new byte[BytesLeft];
            Buffer.BlockCopy(_data, _position, outgoingData, 0, (int)BytesLeft);
            _position = _data.Length;
            return outgoingData;
        }

        public void ReadBytes(byte[] destination, int start, int count)
        {
            Buffer.BlockCopy(_data, _position, destination, start, count);
            _position += count;
        }

        public void ReadBytes(byte[] destination, int count)
        {
            Buffer.BlockCopy(_data, _position, destination, 0, count);
            _position += count;
        }

        public sbyte[] ReadSBytesWithLength()
        {
            return ReadArray<sbyte>(1);
        }

        public byte[] ReadBytesWithLength()
        {
            return ReadArray<byte>(1);
        }
        #endregion

        public void Clear()
        {
            _position = 0;
            _dataSize = 0;
            _data = null;
        }

        public byte[] ReadByteArray()
        {
            return ReadArray<byte>(1);
        }
    }
}
