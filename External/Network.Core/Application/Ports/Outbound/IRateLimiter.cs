using System.Threading.Tasks;

namespace Network.Core.Application.Ports.Outbound;

/// <summary>
/// Port for rate limiting network operations to prevent abuse.
/// Protects against packet spam and DoS attacks.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Checks if a client is allowed to send a packet based on rate limits
    /// </summary>
    /// <param name="clientId">Unique identifier for the client</param>
    /// <param name="packetType">Type of packet being sent</param>
    /// <returns>Rate limit result indicating if allowed and remaining capacity</returns>
    Task<RateLimitResult> CheckRateLimitAsync(string clientId, string packetType);
    
    /// <summary>
    /// Records a packet send for rate limiting tracking
    /// </summary>
    /// <param name="clientId">Unique identifier for the client</param>
    /// <param name="packetType">Type of packet being sent</param>
    Task RecordPacketAsync(string clientId, string packetType);
    
    /// <summary>
    /// Gets current rate limiting statistics for a client
    /// </summary>
    /// <param name="clientId">Unique identifier for the client</param>
    /// <returns>Current rate limiting statistics</returns>
    Task<RateLimitStats> GetStatsAsync(string clientId);
    
    /// <summary>
    /// Resets rate limiting counters for a client (e.g., on disconnect)
    /// </summary>
    /// <param name="clientId">Unique identifier for the client</param>
    Task ResetClientAsync(string clientId);
}

/// <summary>
/// Result of rate limit check
/// </summary>
public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public int RemainingRequests { get; set; }
    public TimeSpan RetryAfter { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Rate limiting statistics for a client
/// </summary>
public class RateLimitStats
{
    public string ClientId { get; set; } = string.Empty;
    public int RequestsInCurrentWindow { get; set; }
    public int MaxRequestsPerWindow { get; set; }
    public DateTime WindowResetTime { get; set; }
    public bool IsBlocked { get; set; }
}