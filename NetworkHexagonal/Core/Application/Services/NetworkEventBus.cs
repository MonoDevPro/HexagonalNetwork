using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace NetworkHexagonal.Core.Application.Services
{
    /// <summary>
    /// Interface para o barramento de eventos de rede
    /// </summary>
    public interface INetworkEventBus
    {
        void Publish<TEvent>(TEvent eventData) where TEvent : class;
        void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
    }
    
    /// <summary>
    /// Implementação em memória do barramento de eventos de rede
    /// </summary>
    public class NetworkEventBus : INetworkEventBus
    {
        private readonly Dictionary<Type, List<object>> _handlers = new();
        private readonly ILogger<NetworkEventBus> _logger;
        
        public NetworkEventBus(ILogger<NetworkEventBus> logger)
        {
            _logger = logger;
        }
        
        public void Publish<TEvent>(TEvent eventData) where TEvent : class
        {
            var eventType = typeof(TEvent);
            
            if (!_handlers.TryGetValue(eventType, out var handlers))
            {
                _logger.LogTrace("Não há manipuladores registrados para o evento do tipo {EventType}", eventType.Name);
                return;
            }
            
            foreach (var handler in handlers)
            {
                try
                {
                    ((Action<TEvent>)handler)(eventData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao executar manipulador de evento para {EventType}", eventType.Name);
                }
            }
        }
        
        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
        {
            var eventType = typeof(TEvent);
            
            if (!_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<object>();
                _handlers[eventType] = handlers;
            }
            
            handlers.Add(handler);
            _logger.LogDebug("Manipulador inscrito para eventos do tipo {EventType}", eventType.Name);
        }
        
        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class
        {
            var eventType = typeof(TEvent);
            
            if (!_handlers.TryGetValue(eventType, out var handlers))
            {
                return;
            }
            
            handlers.Remove(handler);
            _logger.LogDebug("Manipulador removido de eventos do tipo {EventType}", eventType.Name);
        }
    }
}