using System.Threading.Tasks;

namespace Network.Core.Application.Ports.Outbound;

/// <summary>
/// Port for distributed event bus that enables communication between multiple server instances.
/// Replaces the in-memory EventBus for horizontally scalable deployments.
/// </summary>
public interface IDistributedEventBus
{
    /// <summary>
    /// Publishes an event to all subscribers across the distributed system
    /// </summary>
    /// <typeparam name="TEvent">Type of event to publish</typeparam>
    /// <param name="eventData">Event data to publish</param>
    /// <param name="routingKey">Optional routing key for targeted distribution</param>
    Task PublishAsync<TEvent>(TEvent eventData, string? routingKey = null) where TEvent : class;
    
    /// <summary>
    /// Subscribes to events of a specific type from the distributed system
    /// </summary>
    /// <typeparam name="TEvent">Type of event to subscribe to</typeparam>
    /// <param name="handler">Handler function to process received events</param>
    /// <param name="routingPattern">Optional routing pattern for selective subscription</param>
    Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler, string? routingPattern = null) where TEvent : class;
    
    /// <summary>
    /// Unsubscribes from events of a specific type
    /// </summary>
    /// <typeparam name="TEvent">Type of event to unsubscribe from</typeparam>
    /// <param name="handler">Handler function to remove</param>
    Task UnsubscribeAsync<TEvent>(Func<TEvent, Task> handler) where TEvent : class;
    
    /// <summary>
    /// Publishes an event to a specific server instance
    /// </summary>
    /// <typeparam name="TEvent">Type of event to publish</typeparam>
    /// <param name="eventData">Event data to publish</param>
    /// <param name="targetServerId">Identifier of the target server instance</param>
    Task PublishToServerAsync<TEvent>(TEvent eventData, string targetServerId) where TEvent : class;
    
    /// <summary>
    /// Gets the current server instance identifier
    /// </summary>
    string ServerId { get; }
    
    /// <summary>
    /// Connects to the distributed message broker
    /// </summary>
    Task ConnectAsync();
    
    /// <summary>
    /// Disconnects from the distributed message broker
    /// </summary>
    Task DisconnectAsync();
    
    /// <summary>
    /// Checks if the connection to the message broker is healthy
    /// </summary>
    Task<bool> IsHealthyAsync();
}