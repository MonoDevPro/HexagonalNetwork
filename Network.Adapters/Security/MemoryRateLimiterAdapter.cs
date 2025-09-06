using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Network.Core.Application.Ports.Outbound;

namespace Network.Adapters.Security;

/// <summary>
/// In-memory rate limiter adapter using sliding window algorithm.
/// Prevents packet spam and DoS attacks by limiting requests per time window.
/// </summary>
public class MemoryRateLimiterAdapter : IRateLimiter
{
    private readonly ILogger<MemoryRateLimiterAdapter> _logger;
    private readonly ConcurrentDictionary<string, ClientRateLimit> _clientLimits = new();
    private readonly Timer _cleanupTimer;
    
    // Configuration - in production, these should be configurable
    private readonly int _defaultMaxRequestsPerMinute = 60;
    private readonly int _maxRequestsPerSecond = 10;
    private readonly TimeSpan _windowSize = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

    public MemoryRateLimiterAdapter(ILogger<MemoryRateLimiterAdapter> logger)
    {
        _logger = logger;
        
        // Start cleanup timer to remove expired entries
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, _cleanupInterval, _cleanupInterval);
    }

    public async Task<RateLimitResult> CheckRateLimitAsync(string clientId, string packetType)
    {
        var now = DateTime.UtcNow;
        var clientLimit = _clientLimits.GetOrAdd(clientId, _ => new ClientRateLimit(clientId));
        
        lock (clientLimit)
        {
            // Clean old entries outside the window
            clientLimit.CleanOldEntries(now, _windowSize);
            
            // Get packet-specific limits
            var maxRequests = GetMaxRequestsForPacketType(packetType);
            var currentRequests = clientLimit.GetRequestCount(packetType, now, _windowSize);
            
            if (currentRequests >= maxRequests)
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientId}, packet type {PacketType}. " +
                                 "Current: {Current}, Max: {Max}", 
                                 clientId, packetType, currentRequests, maxRequests);
                
                return new RateLimitResult
                {
                    IsAllowed = false,
                    RemainingRequests = 0,
                    RetryAfter = _windowSize,
                    Reason = $"Rate limit exceeded for packet type {packetType}"
                };
            }
            
            return new RateLimitResult
            {
                IsAllowed = true,
                RemainingRequests = maxRequests - currentRequests,
                RetryAfter = TimeSpan.Zero
            };
        }
    }

    public async Task RecordPacketAsync(string clientId, string packetType)
    {
        var now = DateTime.UtcNow;
        var clientLimit = _clientLimits.GetOrAdd(clientId, _ => new ClientRateLimit(clientId));
        
        lock (clientLimit)
        {
            clientLimit.RecordRequest(packetType, now);
        }
    }

    public async Task<RateLimitStats> GetStatsAsync(string clientId)
    {
        if (!_clientLimits.TryGetValue(clientId, out var clientLimit))
        {
            return new RateLimitStats
            {
                ClientId = clientId,
                RequestsInCurrentWindow = 0,
                MaxRequestsPerWindow = _defaultMaxRequestsPerMinute,
                WindowResetTime = DateTime.UtcNow.Add(_windowSize),
                IsBlocked = false
            };
        }
        
        var now = DateTime.UtcNow;
        lock (clientLimit)
        {
            var totalRequests = clientLimit.GetTotalRequestCount(now, _windowSize);
            
            return new RateLimitStats
            {
                ClientId = clientId,
                RequestsInCurrentWindow = totalRequests,
                MaxRequestsPerWindow = _defaultMaxRequestsPerMinute,
                WindowResetTime = now.Add(_windowSize),
                IsBlocked = totalRequests >= _defaultMaxRequestsPerMinute
            };
        }
    }

    public async Task ResetClientAsync(string clientId)
    {
        if (_clientLimits.TryRemove(clientId, out _))
        {
            _logger.LogDebug("Rate limit data reset for client {ClientId}", clientId);
        }
    }

    private int GetMaxRequestsForPacketType(string packetType)
    {
        // Different packet types can have different limits
        return packetType switch
        {
            "chat" => 20,           // Allow more chat messages
            "movement" => 120,      // High frequency for movement
            "action" => 30,         // Moderate for game actions
            "admin" => 5,           // Very restricted for admin commands
            _ => _defaultMaxRequestsPerMinute
        };
    }

    private void CleanupExpiredEntries(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredClients = new List<string>();
        
        foreach (var kvp in _clientLimits)
        {
            lock (kvp.Value)
            {
                kvp.Value.CleanOldEntries(now, _windowSize);
                
                // Remove clients with no recent activity
                if (kvp.Value.IsEmpty())
                {
                    expiredClients.Add(kvp.Key);
                }
            }
        }
        
        foreach (var clientId in expiredClients)
        {
            _clientLimits.TryRemove(clientId, out _);
        }
        
        if (expiredClients.Count > 0)
        {
            _logger.LogDebug("Cleaned up {Count} expired rate limit entries", expiredClients.Count);
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}

/// <summary>
/// Internal class to track rate limit data for a specific client
/// </summary>
internal class ClientRateLimit
{
    public string ClientId { get; }
    private readonly Dictionary<string, List<DateTime>> _packetRequests = new();
    
    public ClientRateLimit(string clientId)
    {
        ClientId = clientId;
    }
    
    public void RecordRequest(string packetType, DateTime timestamp)
    {
        if (!_packetRequests.TryGetValue(packetType, out var requests))
        {
            requests = new List<DateTime>();
            _packetRequests[packetType] = requests;
        }
        
        requests.Add(timestamp);
    }
    
    public int GetRequestCount(string packetType, DateTime now, TimeSpan window)
    {
        if (!_packetRequests.TryGetValue(packetType, out var requests))
            return 0;
            
        var cutoff = now - window;
        return requests.Count(r => r >= cutoff);
    }
    
    public int GetTotalRequestCount(DateTime now, TimeSpan window)
    {
        var cutoff = now - window;
        return _packetRequests.Values
            .SelectMany(requests => requests)
            .Count(r => r >= cutoff);
    }
    
    public void CleanOldEntries(DateTime now, TimeSpan window)
    {
        var cutoff = now - window;
        var keysToRemove = new List<string>();
        
        foreach (var kvp in _packetRequests)
        {
            kvp.Value.RemoveAll(r => r < cutoff);
            
            if (kvp.Value.Count == 0)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            _packetRequests.Remove(key);
        }
    }
    
    public bool IsEmpty()
    {
        return _packetRequests.Count == 0;
    }
}