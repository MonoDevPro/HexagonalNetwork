using System;
using Microsoft.Extensions.Logging;
using Moq;
using Network.Core.Application.Ports.Outbound;
using Network.Core.Domain.Exceptions;
using Network.Core.Domain.Models;
using NUnit.Framework;
using NetworkHexagonal.Adapters.Outbound.Network;

namespace NetworkTests.AdaptersTests.Network
{
    [TestFixture]
    public class LiteNetLibPacketRegistryAdapterTests
    {
        private LiteNetLibPacketRegistryAdapter _packetRegistry;
        private Mock<INetworkSerializer> _serializerMock;
        private Mock<ILogger<LiteNetLibPacketRegistryAdapter>> _loggerMock;
        private Mock<INetworkReader> _readerMock;
        private PacketContext _context;

        [SetUp]
        public void Setup()
        {
            _serializerMock = new Mock<INetworkSerializer>();
            _loggerMock = new Mock<ILogger<LiteNetLibPacketRegistryAdapter>>();
            _readerMock = new Mock<INetworkReader>();
            _packetRegistry = new LiteNetLibPacketRegistryAdapter(_serializerMock.Object, _loggerMock.Object);
            // Correção: usando byte como segundo parâmetro em vez de DeliveryMode
            _context = new PacketContext(1, 0);
        }

        [Test]
        public void RegisterHandler_ShouldRegisterHandlerForPacketType()
        {
            // Arrange
            bool handlerCalled = false;
            var testPacket = new TestPacket { Message = "Test" };
            ulong packetId = 123;

            _serializerMock.Setup(s => s.GetPacketId<TestPacket>()).Returns(packetId);
            _serializerMock.Setup(s => s.Deserialize<TestPacket>(It.IsAny<INetworkReader>()))
                .Returns(testPacket);

            // Act
            _packetRegistry.RegisterHandler<TestPacket>((packet, ctx) => 
            {
                handlerCalled = true;
                Assert.That(packet.Message, Is.EqualTo("Test"));
                Assert.That(ctx.PeerId, Is.EqualTo(1));
            });

            // Assert
            Assert.That(_packetRegistry.HasHandler(packetId), Is.True, "Handler should be registered");
            
            // Act - Handle a packet with the registered ID
            _packetRegistry.HandlePacket(packetId, _readerMock.Object, _context);
            
            // Assert - Handler should be called
            Assert.That(handlerCalled, Is.True, "Handler should be called when processing packet");
            _serializerMock.Verify(s => s.Deserialize<TestPacket>(It.IsAny<INetworkReader>()), Times.Once);
        }

        [Test]
        public void RegisterHandler_WhenHandlerAlreadyExists_ShouldNotReplaceHandler()
        {
            // Arrange
            bool firstHandlerCalled = false;
            bool secondHandlerCalled = false;
            ulong packetId = 123;

            _serializerMock.Setup(s => s.GetPacketId<TestPacket>()).Returns(packetId);
            _serializerMock.Setup(s => s.Deserialize<TestPacket>(It.IsAny<INetworkReader>()))
                .Returns(new TestPacket { Message = "Test" });

            // Act - Register first handler
            _packetRegistry.RegisterHandler<TestPacket>((packet, ctx) => 
            {
                firstHandlerCalled = true;
            });

            // Act - Try to register second handler
            _packetRegistry.RegisterHandler<TestPacket>((packet, ctx) => 
            {
                secondHandlerCalled = true;
            });

            // Act - Handle packet
            _packetRegistry.HandlePacket(packetId, _readerMock.Object, _context);

            // Assert - First handler should be called, second handler should be ignored
            Assert.That(firstHandlerCalled, Is.True, "First handler should be called");
            Assert.That(secondHandlerCalled, Is.False, "Second handler should not be called");
        }

        [Test]
        public void HasHandler_WithRegisteredPacketId_ShouldReturnTrue()
        {
            // Arrange
            ulong packetId = 456;

            _serializerMock.Setup(s => s.GetPacketId<TestPacket>()).Returns(packetId);

            // Act
            _packetRegistry.RegisterHandler<TestPacket>((packet, ctx) => { });

            // Assert
            Assert.That(_packetRegistry.HasHandler(packetId), Is.True);
        }

        [Test]
        public void HasHandler_WithUnregisteredPacketId_ShouldReturnFalse()
        {
            // Arrange
            ulong packetId = 789;

            // Assert
            Assert.That(_packetRegistry.HasHandler(packetId), Is.False);
        }

