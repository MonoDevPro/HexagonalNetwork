using System.Numerics;
using System.Runtime.CompilerServices;
using LiteNetLib.Utils;
using Network.Adapters.Pool;
using Network.Core.Application.Ports.Outbound;
using Network.Core.Domain.Models;

namespace Network.Adapters.LiteNet;

/// <summary>
/// Adaptador que encapsula o NetDataWriter do LiteNetLib
/// </summary>
public class LiteNetLibWriterAdapter(NetDataWriter writer) : INetworkWriter
{
    public static ObjectPool<LiteNetLibWriterAdapter> Pool { get; } 
        = new ObjectPool<LiteNetLibWriterAdapter>(() => new LiteNetLibWriterAdapter(), 
        adapter => adapter.Reset(), 5);
    
    public LiteNetLibWriterAdapter() : this(new NetDataWriter())
    {
    }

    public byte[] Data => writer.Data;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value) => writer.Put(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSByte(sbyte value) => writer.Put(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBool(bool value) => writer.Put(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteShort(short value) => writer.Put(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUShort(ushort value) => writer.Put(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt(int value) => writer.Put(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt(uint value) => writer.Put(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLong(long value) => writer.Put(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteULong(ulong value) => writer.Put(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat(float value) => writer.Put(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteDouble(double value) => writer.Put(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(string value) => writer.Put(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBytes(byte[] value) => writer.PutBytesWithLength(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVector2(Vector2 value) { writer.Put(value.X); writer.Put(value.Y); }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVector3(Vector3 value) { writer.Put(value.X); writer.Put(value.Y); writer.Put(value.Z); }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSerializable<T>(T value) where T : ISerializable => value.Serialize(this);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset() => writer.Reset();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Recycle() => Pool.Return(this);

    public int Length => writer.Length;
}