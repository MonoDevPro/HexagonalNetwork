using Microsoft.Extensions.Logging;
using Moq;
using NetworkHexagonal.Adapters.Outbound.Networking.Packet;
using NetworkHexagonal.Adapters.Outbound.Networking.Serializer;
using NetworkHexagonal.Core.Application.Ports;
using NetworkHexagonal.Core.Application.Ports.Output;
using System.Text;

namespace NetworkHexagonal.Tests.AdaptersTests
{
    public class TestCallbackPacket : IPacket, INetworkSerializable
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;

        public void Serialize(INetworkWriter writer)
        {
            writer.Write(Id);
            writer.Write(Message);
        }

        public void Deserialize(INetworkReader reader)
        {
            Id = reader.ReadInt();
            Message = reader.ReadString();
        }
    }
    
    public class PacketCallbackTests
    {
        private readonly Mock<ILogger> _loggerMock;
        private readonly PacketCallback _packetCallback;
        
        public PacketCallbackTests()
        {
            _loggerMock = new Mock<ILogger>();
            _packetCallback = new PacketCallback(_loggerMock.Object);
        }
        
        [Fact]
        public void RegisterCallback_InvokesCallback_WhenPacketReceived()
        {
            // Arrange
            bool callbackInvoked = false;
            TestCallbackPacket? receivedPacket = null;
            
            _packetCallback.RegisterCallback<TestCallbackPacket>(packet => {
                callbackInvoked = true;
                receivedPacket = packet;
            });
            
            var testPacket = new TestCallbackPacket { Id = 42, Message = "Test Message" };
            
            // Serializa o pacote
            var serializer = new SerializerAdapter();
            byte[] serializedData = serializer.Serialize(testPacket);
            
            // Adiciona o hash do tipo no início dos dados
            var writer = new BufferNetworkWriter();
                writer.Write(HashHelper.GetHash(typeof(TestCallbackPacket)));
                writer.Write(serializedData);
                
                writer.Position = 0;
                var reader = new BufferNetworkReader(writer);
                    // Act
                _packetCallback.InvokeCallback(reader);
            
            // Assert
            Assert.True(callbackInvoked, "O callback não foi invocado");
            Assert.NotNull(receivedPacket);
            Assert.Equal(42, receivedPacket?.Id);
            Assert.Equal("Test Message", receivedPacket?.Message);
        }
        
        [Fact]
        public void RegisterMultipleCallbacks_InvokesCorrectCallback_ForPacketType()
        {
            // Arrange
            bool callback1Invoked = false;
            bool callback2Invoked = false;
            
            // Registra callbacks para diferentes tipos
            _packetCallback.RegisterCallback<TestCallbackPacket>(packet => {
                callback1Invoked = true;
            });
            
            _packetCallback.RegisterCallback<DummyPacket>(packet => {
                callback2Invoked = true;
            });
            
            var testPacket = new TestCallbackPacket { Id = 100, Message = "Multiple Callbacks" };
            
            // Serializa o pacote
            var serializer = new SerializerAdapter();
            byte[] serializedData = serializer.Serialize(testPacket);
            
            // Adiciona o hash do tipo no início dos dados
            var writer = new BufferNetworkWriter();
                writer.Write(HashHelper.GetHash(typeof(TestCallbackPacket)));
                writer.Write(serializedData);
                
                writer.Position = 0;
                var reader = new BufferNetworkReader(writer);
                
                    // Act
                _packetCallback.InvokeCallback(reader);
                
            
            // Assert
            Assert.True(callback1Invoked, "O callback do tipo correto não foi invocado");
            Assert.False(callback2Invoked, "O callback do tipo incorreto foi invocado");
        }
        
        [Fact]
        public void UnregisterCallback_PreventsInvocation_WhenPacketReceived()
        {
            // Arrange
            bool callbackInvoked = false;
            
            _packetCallback.RegisterCallback<TestCallbackPacket>(packet => {
                callbackInvoked = true;
            });
            
            // Cancela o registro do callback
            _packetCallback.UnregisterCallback(HashHelper.GetHash(typeof(TestCallbackPacket)));
            
            var testPacket = new TestCallbackPacket { Id = 200, Message = "Unregistered Callback" };
            
            // Serializa o pacote
            var serializer = new SerializerAdapter();
            byte[] serializedData = serializer.Serialize(testPacket);
            
            // Adiciona o hash do tipo no início dos dados
            var writer = new BufferNetworkWriter();
            
                writer.Write(HashHelper.GetHash(typeof(TestCallbackPacket)));
                writer.Write(serializedData);
                
                writer.Position = 0;
                var reader = new BufferNetworkReader(writer);
                
                    // Act - Deve lançar exceção porque não há callbacks registrados
                    var exception = Assert.Throws<Exception>(() => _packetCallback.InvokeCallback(reader));
                    Assert.Contains("Undefined packet", exception.Message);
                
            
            
            // Assert
            Assert.False(callbackInvoked, "O callback foi invocado mesmo após cancelar o registro");
        }
        
        [Fact]
        public void RegisterCallback_WithUserData_PassesUserDataToCallback()
        {
            // Arrange
            string? receivedUserData = null;
            
            _packetCallback.RegisterCallback<TestCallbackPacket, string>((packet, userData) => {
                receivedUserData = userData;
            });
            
            var testPacket = new TestCallbackPacket { Id = 300, Message = "UserData Test" };
            
            // Serializa o pacote
            var serializer = new SerializerAdapter();
            byte[] serializedData = serializer.Serialize(testPacket);
            
            // Adiciona o hash do tipo no início dos dados
            var writer = new BufferNetworkWriter();
        
            writer.Write(HashHelper.GetHash(typeof(TestCallbackPacket)));
            writer.Write(serializedData);
                
                writer.Position = 0;
                var reader = new BufferNetworkReader(writer);
                
                    // Act
                    _packetCallback.InvokeCallback(reader, "Test UserData");
                
            
            // Assert
            Assert.Equal("Test UserData", receivedUserData);
        }
    }
    
    // Classe auxiliar para calcular o hash de tipos da mesma forma que o PacketCallback
    public static class HashHelper
    {
        public static ulong GetHash(Type type)
        {
            // FNV-1 64 bit hash (mesmo algoritmo usado no PacketCallback)
            ulong hash = 14695981039346656037UL; // offset
            string typeName = type.ToString();
            for (var i = 0; i < typeName.Length; i++)
            {
                hash ^= typeName[i];
                hash *= 1099511628211UL; // prime
            }
            return hash;
        }
    }
}