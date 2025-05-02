using NetworkHexagonal.Core.Application.Ports;
using NetworkHexagonal.Core.Application.Ports.Output;
using Microsoft.Extensions.Logging;
using NetworkHexagonal.Adapters.Outbound.Networking.Util;

namespace NetworkHexagonal.Adapters.Outbound.Networking.Serializer
{
    /// <summary>
    /// Adaptador responsável por serializar e deserializar objetos que implementam INetworkSerializable.
    /// Esta classe implementa o padrão Adapter da arquitetura hexagonal, convertendo objetos de domínio
    /// para bytes e vice-versa, sem conhecer detalhes específicos da implementação de rede.
    /// </summary>
    public class SerializerAdapter : INetworkSerializer
    {
        private readonly ILogger? _logger;
        
        public SerializerAdapter(ILogger? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Serializa um objeto que implementa INetworkSerializable para um array de bytes.
        /// </summary>
        /// <typeparam name="T">Tipo do objeto a ser serializado</typeparam>
        /// <param name="obj">Objeto a ser serializado</param>
        /// <returns>Array de bytes representando o objeto serializado</returns>
        public byte[] Serialize<T>(T obj) where T : INetworkSerializable
        {
            _logger?.LogTrace("Serializando objeto do tipo {Type}", typeof(T).Name);
            
            var writer = BufferNetworkWriter.Get();
            writer.Write(obj);
            var data = writer.CopyData();

            BufferNetworkWriter.Return(writer);
            return data;
        }

        /// <summary>
        /// Deserializa um array de bytes para um objeto que implementa INetworkSerializable.
        /// </summary>
        /// <typeparam name="T">Tipo do objeto a ser deserializado</typeparam>
        /// <param name="data">Array de bytes a ser deserializado</param>
        /// <returns>Objeto deserializado</returns>
        public T Deserialize<T>(byte[] data) where T : INetworkSerializable, new()
        {
            _logger?.LogTrace("Deserializando para objeto do tipo {Type}", typeof(T).Name);
            
            var reader = BufferNetworkReader.Get();
            reader.SetSource(data);
            var obj = new T();
            obj.Deserialize(reader);

            BufferNetworkReader.Return(reader);
            return obj;
        }

        /// <summary>
        /// Serializa um objeto que implementa INetworkSerializable para um array de bytes.
        /// </summary>
        /// <typeparam name="T">Tipo do objeto a ser serializado</typeparam>
        /// <param name="obj">Objeto a ser serializado</param>
        /// <returns>Array de bytes representando o objeto serializado</returns>
        public byte[] SerializePacket<T>(T obj) where T : IPacket
        {
            _logger?.LogTrace("Serializando objeto do tipo {Type}", typeof(T).Name);
            
            var writer = BufferNetworkWriter.Get();
            writer.Write(HashHelper<T>.Id);
            obj.Serialize(writer);
            var data = writer.CopyData();

            BufferNetworkWriter.Return(writer);
            return data;
        }

        /// <summary>
        /// Deserializa um array de bytes para um objeto que implementa INetworkSerializable.
        /// </summary>
        /// <typeparam name="T">Tipo do objeto a ser deserializado</typeparam>
        /// <param name="data">Array de bytes a ser deserializado</param>
        /// <returns>Objeto deserializado</returns>
        public BufferNetworkReader DeserializePacket(byte[] data)
        {
            var reader = BufferNetworkReader.Get();
            reader.SetSource(data);
            return reader;
        }
    }
}
