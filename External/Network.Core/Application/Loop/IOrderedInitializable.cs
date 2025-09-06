namespace Network.Core.Application.Loop;

/// <summary>
/// Combina IInitializable com uma ordem de execução.
/// </summary>
public interface IOrderedInitializable : IInitializable, IOrderedService { }