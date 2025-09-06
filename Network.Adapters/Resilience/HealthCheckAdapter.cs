using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Network.Core.Application.Ports.Outbound;

namespace Network.Adapters.Resilience;

/// <summary>
/// Health check service adapter for monitoring system health and readiness.
/// Provides health endpoints for Kubernetes and other orchestration systems.
/// </summary>
public class HealthCheckAdapter : IHealthCheckService
{
    private readonly ILogger<HealthCheckAdapter> _logger;
    private readonly ConcurrentDictionary<string, Func<Task<ComponentHealthResult>>> _healthChecks = new();
    private DateTime _startTime;

    public HealthCheckAdapter(ILogger<HealthCheckAdapter> logger)
    {
        _logger = logger;
        _startTime = DateTime.UtcNow;
        
        // Register default health checks
        RegisterDefaultHealthChecks();
    }

    public async Task<HealthCheckResult> CheckHealthAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new HealthCheckResult();
        
        try
        {
            var healthCheckTasks = _healthChecks.Select(async kvp =>
            {
                try
                {
                    var componentResult = await kvp.Value();
                    return new KeyValuePair<string, ComponentHealthResult>(kvp.Key, componentResult);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Health check failed for component {ComponentName}", kvp.Key);
                    return new KeyValuePair<string, ComponentHealthResult>(kvp.Key, new ComponentHealthResult
                    {
                        Status = HealthStatus.Unhealthy,
                        Description = $"Health check failed: {ex.Message}",
                        Duration = stopwatch.Elapsed
                    });
                }
            });

            var componentResults = await Task.WhenAll(healthCheckTasks);
            
            foreach (var componentResult in componentResults)
            {
                result.Components[componentResult.Key] = componentResult.Value;
            }

            // Determine overall status
            result.Status = DetermineOverallStatus(result.Components.Values);
            result.Duration = stopwatch.Elapsed;
            result.Description = $"Health check completed. {result.Components.Count} components checked.";
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check execution failed");
            result.Status = HealthStatus.Unhealthy;
            result.Description = $"Health check execution failed: {ex.Message}";
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    public async Task<ComponentHealthResult> CheckComponentHealthAsync(string componentName)
    {
        if (!_healthChecks.TryGetValue(componentName, out var healthCheck))
        {
            return new ComponentHealthResult
            {
                Status = HealthStatus.Unhealthy,
                Description = $"Component '{componentName}' not found",
                Duration = TimeSpan.Zero
            };
        }

        try
        {
            return await healthCheck();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for component {ComponentName}", componentName);
            return new ComponentHealthResult
            {
                Status = HealthStatus.Unhealthy,
                Description = $"Health check failed: {ex.Message}",
                Duration = TimeSpan.Zero
            };
        }
    }

    public void RegisterHealthCheck(string componentName, Func<Task<ComponentHealthResult>> healthCheck)
    {
        _healthChecks.AddOrUpdate(componentName, healthCheck, (key, oldValue) => healthCheck);
        _logger.LogDebug("Health check registered for component {ComponentName}", componentName);
    }

    public async Task<bool> IsReadyAsync()
    {
        try
        {
            var healthResult = await CheckHealthAsync();
            return healthResult.Status != HealthStatus.Unhealthy;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsAliveAsync()
    {
        try
        {
            // Basic liveness check - service is running and responsive
            var uptime = DateTime.UtcNow - _startTime;
            return uptime > TimeSpan.FromSeconds(5); // Require at least 5 seconds uptime
        }
        catch
        {
            return false;
        }
    }

    private void RegisterDefaultHealthChecks()
    {
        // Memory health check
        RegisterHealthCheck("memory", async () =>
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                GC.Collect();
                var memoryBefore = GC.GetTotalMemory(false);
                var memoryAfter = GC.GetTotalMemory(true);
                var memoryMB = memoryAfter / (1024 * 1024);
                
                var status = memoryMB switch
                {
                    < 100 => HealthStatus.Healthy,
                    < 500 => HealthStatus.Degraded,
                    _ => HealthStatus.Unhealthy
                };

                return new ComponentHealthResult
                {
                    Status = status,
                    Description = $"Memory usage: {memoryMB:F1} MB",
                    Duration = stopwatch.Elapsed,
                    Data = new Dictionary<string, object>
                    {
                        ["memory_mb"] = memoryMB,
                        ["memory_before_gc"] = memoryBefore,
                        ["memory_after_gc"] = memoryAfter
                    }
                };
            }
            catch (Exception ex)
            {
                return new ComponentHealthResult
                {
                    Status = HealthStatus.Unhealthy,
                    Description = $"Memory check failed: {ex.Message}",
                    Duration = stopwatch.Elapsed
                };
            }
        });

        // Uptime health check
        RegisterHealthCheck("uptime", async () =>
        {
            var stopwatch = Stopwatch.StartNew();
            var uptime = DateTime.UtcNow - _startTime;
            
            return new ComponentHealthResult
            {
                Status = HealthStatus.Healthy,
                Description = $"Service uptime: {uptime:dd\\.hh\\:mm\\:ss}",
                Duration = stopwatch.Elapsed,
                Data = new Dictionary<string, object>
                {
                    ["uptime_seconds"] = uptime.TotalSeconds,
                    ["start_time"] = _startTime
                }
            };
        });

        // Thread pool health check
        RegisterHealthCheck("threadpool", async () =>
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                ThreadPool.GetAvailableThreads(out var workerThreads, out var completionPortThreads);
                ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
                
                var workerUtilization = (double)(maxWorkerThreads - workerThreads) / maxWorkerThreads;
                var completionUtilization = (double)(maxCompletionPortThreads - completionPortThreads) / maxCompletionPortThreads;
                
                var status = Math.Max(workerUtilization, completionUtilization) switch
                {
                    < 0.7 => HealthStatus.Healthy,
                    < 0.9 => HealthStatus.Degraded,
                    _ => HealthStatus.Unhealthy
                };

                return new ComponentHealthResult
                {
                    Status = status,
                    Description = $"ThreadPool utilization: {workerUtilization:P1} worker, {completionUtilization:P1} completion",
                    Duration = stopwatch.Elapsed,
                    Data = new Dictionary<string, object>
                    {
                        ["worker_threads_available"] = workerThreads,
                        ["completion_port_threads_available"] = completionPortThreads,
                        ["worker_threads_max"] = maxWorkerThreads,
                        ["completion_port_threads_max"] = maxCompletionPortThreads,
                        ["worker_utilization"] = workerUtilization,
                        ["completion_utilization"] = completionUtilization
                    }
                };
            }
            catch (Exception ex)
            {
                return new ComponentHealthResult
                {
                    Status = HealthStatus.Unhealthy,
                    Description = $"ThreadPool check failed: {ex.Message}",
                    Duration = stopwatch.Elapsed
                };
            }
        });
    }

    private static HealthStatus DetermineOverallStatus(IEnumerable<ComponentHealthResult> componentResults)
    {
        if (componentResults.Any(r => r.Status == HealthStatus.Unhealthy))
            return HealthStatus.Unhealthy;
            
        if (componentResults.Any(r => r.Status == HealthStatus.Degraded))
            return HealthStatus.Degraded;
            
        return HealthStatus.Healthy;
    }
}