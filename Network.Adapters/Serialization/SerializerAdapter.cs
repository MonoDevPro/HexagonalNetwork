using System.Runtime.Serialization;
using Microsoft.Extensions.Logging;
using Network.Adapters.LiteNet;
using Network.Core.Application.Ports.Outbound;
using Network.Core.Domain.Models;
using NetworkHexagonal.Adapters.Outbound.Network;
using ISerializable = Network.Core.Domain.Models.ISerializable;

namespace NetworkHexagonal.Adapters.Outbound.Serialization
{
    /// <summary>
    /// Adaptador de serialização usando LiteNetLib
    /// </summary>
    public class SerializerAdapter : INetworkSerializer
    {
        private readonly ILogger<SerializerAdapter> _logger;
        private readonly Dictionary<Type, ulong> _packetIdRegistry = new();
        private readonly Dictionary<ulong, Type> _packetTypeRegistry = new();
        
        public SerializerAdapter(ILogger<SerializerAdapter> logger)
        {
            _logger = logger;
        }
        
        public void RegisterPacketType<T>() where T : IPacket, new()
        {
            var type = typeof(T);
            var packetId = GetPacketTypeHash<T>();
            
            if (_packetIdRegistry.ContainsKey(type))
            {
                _logger.LogWarning("Tipo de pacote {PacketType} já registrado", type.Name);
                return;
            }
            
            _packetIdRegistry[type] = packetId;
            _packetTypeRegistry[packetId] = type;
            
            _logger.LogDebug("Registrado tipo de pacote {PacketType} com ID {PacketId}", 
                type.Name, packetId);
        }
        
        public ulong GetPacketId<T>() where T : IPacket
        {
            var type = typeof(T);
            if (_packetIdRegistry.TryGetValue(type, out var packetId))
            {
                return packetId;
            }
            
            packetId = GetPacketTypeHash<T>();
            _packetIdRegistry[type] = packetId;
            _packetTypeRegistry[packetId] = type;
            
            return packetId;
        }
        
        public INetworkWriter Serialize<T>(T packet) where T : IPacket
        {
            try
            {
                var writerAdapter = LiteNetLibWriterAdapter.Pool.Get();
                var packetId = GetPacketId<T>();
                writerAdapter.WriteULong(packetId);
                SerializePacket(packet, writerAdapter); // Usa o adaptador diretamente
                return writerAdapter; // Retorna o NetDataWriter encapsulado
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao serializar pacote do tipo {PacketType}", typeof(T).Name);
                throw new SerializationException($"Falha ao serializar pacote do tipo {typeof(T).Name}", ex);
            }
        }
        
        public T Deserialize<T>(INetworkReader reader) where T : IPacket, new()
        {
            try
            {
                var packet = new T();
                DeserializePacket(packet, reader);
                return packet;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deserializar pacote do tipo {PacketType}", typeof(T).Name);
                throw new SerializationException($"Falha ao deserializar pacote do tipo {typeof(T).Name}", ex);
            }
        }
        
        public IPacket Deserialize(ulong packetId, INetworkReader reader)
        {
            if (!_packetTypeRegistry.TryGetValue(packetId, out var packetType))
            {
                throw new SerializationException($"ID de pacote desconhecido: {packetId}");
            }
            
            try
            {
                // Correção para evitar warning CS8600 (possível valor nulo)
                var packet = Activator.CreateInstance(packetType) as IPacket;
                if (packet == null)
                {
                    throw new SerializationException($"Falha ao criar instância do tipo {packetType.Name}");
                }
                
                DeserializePacket(packet, reader);
                return packet;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deserializar pacote com ID {PacketId}", packetId);
                throw new SerializationException($"Falha ao deserializar pacote com ID {packetId}", ex);
            }
        }
        
        private void SerializePacket<T>(T packet, INetworkWriter writer) where T : IPacket
        {
            if (packet is ISerializable serializablePacket)
            {
                serializablePacket.Serialize(writer);
            }
            else
            {
                throw new SerializationException($"Pacote do tipo {typeof(T).Name} não implementa ISerializable");
            }
        }
        
        private void DeserializePacket(IPacket packet, INetworkReader reader)
        {
            if (packet is ISerializable serializablePacket)
            {
                serializablePacket.Deserialize(reader);
            }
            else
            {
                throw new SerializationException($"Pacote do tipo {packet.GetType().Name} não implementa ISerializable");
            }
        }
        
        private ulong GetPacketTypeHash<T>()
        {
            // Implementação simples - em produção você pode querer um algoritmo de hash mais robusto
            return HashHelper<T>.Id;
        }

        /// <summary>
        /// Classe estática para caching de hash por tipo.
        /// Usa o algoritmo FNV-1 de 64 bits para calcular o hash.
        /// </summary>
        /// <typeparam name="T">Tipo para o qual calcular o hash</typeparam>
        internal static class HashHelper<T>
        {
            public static readonly ulong Id;

            //FNV-1 64 bit hash
            static HashHelper()
            {
                ulong hash = 14695981039346656037UL; //offset
                string typeName = typeof(T).ToString();
                for (var i = 0; i < typeName.Length; i++)
                {
                    hash ^= typeName[i];
                    hash *= 1099511628211UL; //prime
                }
                Id = hash;
            }
        }
    }
}