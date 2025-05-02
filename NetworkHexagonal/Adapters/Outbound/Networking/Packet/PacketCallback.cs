using Microsoft.Extensions.Logging;
using NetworkHexagonal.Adapters.Outbound.Networking.Serializer;
using NetworkHexagonal.Adapters.Outbound.Networking.Util;
using NetworkHexagonal.Core.Application.Ports;
using NetworkHexagonal.Core.Application.Ports.Output;

namespace NetworkHexagonal.Adapters.Outbound.Networking.Packet
{
    /// <summary>
    /// Implementação do adaptador para gerenciamento de callbacks de pacotes.
    /// Esta classe é responsável por registrar, armazenar e invocar callbacks
    /// para tipos específicos de pacotes, usando um sistema de hash para identificação.
    /// </summary>
    public class PacketCallback : IPacketCallbacks
    {
        private delegate void SubscribeDelegate(BufferNetworkReader reader, object userData);
        private readonly Dictionary<ulong, SubscribeDelegate> _callbacks 
            = new Dictionary<ulong, SubscribeDelegate>();

        private readonly ILogger? _logger;

        public event Action<IPacket> OnPacketReceived;

        public PacketCallback(ILogger? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Registra um callback para um tipo específico de pacote.
        /// </summary>
        /// <typeparam name="T">Tipo do pacote</typeparam>
        /// <param name="callback">Ação a ser executada quando um pacote deste tipo for recebido</param>
        public void RegisterCallback<TPacket>(Action<TPacket> callback)
            where TPacket : IPacket, new()
        {
            var id = HashHelper<TPacket>.Id;
            if (_callbacks.ContainsKey(id))
            {
                _logger?.LogWarning("Callback já registrado para pacote do tipo {Type}", typeof(TPacket).Name);
                return;
            }

            _callbacks[id] = (bufferReader, userData) => 
                {
                    var packet = new TPacket();
                    packet.Deserialize(bufferReader);

                    callback(packet);
                };

            _logger?.LogTrace("Registrado callback para pacote do tipo {Type} com hash {Hash}", typeof(TPacket).Name, id);
        }

        /// <summary>
        /// Registra um callback para um tipo específico de pacote com dados do usuário.
        /// </summary>
        /// <typeparam name="T">Tipo do pacote</typeparam>
        /// <typeparam name="TUserData">Tipo dos dados do usuário</typeparam>
        /// <param name="callback">Ação a ser executada quando um pacote deste tipo for recebido</param>
        public void RegisterCallback<TPacket, TUserData>(Action<TPacket, TUserData> callback)
            where TPacket : IPacket, new()
        {
            var id = HashHelper<TPacket>.Id;
            if (_callbacks.ContainsKey(id))
            {
                _logger?.LogWarning("Callback já registrado para pacote do tipo {Type}", typeof(TPacket).Name);
                return;
            }

            _callbacks[id] = (bufferReader, userData) => 
                {
                    var packet = new TPacket();
                    packet.Deserialize(bufferReader);

                    callback(packet, (TUserData)userData);
                };

            _logger?.LogTrace("Registrado callback para pacote do tipo {Type} com hash {Hash}", typeof(TPacket).Name, id);
        }

        /// <summary>
        /// Invoca o callback correspondente ao pacote lido do INetworkReader com dados do usuário.
        /// </summary>
        /// <param name="reader">Leitor de rede contendo dados do pacote</param>
        /// <param name="userData">Dados do usuário a serem passados para o callback</param>
        public void InvokeCallback(BufferNetworkReader reader)
        {
            ulong id = reader.ReadULong();

            if (_callbacks.TryGetValue(id, out var action))
                action(reader, null!);
            else
                throw new Exception($"Undefined packet in INetworkReader with hash {id}");
        }

        public void InvokeCallback(BufferNetworkReader reader, object userData)
        {
            ulong id = reader.ReadULong();
            
            if (_callbacks.TryGetValue(id, out var action))
                action(reader, userData);
            else
                throw new Exception($"Undefined packet in INetworkReader with hash {id}");
        }

        /// <summary>
        /// Cancela o registro de um callback para um tipo específico de pacote.
        /// </summary>
        /// <typeparam name="T">Tipo do pacote</typeparam>
        public void UnregisterCallback(ulong id)
        {
            if (_callbacks.ContainsKey(id))
            {
                _callbacks.Remove(id);
                _logger?.LogTrace("Cancelado registro de callback para o pacote do id {Id}", id);
            }
        }

        /// <summary>
        /// Limpa todos os callbacks registrados.
        /// </summary>
        public void ClearCallbacks()
        {
            int count = _callbacks.Count;
            _callbacks.Clear();
            _logger?.LogTrace("Limpos {Count} callbacks registrados", count);
        }
    
    }
}