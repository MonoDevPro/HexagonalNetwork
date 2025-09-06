using System.Threading.Tasks;

namespace Network.Core.Application.Ports.Outbound;

/// <summary>
/// Port for packet encryption and decryption operations.
/// Provides abstraction for different encryption methods (DTLS, AES, etc.)
/// </summary>
public interface IPacketEncryption
{
    /// <summary>
    /// Encrypts packet data before transmission
    /// </summary>
    /// <param name="data">Raw packet data to encrypt</param>
    /// <param name="peerId">ID of the peer for per-connection encryption keys</param>
    /// <returns>Encrypted data</returns>
    Task<byte[]> EncryptAsync(byte[] data, string peerId);
    
    /// <summary>
    /// Decrypts received packet data
    /// </summary>
    /// <param name="encryptedData">Encrypted packet data</param>
    /// <param name="peerId">ID of the peer for per-connection encryption keys</param>
    /// <returns>Decrypted data</returns>
    Task<byte[]> DecryptAsync(byte[] encryptedData, string peerId);
    
    /// <summary>
    /// Generates or exchanges encryption keys for a peer
    /// </summary>
    /// <param name="peerId">ID of the peer</param>
    /// <returns>True if key exchange was successful</returns>
    Task<bool> EstablishEncryptionAsync(string peerId);
    
    /// <summary>
    /// Removes encryption keys for a disconnected peer
    /// </summary>
    /// <param name="peerId">ID of the peer</param>
    Task RemoveEncryptionAsync(string peerId);
}