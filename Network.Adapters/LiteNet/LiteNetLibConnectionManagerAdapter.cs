using LiteNetLib;
using Microsoft.Extensions.Logging;
using Network.Core.Application.Ports.Outbound;
using Network.Core.Domain.Events;

namespace Network.Adapters.LiteNet;

/// <summary>
/// Adaptador para gerenciamento de conexões do LiteNetLib
/// </summary>
public class LiteNetLibConnectionManagerAdapter(ILogger<LiteNetLibConnectionManagerAdapter> logger) : IConnectionManager
{
    private readonly Dictionary<int, NetPeer> _peers = new();

    public event Action<ConnectionLatencyEvent>? ConnectionLatencyEvent;
    public void OnConnectionLatencyEvent(NetPeer peer, int latency)
    {
        ConnectionLatencyEvent?.Invoke(new ConnectionLatencyEvent(peer.Id, latency));
    }

    /// <summary>
    /// Registra um novo peer
    /// </summary>
    /// <param name="peerId">ID do peer</param>
    /// <param name="peer">Instância do NetPeer</param>
    public void RegisterPeer(int peerId, NetPeer peer)
    {
        _peers[peerId] = peer;
        logger.LogDebug("Peer registrado: {PeerId}", peerId);
    }
        
    /// <summary>
    /// Remove um peer do registro
    /// </summary>
    /// <param name="peerId">ID do peer</param>
    public void UnregisterPeer(int peerId)
    {
        if (_peers.Remove(peerId))
        {
            logger.LogDebug("Peer removido: {PeerId}", peerId);
        }
    }
        
    /// <summary>
    /// Obtém um peer pelo ID
    /// </summary>
    /// <param name="peerId">ID do peer</param>
    /// <returns>A instância do NetPeer ou null se não encontrado</returns>
    public NetPeer? GetPeer(int peerId)  // Adicionado o operador ? para indicar que pode retornar null
    {
        if (_peers.TryGetValue(peerId, out var peer))
        {
            return peer;
        }
            
        logger.LogWarning("Tentativa de obter peer inexistente: {PeerId}", peerId);
        return null;
    }
        
    /// <summary>
    /// Obtém todos os peers conectados
    /// </summary>
    /// <returns>Lista de peers</returns>
    public IReadOnlyCollection<NetPeer> GetAllPeers()
    {
        return _peers.Values.ToList().AsReadOnly();
    }
        
    /// <summary>
    /// Obtém o número de peers conectados
    /// </summary>
    public int GetConnectedPeerCount()
    {
        return _peers.Count;
    }

    public bool IsPeerConnected(int peerId)
    {
        if (_peers.TryGetValue(peerId, out var peer))
        {
            return peer.ConnectionState == ConnectionState.Connected;
        }
        else
        {
            logger.LogWarning("Tentativa de verificar conexão de peer inexistente: {PeerId}", peerId);
            return false;
        }
    }
}