        [Test]
        public void HandlePacket_WithUnregisteredPacketId_ShouldThrowException()
        {
            // Arrange
            ulong packetId = 999;

            // Act & Assert
            Assert.Throws<PacketHandlingException>(() => 
                _packetRegistry.HandlePacket(packetId, _readerMock.Object, _context));
        }

        [Test]
        public void HandlePacket_WhenDeserializationFails_ShouldThrowPacketHandlingException()
        {
            // Arrange
            ulong packetId = 555;

            _serializerMock.Setup(s => s.GetPacketId<TestPacket>()).Returns(packetId);
            _serializerMock.Setup(s => s.Deserialize<TestPacket>(It.IsAny<INetworkReader>()))
                .Throws(new SerializationException("Failed to deserialize"));

            // Act
            _packetRegistry.RegisterHandler<TestPacket>((packet, ctx) => { });

            // Assert
            Assert.Throws<PacketHandlingException>(() => 
                _packetRegistry.HandlePacket(packetId, _readerMock.Object, _context));
        }

        [Test]
        public void HandlePacket_WhenHandlerThrowsException_ShouldPropagateAsPacketHandlingException()
        {
            // Arrange
            ulong packetId = 777;
            var testPacket = new TestPacket { Message = "Test" };

            _serializerMock.Setup(s => s.GetPacketId<TestPacket>()).Returns(packetId);
            _serializerMock.Setup(s => s.Deserialize<TestPacket>(It.IsAny<INetworkReader>()))
                .Returns(testPacket);

            // Act - Register handler that throws exception
            _packetRegistry.RegisterHandler<TestPacket>((packet, ctx) => 
            {
                throw new InvalidOperationException("Handler exception");
            });

            // Assert
            Assert.Throws<PacketHandlingException>(() => 
                _packetRegistry.HandlePacket(packetId, _readerMock.Object, _context));
        }

        [Test]
        public void RegisterMultipleHandlers_ForDifferentPacketTypes_ShouldRegisterCorrectly()
        {
            // Arrange
            ulong packetId1 = 111;
            ulong packetId2 = 222;
            bool handler1Called = false;
            bool handler2Called = false;

            _serializerMock.Setup(s => s.GetPacketId<TestPacket>()).Returns(packetId1);
            _serializerMock.Setup(s => s.GetPacketId<AnotherTestPacket>()).Returns(packetId2);
            
            _serializerMock.Setup(s => s.Deserialize<TestPacket>(It.IsAny<INetworkReader>()))
                .Returns(new TestPacket { Message = "First" });
            
            _serializerMock.Setup(s => s.Deserialize<AnotherTestPacket>(It.IsAny<INetworkReader>()))
                .Returns(new AnotherTestPacket { Value = 42 });

            // Act
            _packetRegistry.RegisterHandler<TestPacket>((packet, ctx) => 
            {
                handler1Called = true;
                Assert.That(packet.Message, Is.EqualTo("First"));
            });

            _packetRegistry.RegisterHandler<AnotherTestPacket>((packet, ctx) => 
            {
                handler2Called = true;
                Assert.That(packet.Value, Is.EqualTo(42));
            });

            // Assert
            Assert.That(_packetRegistry.HasHandler(packetId1), Is.True);
            Assert.That(_packetRegistry.HasHandler(packetId2), Is.True);

            // Act - Handle first packet type
            _packetRegistry.HandlePacket(packetId1, _readerMock.Object, _context);
            
            // Assert - Only first handler should be called
            Assert.That(handler1Called, Is.True);
            Assert.That(handler2Called, Is.False);

            // Reset and handle second packet type
            handler1Called = false;
            _packetRegistry.HandlePacket(packetId2, _readerMock.Object, _context);
            
            // Assert - Only second handler should be called
            Assert.That(handler1Called, Is.False);
            Assert.That(handler2Called, Is.True);
        }

        // Classes de teste para pacotes
        private class TestPacket : IPacket, ISerializable
        {
            public string Message { get; set; } = string.Empty;
            
            public void Serialize(INetworkWriter writer)
            {
                writer.WriteString(Message);
            }
            
            public void Deserialize(INetworkReader reader)
            {
                Message = reader.ReadString();
            }
        }
        
        private class AnotherTestPacket : IPacket, ISerializable
        {
            public int Value { get; set; }
            
            public void Serialize(INetworkWriter writer)
            {
                writer.WriteInt(Value);
            }
            
            public void Deserialize(INetworkReader reader)
            {
                Value = reader.ReadInt();
            }
        }
    }
}