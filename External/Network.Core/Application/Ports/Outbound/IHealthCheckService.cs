using System.Threading.Tasks;

namespace Network.Core.Application.Ports.Outbound;

/// <summary>
/// Port for health check operations.
/// Provides system health information for monitoring and orchestration.
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Performs a comprehensive health check of the system
    /// </summary>
    /// <returns>Overall health status and detailed component information</returns>
    Task<HealthCheckResult> CheckHealthAsync();
    
    /// <summary>
    /// Performs a health check for a specific component
    /// </summary>
    /// <param name="componentName">Name of the component to check</param>
    /// <returns>Health status for the specific component</returns>
    Task<ComponentHealthResult> CheckComponentHealthAsync(string componentName);
    
    /// <summary>
    /// Registers a health check for a component
    /// </summary>
    /// <param name="componentName">Name of the component</param>
    /// <param name="healthCheck">Health check function</param>
    void RegisterHealthCheck(string componentName, Func<Task<ComponentHealthResult>> healthCheck);
    
    /// <summary>
    /// Gets a quick readiness status (suitable for load balancer checks)
    /// </summary>
    /// <returns>True if the service is ready to accept requests</returns>
    Task<bool> IsReadyAsync();
    
    /// <summary>
    /// Gets a quick liveness status (suitable for restart checks)
    /// </summary>
    /// <returns>True if the service is alive and functioning</returns>
    Task<bool> IsAliveAsync();
}

/// <summary>
/// Overall health check result
/// </summary>
public class HealthCheckResult
{
    public HealthStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public Dictionary<string, ComponentHealthResult> Components { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Health check result for a specific component
/// </summary>
public class ComponentHealthResult
{
    public HealthStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Health status enumeration
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}