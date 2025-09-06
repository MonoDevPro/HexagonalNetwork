namespace Network.Core.Application.Loop;

/// <summary>
/// Combina IUpdatable com uma ordem de execução.
/// </summary>
public interface IOrderedUpdatable : IUpdatable, IOrderedService { }
