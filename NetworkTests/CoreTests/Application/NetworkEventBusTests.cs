using System;
using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using NetworkHexagonal.Core.Application.Services;

namespace NetworkTests.CoreTests.Application
{
    [TestFixture]
    public class NetworkEventBusTests
    {
        private NetworkEventBus _eventBus;
        private Mock<ILogger<NetworkEventBus>> _loggerMock;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<NetworkEventBus>>();
            _eventBus = new NetworkEventBus(_loggerMock.Object);
        }

        [Test]
        public void Publish_WithNoSubscribers_ShouldNotThrowException()
        {
            // Arrange
            var testEvent = new TestEvent { Message = "Test message" };

            // Act & Assert
            Assert.DoesNotThrow(() => _eventBus.Publish(testEvent));
        }

        [Test]
        public void Subscribe_ShouldRegisterHandler()
        {
            // Arrange
            var testEvent = new TestEvent { Message = "Test message" };
            var handlerCalled = false;
            Action<TestEvent> handler = ev => handlerCalled = true;

            // Act
            _eventBus.Subscribe(handler);
            _eventBus.Publish(testEvent);

            // Assert
            Assert.That(handlerCalled, Is.True, "Handler should have been called");
        }

        [Test]
        public void Subscribe_MultipleSameTypeEvents_ShouldCallAllHandlers()
        {
            // Arrange
            var testEvent = new TestEvent { Message = "Test message" };
            var handler1Called = false;
            var handler2Called = false;

            Action<TestEvent> handler1 = ev => handler1Called = true;
            Action<TestEvent> handler2 = ev => handler2Called = true;

            // Act
            _eventBus.Subscribe(handler1);
            _eventBus.Subscribe(handler2);
            _eventBus.Publish(testEvent);

            // Assert
            Assert.That(handler1Called, Is.True, "First handler should have been called");
            Assert.That(handler2Called, Is.True, "Second handler should have been called");
        }

        [Test]
        public void Subscribe_DifferentTypeEvents_ShouldOnlyCallMatchingHandlers()
        {
            // Arrange
            var testEvent1 = new TestEvent { Message = "Test message 1" };
            var testEvent2 = new AnotherTestEvent { Value = 42 };
            
            var handler1Called = false;
            var handler2Called = false;

            Action<TestEvent> handler1 = ev => handler1Called = true;
            Action<AnotherTestEvent> handler2 = ev => handler2Called = true;

            // Act
            _eventBus.Subscribe(handler1);
            _eventBus.Subscribe(handler2);
            
            // Publish first event type
            _eventBus.Publish(testEvent1);

            // Assert
            Assert.That(handler1Called, Is.True, "TestEvent handler should have been called");
            Assert.That(handler2Called, Is.False, "AnotherTestEvent handler should not have been called yet");

            // Reset and publish second event type
            handler1Called = false;
            _eventBus.Publish(testEvent2);

            // Assert again
            Assert.That(handler1Called, Is.False, "TestEvent handler should not have been called");
            Assert.That(handler2Called, Is.True, "AnotherTestEvent handler should have been called");
        }

        [Test]
        public void Unsubscribe_ShouldRemoveHandler()
        {
            // Arrange
            var testEvent = new TestEvent { Message = "Test message" };
            var handlerCalled = false;
            Action<TestEvent> handler = ev => handlerCalled = true;

            // Act - Subscribe, then unsubscribe, then publish
            _eventBus.Subscribe(handler);
            _eventBus.Unsubscribe(handler);
            _eventBus.Publish(testEvent);

            // Assert
            Assert.That(handlerCalled, Is.False, "Handler should not have been called after unsubscribing");
        }

        [Test]
        public void Unsubscribe_WithMultipleHandlers_ShouldOnlyRemoveSpecifiedHandler()
        {
            // Arrange
            var testEvent = new TestEvent { Message = "Test message" };
            var handler1Called = false;
            var handler2Called = false;

            Action<TestEvent> handler1 = ev => handler1Called = true;
            Action<TestEvent> handler2 = ev => handler2Called = true;

            // Act
            _eventBus.Subscribe(handler1);
            _eventBus.Subscribe(handler2);
            _eventBus.Unsubscribe(handler1);
            _eventBus.Publish(testEvent);

            // Assert
            Assert.That(handler1Called, Is.False, "Unsubscribed handler should not have been called");
            Assert.That(handler2Called, Is.True, "Remaining handler should have been called");
        }

        [Test]
        public void Unsubscribe_NonExistentHandler_ShouldNotThrowException()
        {
            // Arrange
            Action<TestEvent> handler = ev => { };

            // Act & Assert
            Assert.DoesNotThrow(() => _eventBus.Unsubscribe(handler));
        }

        [Test]
        public void Publish_HandlerThrowsException_ShouldContinueToNextHandler()
        {
            // Arrange
            var testEvent = new TestEvent { Message = "Test message" };
            var handler2Called = false;

            Action<TestEvent> handler1 = ev => throw new InvalidOperationException("Test exception");
            Action<TestEvent> handler2 = ev => handler2Called = true;

            // Act
            _eventBus.Subscribe(handler1);
            _eventBus.Subscribe(handler2);
            _eventBus.Publish(testEvent);

            // Assert
            // Verificar que o segundo handler foi chamado mesmo após exceção no primeiro
            Assert.That(handler2Called, Is.True, "Second handler should have been called despite exception in first handler");
            
            // Verificar que o logger registrou o erro
            _loggerMock.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        // Classes de eventos para teste
        public class TestEvent
        {
            public string Message { get; set; } = string.Empty;
        }

        public class AnotherTestEvent
        {
            public int Value { get; set; }
        }
    }
}