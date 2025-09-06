using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Network.Core.Application.Ports.Outbound;

namespace Network.Adapters.Resilience;

/// <summary>
/// Circuit breaker adapter implementing the circuit breaker pattern.
/// Prevents cascading failures by temporarily blocking operations to failed services.
/// </summary>
public class CircuitBreakerAdapter : ICircuitBreaker
{
    private readonly ILogger<CircuitBreakerAdapter> _logger;
    private readonly ConcurrentDictionary<string, CircuitBreakerState> _states = new();
    private readonly ConcurrentDictionary<string, CircuitBreakerMetrics> _metrics = new();
    
    // Configuration - in production, these should be configurable per operation
    private readonly int _failureThreshold = 5;
    private readonly TimeSpan _timeout = TimeSpan.FromMinutes(1);
    private readonly int _successThreshold = 3;

    public CircuitBreakerAdapter(ILogger<CircuitBreakerAdapter> logger)
    {
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationKey)
    {
        var state = GetState(operationKey);
        var metrics = _metrics.GetOrAdd(operationKey, _ => new CircuitBreakerMetrics());
        
        switch (state)
        {
            case CircuitBreakerState.Open:
                if (DateTime.UtcNow - metrics.LastFailureTime < _timeout)
                {
                    throw new CircuitBreakerOpenException(operationKey, _timeout);
                }
                
                // Transition to Half-Open
                _states.TryUpdate(operationKey, CircuitBreakerState.HalfOpen, CircuitBreakerState.Open);
                _logger.LogInformation("Circuit breaker for {OperationKey} transitioned to Half-Open", operationKey);
                break;
                
            case CircuitBreakerState.HalfOpen:
                // Allow limited requests to test if service is back
                break;
                
            case CircuitBreakerState.Closed:
                // Normal operation
                break;
        }

        try
        {
            var result = await operation();
            
            // Operation succeeded
            RecordSuccess(operationKey, metrics);
            
            return result;
        }
        catch (Exception ex)
        {
            // Operation failed
            RecordFailure(operationKey, metrics, ex);
            throw;
        }
    }

    public async Task ExecuteAsync(Func<Task> operation, string operationKey)
    {
        await ExecuteAsync(async () =>
        {
            await operation();
            return true; // Dummy return value
        }, operationKey);
    }

    public CircuitBreakerState GetState(string operationKey)
    {
        return _states.GetOrAdd(operationKey, _ => CircuitBreakerState.Closed);
    }

    public void Open(string operationKey)
    {
        _states.AddOrUpdate(operationKey, CircuitBreakerState.Open, (key, oldValue) => CircuitBreakerState.Open);
        var metrics = _metrics.GetOrAdd(operationKey, _ => new CircuitBreakerMetrics());
        metrics.LastFailureTime = DateTime.UtcNow;
        
        _logger.LogWarning("Circuit breaker for {OperationKey} manually opened", operationKey);
    }

    public void Close(string operationKey)
    {
        _states.AddOrUpdate(operationKey, CircuitBreakerState.Closed, (key, oldValue) => CircuitBreakerState.Closed);
        var metrics = _metrics.GetOrAdd(operationKey, _ => new CircuitBreakerMetrics());
        metrics.Reset();
        
        _logger.LogInformation("Circuit breaker for {OperationKey} manually closed", operationKey);
    }

    private void RecordSuccess(string operationKey, CircuitBreakerMetrics metrics)
    {
        var currentState = GetState(operationKey);
        
        metrics.ConsecutiveFailures = 0;
        metrics.ConsecutiveSuccesses++;
        metrics.LastSuccessTime = DateTime.UtcNow;
        
        if (currentState == CircuitBreakerState.HalfOpen && metrics.ConsecutiveSuccesses >= _successThreshold)
        {
            // Transition back to Closed
            _states.TryUpdate(operationKey, CircuitBreakerState.Closed, CircuitBreakerState.HalfOpen);
            metrics.Reset();
            _logger.LogInformation("Circuit breaker for {OperationKey} transitioned to Closed after {SuccessCount} successes", 
                operationKey, metrics.ConsecutiveSuccesses);
        }
    }

    private void RecordFailure(string operationKey, CircuitBreakerMetrics metrics, Exception exception)
    {
        var currentState = GetState(operationKey);
        
        metrics.ConsecutiveSuccesses = 0;
        metrics.ConsecutiveFailures++;
        metrics.LastFailureTime = DateTime.UtcNow;
        metrics.LastException = exception;
        
        _logger.LogWarning(exception, "Operation {OperationKey} failed. Consecutive failures: {FailureCount}", 
            operationKey, metrics.ConsecutiveFailures);
        
        if (currentState == CircuitBreakerState.Closed && metrics.ConsecutiveFailures >= _failureThreshold)
        {
            // Transition to Open
            _states.TryUpdate(operationKey, CircuitBreakerState.Open, CircuitBreakerState.Closed);
            _logger.LogError("Circuit breaker for {OperationKey} opened after {FailureCount} consecutive failures", 
                operationKey, metrics.ConsecutiveFailures);
        }
        else if (currentState == CircuitBreakerState.HalfOpen)
        {
            // Transition back to Open
            _states.TryUpdate(operationKey, CircuitBreakerState.Open, CircuitBreakerState.HalfOpen);
            _logger.LogWarning("Circuit breaker for {OperationKey} reopened due to failure in Half-Open state", operationKey);
        }
    }
}

/// <summary>
/// Internal class to track circuit breaker metrics
/// </summary>
internal class CircuitBreakerMetrics
{
    public int ConsecutiveFailures { get; set; }
    public int ConsecutiveSuccesses { get; set; }
    public DateTime LastFailureTime { get; set; }
    public DateTime LastSuccessTime { get; set; }
    public Exception? LastException { get; set; }
    
    public void Reset()
    {
        ConsecutiveFailures = 0;
        ConsecutiveSuccesses = 0;
        LastException = null;
        // Don't reset timestamps to maintain history
    }
}