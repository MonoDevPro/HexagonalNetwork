using Network.Core.Application.Loop;
using Network.Core.Application.Ports.Inbound;

namespace Network.Adapters.Loop;

public class NetworkLoopAdapter(IClientNetworkApp? clientNetworkApp, IServerNetworkApp? serverNetworkApp) : IOrderedInitializable, IOrderedUpdatable
{
    public int Order { get; } = 0;
    
    public ValueTask DisposeAsync()
    {
        clientNetworkApp?.Dispose();
        serverNetworkApp?.Dispose();
        return ValueTask.CompletedTask;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        serverNetworkApp?.Start();
        
        if (clientNetworkApp != null)
            await clientNetworkApp.ConnectAsync();
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        clientNetworkApp?.Disconnect();
        serverNetworkApp?.Stop();
        return Task.CompletedTask;
    }

    public void Update(float deltaTime)
    {
        clientNetworkApp?.Update();
        serverNetworkApp?.Update();
    }
}