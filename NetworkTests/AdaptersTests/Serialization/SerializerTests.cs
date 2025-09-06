using System;
using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Network.Adapters.LiteNet;
using Network.Adapters.Serialization;
using Network.Core.Application.Ports.Outbound;
using Network.Core.Domain.Exceptions;
using Network.Core.Domain.Models;
using NUnit.Framework;

namespace NetworkTests.AdaptersTests.Serialization
{
    [TestFixture]
    public class SerializerTests : IDisposable
    {
        private INetworkSerializer _serializer;
        private ServiceProvider _serviceProvider;
        
        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            
            // Adiciona logging
            services.AddLogging(builder => {
                builder.AddConsole();
            });
            
            // Adiciona serviços
            services.AddSingleton<INetworkSerializer, SerializerAdapter>();
            
            _serviceProvider = services.BuildServiceProvider();
            
            // Obtém o serializador
            _serializer = _serviceProvider.GetRequiredService<INetworkSerializer>();
        }
        
        [TearDown]
        public void TearDown()
        {
            // Libera recursos gerenciados
            _serviceProvider?.Dispose();
        }
        
        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
        
        [Test]
        public void TestSerializerRegisterTypesAndGetId()
        {
            // Registra o tipo de pacote
            _serializer.RegisterPacketType<TestPacket>();
            
            // Obtém o ID do pacote
            ulong packetId = _serializer.GetPacketId<TestPacket>();
            
            // Verifica se o ID não é zero
            Assert.That(packetId, Is.Not.EqualTo(0), "Packet ID should not be zero");
            
            // Verifica se o mesmo ID é retornado para o mesmo tipo
            ulong samePacketId = _serializer.GetPacketId<TestPacket>();
            Assert.That(samePacketId, Is.EqualTo(packetId), "Same packet type should return same ID");
            
            // Verifica se IDs diferentes são retornados para tipos diferentes
            ulong otherPacketId = _serializer.GetPacketId<OtherTestPacket>();
            Assert.That(otherPacketId, Is.Not.EqualTo(packetId), "Different packet types should have different IDs");
        }
        
        [Test]
        public void TestSerializationAndDeserialization()
        {
            // Cria um pacote de teste
            var originalPacket = new TestPacket
            {
                TestString = "Hello, World!",
                TestInt = 42,
                TestBool = true,
                TestFloat = 3.14159f,
                TestVector = new Vector2(1.0f, 2.0f),
                OtherTestPacket = new OtherTestPacket
                {
                    Message = "Hello from OtherTestPacket!"
                }
            };
            
            // Serializa o pacote
            var serializedWriter = _serializer.Serialize(originalPacket);
            
            // Assegura que o resultado não é nulo
            Assert.That(serializedWriter, Is.Not.Null, "Serialized data should not be null");
            
            // Verifica se writer não é nulo antes de acessar (corrigir warning CS8602)
            if (serializedWriter != null)
            {
                var reader = LiteNetLibReaderAdapter.Pool.Get();
                reader.SetSource(serializedWriter.Data);
                
                // Lê o ID do pacote
                ulong packetId = reader.ReadULong();
                
                // Verifica se o ID corresponde ao tipo TestPacket
                Assert.That(packetId, Is.EqualTo(_serializer.GetPacketId<TestPacket>()), "Packet IDs should match");
                
                // Desserializa o pacote
                var deserializedPacket = _serializer.Deserialize<TestPacket>(reader);
                
                // Verifica se os dados estão corretos
                Assert.That(deserializedPacket.TestString, Is.EqualTo(originalPacket.TestString), "String values should match");
                Assert.That(deserializedPacket.TestInt, Is.EqualTo(originalPacket.TestInt), "Int values should match");
                Assert.That(deserializedPacket.TestBool, Is.EqualTo(originalPacket.TestBool), "Bool values should match");
                Assert.That(deserializedPacket.TestFloat, Is.EqualTo(originalPacket.TestFloat), "Float values should match");
                Assert.That(deserializedPacket.TestVector.X, Is.EqualTo(originalPacket.TestVector.X), "Vector X should match");
                Assert.That(deserializedPacket.TestVector.Y, Is.EqualTo(originalPacket.TestVector.Y), "Vector Y should match");
                Assert.That(deserializedPacket.OtherTestPacket, Is.Not.Null, "OtherTestPacket should not be null");
                Assert.That(deserializedPacket.OtherTestPacket.Message, Is.EqualTo(originalPacket.OtherTestPacket.Message), "OtherTestPacket message should match");

                reader.Recycle();
                // Recycle the writer to the pool
                serializedWriter.Recycle();
            }

        }
        
