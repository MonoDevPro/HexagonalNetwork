using NetworkHexagonal.Core.Application.Ports.Output;

namespace NetworkHexagonal.Core.Application.Ports.Input
{
    public interface INetworkService : IDisposable
    {
        void Initialize();
        void Configure(INetworkConfiguration config);
        void Update();
    }
}