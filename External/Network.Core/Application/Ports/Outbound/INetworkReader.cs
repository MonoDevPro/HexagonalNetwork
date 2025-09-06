using System.Numerics;
using Network.Core.Domain.Models;

namespace Network.Core.Application.Ports.Outbound
{
    /// <summary>
    /// Interface para leitura de dados da rede, abstraindo a implementação específica do LiteNetLib
    /// </summary>
    public interface INetworkReader
    {
        byte ReadByte();
        sbyte ReadSByte();
        bool ReadBool();
        short ReadShort();
        ushort ReadUShort();
        int ReadInt();
        uint ReadUInt();
        long ReadLong();
        ulong ReadULong();
        float ReadFloat();
        double ReadDouble();
        string ReadString();
        byte[] ReadBytes(int count);
        Vector2 ReadVector2();
        Vector3 ReadVector3();
        T ReadSerializable<T>() where T : ISerializable, new();
        
        void ResetPosition(int position = 0);
        int Position { get; }
        int Available { get; }
    }
}