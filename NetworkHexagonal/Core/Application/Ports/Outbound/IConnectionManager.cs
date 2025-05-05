
namespace NetworkHexagonal.Core.Application.Ports.Outbound;

/// <summary>
/// Interface para gerenciamento de conexões
/// </summary>
public interface IConnectionManager
{
    /// <summary>
    /// Obtém o número de conexões ativas
    /// </summary>
    /// <returns>O número de conexões ativas</returns>
    int GetConnectedPeerCount();

    /// <summary>
    /// Verificar se uma conexão está ativa
    /// </summary>
    /// <param name="peerId">O ID do peer</param>
    /// <returns>True se a conexão estiver ativa, caso contrário, false</returns>
    bool IsPeerConnected(int peerId);
}