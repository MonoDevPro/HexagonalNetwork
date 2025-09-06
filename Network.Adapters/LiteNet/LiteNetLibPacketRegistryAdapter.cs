using Microsoft.Extensions.Logging;
using Network.Core.Application.Ports.Outbound;
using Network.Core.Domain.Exceptions;
using Network.Core.Domain.Models;

namespace Network.Adapters.LiteNet;

/// <summary>
/// Adaptador para registro de manipuladores de pacotes
/// </summary>
public class LiteNetLibPacketRegistryAdapter : IPacketRegistry
{
    private readonly Dictionary<ulong, PacketHandlerDelegate> _packetHandlers = new();
    private readonly ILogger<LiteNetLibPacketRegistryAdapter> _logger;
    private readonly INetworkSerializer _serializer;
        
    public LiteNetLibPacketRegistryAdapter(
        INetworkSerializer serializer,
        ILogger<LiteNetLibPacketRegistryAdapter> logger)
    {
        _serializer = serializer;
        _logger = logger;
    }
        
    /// <summary>
    /// Registra um manipulador para um tipo específico de pacote
    /// </summary>
    public void RegisterHandler<T>(Action<T, PacketContext> handler) where T : IPacket, new()
    {
        var packetId = _serializer.GetPacketId<T>();
            
        if (_packetHandlers.ContainsKey(packetId))
        {
            _logger.LogWarning("Manipulador para o tipo {PacketType} já registrado", typeof(T).Name);
            return;
        }
            
        _packetHandlers[packetId] = (reader, context) => {
            try
            {
                var deserializedPacket = _serializer.Deserialize<T>(reader);
                handler(deserializedPacket, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar pacote do tipo {PacketType}", typeof(T).Name);
                throw new PacketHandlingException($"Falha ao processar pacote do tipo {typeof(T).Name}", ex);
            }
        };
            
        _logger.LogDebug("Registrado manipulador para o tipo de pacote {PacketType} com ID {PacketId}", 
            typeof(T).Name, packetId);
    }
        
    /// <summary>
    /// Verifica se existe um manipulador registrado para um ID de pacote
    /// </summary>
    public bool HasHandler(ulong packetId)
    {
        return _packetHandlers.ContainsKey(packetId);
    }
        
    /// <summary>
    /// Processa um pacote com base no seu ID
    /// </summary>
    public void HandlePacket(ulong packetId, INetworkReader reader, PacketContext context)
    {
        if (_packetHandlers.TryGetValue(packetId, out var handler))
        {
            handler(reader, context);
        }
        else
        {
            _logger.LogWarning("Nenhum manipulador registrado para pacote com ID {PacketId}", packetId);
            throw new PacketHandlingException($"Nenhum manipulador registrado para pacote com ID {packetId}");
        }
    }
}