using System.Threading.Tasks;

namespace Network.Core.Application.Ports.Outbound;

/// <summary>
/// Port for circuit breaker pattern implementation.
/// Prevents cascading failures by temporarily blocking operations to failed services.
/// </summary>
public interface ICircuitBreaker
{
    /// <summary>
    /// Executes an operation through the circuit breaker
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operation">Operation to execute</param>
    /// <param name="operationKey">Unique key identifying the operation type</param>
    /// <returns>Result of the operation</returns>
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationKey);
    
    /// <summary>
    /// Executes an operation through the circuit breaker (void return)
    /// </summary>
    /// <param name="operation">Operation to execute</param>
    /// <param name="operationKey">Unique key identifying the operation type</param>
    Task ExecuteAsync(Func<Task> operation, string operationKey);
    
    /// <summary>
    /// Gets the current state of a circuit breaker
    /// </summary>
    /// <param name="operationKey">Unique key identifying the operation type</param>
    /// <returns>Current circuit breaker state</returns>
    CircuitBreakerState GetState(string operationKey);
    
    /// <summary>
    /// Manually opens a circuit breaker
    /// </summary>
    /// <param name="operationKey">Unique key identifying the operation type</param>
    void Open(string operationKey);
    
    /// <summary>
    /// Manually closes a circuit breaker
    /// </summary>
    /// <param name="operationKey">Unique key identifying the operation type</param>
    void Close(string operationKey);
}

/// <summary>
/// Circuit breaker states
/// </summary>
public enum CircuitBreakerState
{
    Closed,    // Normal operation
    Open,      // Blocking all requests
    HalfOpen   // Testing if service is back online
}

/// <summary>
/// Exception thrown when circuit breaker is open
/// </summary>
public class CircuitBreakerOpenException : Exception
{
    public string OperationKey { get; }
    public TimeSpan RetryAfter { get; }
    
    public CircuitBreakerOpenException(string operationKey, TimeSpan retryAfter) 
        : base($"Circuit breaker is open for operation '{operationKey}'. Retry after {retryAfter}")
    {
        OperationKey = operationKey;
        RetryAfter = retryAfter;
    }
}