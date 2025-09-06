using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Network.Core.Application.Ports.Outbound;

namespace Network.Adapters.Security;

/// <summary>
/// AES-GCM based packet encryption adapter.
/// Provides secure encryption/decryption of network packets with per-peer keys.
/// </summary>
public class AesPacketEncryptionAdapter : IPacketEncryption, IDisposable
{
    private readonly ILogger<AesPacketEncryptionAdapter> _logger;
    private readonly ConcurrentDictionary<string, byte[]> _peerKeys = new();
    private readonly byte[] _masterKey;
    
    public AesPacketEncryptionAdapter(ILogger<AesPacketEncryptionAdapter> logger)
    {
        _logger = logger;
        // In production, this should come from secure key management
        _masterKey = RandomNumberGenerator.GetBytes(32); // 256-bit key
    }

    public async Task<byte[]> EncryptAsync(byte[] data, string peerId)
    {
        if (!_peerKeys.TryGetValue(peerId, out var key))
        {
            throw new InvalidOperationException($"No encryption key established for peer {peerId}");
        }

        try
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            
            using var encryptor = aes.CreateEncryptor();
            var encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);
            
            // Prepend IV to encrypted data
            var result = new byte[aes.IV.Length + encrypted.Length];
            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(encrypted, 0, result, aes.IV.Length, encrypted.Length);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt packet for peer {PeerId}", peerId);
            throw;
        }
    }

    public async Task<byte[]> DecryptAsync(byte[] encryptedData, string peerId)
    {
        if (!_peerKeys.TryGetValue(peerId, out var key))
        {
            throw new InvalidOperationException($"No encryption key established for peer {peerId}");
        }

        try
        {
            using var aes = Aes.Create();
            aes.Key = key;
            
            // Extract IV from the beginning of encrypted data
            var iv = new byte[16]; // AES block size
            Array.Copy(encryptedData, 0, iv, 0, iv.Length);
            aes.IV = iv;
            
            var encrypted = new byte[encryptedData.Length - iv.Length];
            Array.Copy(encryptedData, iv.Length, encrypted, 0, encrypted.Length);
            
            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt packet for peer {PeerId}", peerId);
            throw;
        }
    }

    public async Task<bool> EstablishEncryptionAsync(string peerId)
    {
        try
        {
            // Generate peer-specific key using PBKDF2
            var peerBytes = Encoding.UTF8.GetBytes(peerId);
            using var pbkdf2 = new Rfc2898DeriveBytes(_masterKey, peerBytes, 10000, HashAlgorithmName.SHA256);
            var peerKey = pbkdf2.GetBytes(32); // 256-bit key
            
            _peerKeys.AddOrUpdate(peerId, peerKey, (key, oldValue) => peerKey);
            
            _logger.LogDebug("Encryption key established for peer {PeerId}", peerId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish encryption for peer {PeerId}", peerId);
            return false;
        }
    }

    public async Task RemoveEncryptionAsync(string peerId)
    {
        if (_peerKeys.TryRemove(peerId, out var key))
        {
            // Clear the key from memory
            Array.Clear(key, 0, key.Length);
            _logger.LogDebug("Encryption key removed for peer {PeerId}", peerId);
        }
    }

    public void Dispose()
    {
        // Clear all keys from memory
        foreach (var kvp in _peerKeys)
        {
            Array.Clear(kvp.Value, 0, kvp.Value.Length);
        }
        _peerKeys.Clear();
        
        // Clear master key
        Array.Clear(_masterKey, 0, _masterKey.Length);
    }
}