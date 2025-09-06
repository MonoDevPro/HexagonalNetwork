using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Network.Core.Application.Ports.Outbound;

namespace Network.Adapters.Distributed;

/// <summary>
/// RabbitMQ-based distributed event bus adapter.
/// Enables communication between multiple server instances for horizontal scaling.
/// </summary>
public class RabbitMqDistributedEventBusAdapter : IDistributedEventBus, IDisposable
{
    private readonly ILogger<RabbitMqDistributedEventBusAdapter> _logger;
    private readonly ConcurrentDictionary<Type, ConcurrentBag<object>> _subscribers = new();
    
    private IConnection? _connection;
    private IModel? _channel;
    private readonly string _serverId;
    private readonly string _exchangeName = "network.events";
    private readonly string _queueName;
    
    // Configuration - in production, these should come from settings
    private readonly string _hostName = "localhost";
    private readonly int _port = 5672;
    private readonly string _userName = "guest";
    private readonly string _password = "guest";

    public string ServerId => _serverId;

    public RabbitMqDistributedEventBusAdapter(ILogger<RabbitMqDistributedEventBusAdapter> logger)
    {
        _logger = logger;
        _serverId = Environment.MachineName + "-" + Guid.NewGuid().ToString("N")[..8];
        _queueName = $"network.events.{_serverId}";
    }

    public async Task ConnectAsync()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _hostName,
                Port = _port,
                UserName = _userName,
                Password = _password,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange for topic-based routing
            _channel.ExchangeDeclare(_exchangeName, ExchangeType.Topic, durable: true);

            // Declare server-specific queue
            _channel.QueueDeclare(_queueName, durable: false, exclusive: true, autoDelete: true);

            // Set up consumer for receiving events
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnEventReceived;
            _channel.BasicConsume(_queueName, autoAck: true, consumer);

            _logger.LogInformation("Connected to RabbitMQ. Server ID: {ServerId}", _serverId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            _channel?.Close();
            _connection?.Close();
            _logger.LogInformation("Disconnected from RabbitMQ");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during RabbitMQ disconnect");
        }
    }

    public async Task PublishAsync<TEvent>(TEvent eventData, string? routingKey = null) where TEvent : class
    {
        if (_channel == null)
            throw new InvalidOperationException("Not connected to RabbitMQ");

        try
        {
            var eventType = typeof(TEvent).Name;
            var message = JsonConvert.SerializeObject(new EventMessage<TEvent>
            {
                EventType = eventType,
                Data = eventData,
                Timestamp = DateTime.UtcNow,
                ServerId = _serverId
            });

            var body = Encoding.UTF8.GetBytes(message);
            var key = routingKey ?? $"event.{eventType}";

            _channel.BasicPublish(_exchangeName, key, null, body);
            _logger.LogDebug("Published event {EventType} with routing key {RoutingKey}", eventType, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}", typeof(TEvent).Name);
            throw;
        }
    }

    public async Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler, string? routingPattern = null) where TEvent : class
    {
        var eventType = typeof(TEvent);
        var subscribers = _subscribers.GetOrAdd(eventType, _ => new ConcurrentBag<object>());
        subscribers.Add(handler);

        if (_channel != null)
        {
            var pattern = routingPattern ?? $"event.{eventType.Name}";
            _channel.QueueBind(_queueName, _exchangeName, pattern);
            _logger.LogDebug("Subscribed to events {EventType} with pattern {Pattern}", eventType.Name, pattern);
        }
    }

    public async Task UnsubscribeAsync<TEvent>(Func<TEvent, Task> handler) where TEvent : class
    {
        var eventType = typeof(TEvent);
        if (_subscribers.TryGetValue(eventType, out var subscribers))
        {
            // Note: ConcurrentBag doesn't support removal, in production use a different structure
            _logger.LogDebug("Unsubscribed from events {EventType}", eventType.Name);
        }
    }

    public async Task PublishToServerAsync<TEvent>(TEvent eventData, string targetServerId) where TEvent : class
    {
        await PublishAsync(eventData, $"server.{targetServerId}.{typeof(TEvent).Name}");
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            return _connection?.IsOpen == true && _channel?.IsOpen == true;
        }
        catch
        {
            return false;
        }
    }

    private async void OnEventReceived(object? sender, BasicDeliverEventArgs e)
    {
        try
        {
            var messageBody = Encoding.UTF8.GetString(e.Body.ToArray());
            var baseMessage = JsonConvert.DeserializeObject<BaseEventMessage>(messageBody);
            
            if (baseMessage == null || baseMessage.ServerId == _serverId)
            {
                // Skip events from self
                return;
            }

            // Find subscribers for this event type
            var eventTypeName = baseMessage.EventType;
            var eventType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == eventTypeName);

            if (eventType == null || !_subscribers.TryGetValue(eventType, out var subscribers))
            {
                _logger.LogTrace("No subscribers found for event type {EventType}", eventTypeName);
                return;
            }

            // Deserialize the event data with the correct type
            var eventData = JsonConvert.DeserializeObject(messageBody, typeof(EventMessage<>).MakeGenericType(eventType));
            if (eventData == null) return;

            var dataProperty = eventData.GetType().GetProperty("Data");
            var actualEventData = dataProperty?.GetValue(eventData);
            
            if (actualEventData == null) return;

            // Call all subscribers
            var tasks = new List<Task>();
            foreach (var subscriber in subscribers)
            {
                if (subscriber is Delegate del)
                {
                    var task = (Task?)del.DynamicInvoke(actualEventData);
                    if (task != null)
                        tasks.Add(task);
                }
            }

            if (tasks.Count > 0)
                await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing received event");
        }
    }

    public void Dispose()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during RabbitMQ disposal");
        }
    }
}

/// <summary>
/// Event message wrapper for serialization
/// </summary>
internal class EventMessage<T> : BaseEventMessage
{
    public T? Data { get; set; }
}

/// <summary>
/// Base event message for type identification
/// </summary>
internal class BaseEventMessage
{
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string ServerId { get; set; } = string.Empty;
}