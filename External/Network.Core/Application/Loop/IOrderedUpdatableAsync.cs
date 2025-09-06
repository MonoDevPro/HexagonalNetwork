using System.Threading;
using System.Threading.Tasks;

namespace Network.Core.Application.Loop;

/// <summary>
/// Interface for ordered services that need to perform asynchronous update operations.
/// Provides ordering capability for initialization and update sequence.
/// </summary>
public interface IOrderedUpdatableAsync : IUpdatableAsync, IOrderedService
{
    // Inherits UpdateAsync from IUpdatableAsync and Order from IOrderedService
}