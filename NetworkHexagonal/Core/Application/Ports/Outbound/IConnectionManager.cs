
namespace NetworkHexagonal.Core.Application.Ports.Outbound;

/// <summary>
/// Interface para gerenciamento de conexões
/// </summary>
public interface IConnectionManager
{
    int GetConnectedPeerCount();
}