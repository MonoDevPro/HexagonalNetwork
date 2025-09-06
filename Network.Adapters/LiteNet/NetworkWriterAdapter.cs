using System.Numerics;
using LiteNetLib.Utils;
using Network.Core.Application.Ports.Outbound;
using Network.Core.Domain.Models;

namespace Network.Adapters.LiteNet;

/// <summary>
/// Adaptador que encapsula o NetDataWriter do LiteNetLib
/// </summary>
public class LiteNetLibWriterAdapter : INetworkWriter
{
    private readonly NetDataWriter _writer;

    public static ObjectPool<LiteNetLibWriterAdapter> Pool { get; } 
        = new ObjectPool<LiteNetLibWriterAdapter>(() => new LiteNetLibWriterAdapter(), 
        adapter => adapter.Reset(), 5);
    
    public LiteNetLibWriterAdapter()
    {
        _writer = new NetDataWriter();
    }

    public LiteNetLibWriterAdapter(NetDataWriter writer)
    {
        _writer = writer;
    }

    public byte[] Data => _writer.Data;
    
    public void WriteByte(byte value) => _writer.Put(value);
    public void WriteSByte(sbyte value) => _writer.Put(value);
    public void WriteBool(bool value) => _writer.Put(value);
    public void WriteShort(short value) => _writer.Put(value);
    public void WriteUShort(ushort value) => _writer.Put(value);
    public void WriteInt(int value) => _writer.Put(value);
    public void WriteUInt(uint value) => _writer.Put(value);
    public void WriteLong(long value) => _writer.Put(value);
    public void WriteULong(ulong value) => _writer.Put(value);
    public void WriteFloat(float value) => _writer.Put(value);
    public void WriteDouble(double value) => _writer.Put(value);
    public void WriteString(string value) => _writer.Put(value);
    public void WriteBytes(byte[] value) => _writer.PutBytesWithLength(value);
    
    public void WriteVector2(Vector2 value)
    {
        _writer.Put(value.X);
        _writer.Put(value.Y);
    }
    
    public void WriteVector3(Vector3 value)
    {
        _writer.Put(value.X);
        _writer.Put(value.Y);
        _writer.Put(value.Z);
    }

    public void WriteSerializable<T>(T value) where T : ISerializable
    {
        value.Serialize(this);
    }
    
    public void Reset() => _writer.Reset();

    public void Recycle()
    {
        Pool.Return(this);
    }

    public int Length => _writer.Length;
}