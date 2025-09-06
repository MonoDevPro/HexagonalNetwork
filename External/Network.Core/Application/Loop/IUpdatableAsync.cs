using System.Threading;
using System.Threading.Tasks;

namespace Network.Core.Application.Loop;

/// <summary>
/// Interface for components that need to perform asynchronous update operations.
/// This allows non-blocking I/O operations and better scalability.
/// </summary>
public interface IUpdatableAsync
{
    /// <summary>
    /// Asynchronously updates the component.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update in seconds</param>
    /// <param name="cancellationToken">Token to observe for cancellation requests</param>
    /// <returns>A task representing the asynchronous update operation</returns>
    Task UpdateAsync(float deltaTime, CancellationToken cancellationToken = default);
}