namespace Network.Core.Application.Loop;

public interface IInitializable : IAsyncDisposable
{
    Task InitializeAsync(CancellationToken ct = default);
    
    Task StopAsync(CancellationToken ct = default);
}