        [Test]
        public void TestDeserializeByPacketId()
        {
            // Registra o tipo para garantir que ele existe no registro
            _serializer.RegisterPacketType<TestPacket>();
            
            // Cria um pacote de teste
            var originalPacket = new TestPacket
            {
                TestString = "Hello, ID-based deserialization!",
                TestInt = 123,
                TestBool = false,
                TestFloat = 2.71828f,
                TestVector = new Vector2(3.0f, 4.0f),
                OtherTestPacket = new OtherTestPacket
                {
                    Message = "Hello from OtherTestPacket!"
                }
            };
            
            // Serializa o pacote
            var serializedWriter = _serializer.Serialize(originalPacket);
            
            // Verifica se writer não é nulo antes de acessar (corrigir warning CS8602)
            if (serializedWriter != null)
            {
                var reader = LiteNetLibReaderAdapter.Pool.Get();
                reader.SetSource(serializedWriter.Data);
                
                // Lê o ID do pacote
                ulong packetId = reader.ReadULong();
                
                // Desserializa o pacote usando o ID
                var deserializedPacket = _serializer.Deserialize(packetId, reader);
                
                // Verifica se é um TestPacket
                Assert.That(deserializedPacket, Is.TypeOf<TestPacket>(), "Should deserialize as TestPacket");
                var typedPacket = deserializedPacket as TestPacket;
                
                // Verifica se typedPacket não é nulo antes de acessar (corrigir warning CS8602)
                if (typedPacket != null)
                {
                    // Verifica os valores
                    Assert.That(typedPacket.TestString, Is.EqualTo(originalPacket.TestString), "String values should match");
                    Assert.That(typedPacket.TestInt, Is.EqualTo(originalPacket.TestInt), "Int values should match");
                    Assert.That(typedPacket.TestBool, Is.EqualTo(originalPacket.TestBool), "Bool values should match");
                    Assert.That(typedPacket.TestFloat, Is.EqualTo(originalPacket.TestFloat), "Float values should match");
                    Assert.That(typedPacket.TestVector.X, Is.EqualTo(originalPacket.TestVector.X), "Vector X should match");
                    Assert.That(typedPacket.TestVector.Y, Is.EqualTo(originalPacket.TestVector.Y), "Vector Y should match");
                    Assert.That(typedPacket.OtherTestPacket.Message, Is.EqualTo(originalPacket.OtherTestPacket.Message), "OtherTestPacket message should match");
                    // Verifica se OtherTestPacket é nulo, pois não foi definido no original
                }
                reader.Recycle();

                // Recycle the writer to the pool
                serializedWriter.Recycle();
            }
        }
        
        [Test]
        public void TestDeserializeInvalidPacketId()
        {
            // Cria um reader com um ID inválido
            var writer = LiteNetLibWriterAdapter.Pool.Get();
            writer.WriteULong(999999999UL); // ID que certamente não existe
            var reader = LiteNetLibReaderAdapter.Pool.Get();
            reader.SetSource(writer.Data);
            writer.Recycle();
            
            // Tenta desserializar e espera uma exceção
            Assert.Throws<SerializationException>(() => _serializer.Deserialize(999999999UL, reader), 
                "Should throw SerializationException for unknown packet ID");

            // Recycle the reader to the pool
            reader.Recycle();
        }
        
        // Classes para teste - corrigindo avisos de propriedades não-nulas
        public class TestPacket : IPacket, ISerializable
        {
            public string TestString { get; set; } = string.Empty;
            public int TestInt { get; set; }
            public bool TestBool { get; set; }
            public float TestFloat { get; set; }
            public Vector2 TestVector { get; set; }
            public OtherTestPacket OtherTestPacket { get; set; } = new OtherTestPacket();
            
            public void Serialize(INetworkWriter writer)
            {
                writer.WriteString(TestString);
                writer.WriteInt(TestInt);
                writer.WriteBool(TestBool);
                writer.WriteFloat(TestFloat);
                writer.WriteVector2(TestVector);

                writer.WriteSerializable(OtherTestPacket);
            }
            
            public void Deserialize(INetworkReader reader)
            {
                TestString = reader.ReadString();
                TestInt = reader.ReadInt();
                TestBool = reader.ReadBool();
                TestFloat = reader.ReadFloat();
                TestVector = reader.ReadVector2();
                
                OtherTestPacket = reader.ReadSerializable<OtherTestPacket>();
            }
        }
        
        public class OtherTestPacket : IPacket, ISerializable
        {

            public string Message { get; set; }
            
            public void Serialize(INetworkWriter writer)
            {
                writer.WriteString(Message);
            }
            
            public void Deserialize(INetworkReader reader)
            {
                Message = reader.ReadString();
            }
        }
    }
}