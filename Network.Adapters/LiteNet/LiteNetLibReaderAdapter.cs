using System.Numerics;
using LiteNetLib.Utils;
using Network.Core.Application.Ports.Outbound;
using Network.Core.Domain.Models;

namespace Network.Adapters.LiteNet;

/// <summary>
/// Adaptador que encapsula o NetDataReader do LiteNetLib
/// </summary>
public class LiteNetLibReaderAdapter : INetworkReader
{
    private readonly NetDataReader _reader;

    public static ObjectPool<LiteNetLibReaderAdapter> Pool { get; } = new ObjectPool<LiteNetLibReaderAdapter>(() => new LiteNetLibReaderAdapter(), 
        adapter => adapter.ResetPosition(), 5);
    
    public LiteNetLibReaderAdapter()
    {
        _reader = new NetDataReader();
    }
    
    public LiteNetLibReaderAdapter(NetDataReader reader)
    {
        _reader = reader;
    }
    
    public LiteNetLibReaderAdapter(byte[] data)
    {
        _reader = new NetDataReader(data);
    }
    
    public NetDataReader Data => _reader;
    
    public byte ReadByte() => _reader.GetByte();
    public sbyte ReadSByte() => _reader.GetSByte();
    public bool ReadBool() => _reader.GetBool();
    public short ReadShort() => _reader.GetShort();
    public ushort ReadUShort() => _reader.GetUShort();
    public int ReadInt() => _reader.GetInt();
    public uint ReadUInt() => _reader.GetUInt();
    public long ReadLong() => _reader.GetLong();
    public ulong ReadULong() => _reader.GetULong();
    public float ReadFloat() => _reader.GetFloat();
    public double ReadDouble() => _reader.GetDouble();
    public string ReadString() => _reader.GetString();
    public byte[] ReadBytes(int count) => _reader.GetBytesWithLength();
    
    public Vector2 ReadVector2() => new Vector2(_reader.GetFloat(), _reader.GetFloat());
    public Vector3 ReadVector3() => new Vector3(_reader.GetFloat(), _reader.GetFloat(), _reader.GetFloat());
    
    public T ReadSerializable<T>() where T : ISerializable, new()
    {
        var instance = new T();
        instance.Deserialize(this);
        return instance;
    }

    public void ResetPosition(int position = 0)
    {
        _reader.SetPosition(position);
    }

    public void SetSource(byte[] data)
    {
        _reader.SetSource(data);
    }
    public void Recycle()
    {
        Pool.Return(this);
    }
    
    public int Position => _reader.Position;
    public int Available => _reader.AvailableBytes;
}