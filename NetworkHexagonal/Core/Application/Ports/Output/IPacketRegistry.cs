namespace NetworkHexagonal.Core.Application.Ports.Output
{
    /// <summary>
    /// Interface que define o contrato para registro de tipos de pacotes de rede.
    /// Esta porta permite que o núcleo da aplicação registre tipos de pacotes
    /// sem conhecer a implementação concreta do mecanismo de comunicação.
    /// </summary>
    public interface IPacketRegistry
    {
        /// <summary>
        /// Registra um tipo de pacote e associa um callback para quando este tipo de pacote for recebido.
        /// </summary>
        /// <typeparam name="TPacket">Tipo do pacote a ser registrado</typeparam>
        /// <param name="callback">Ação a ser executada quando um pacote deste tipo for recebido</param>
        void Register<TPacket>(Action<TPacket> callback) where TPacket : IPacket, new();
        
        /// <summary>
        /// Registra um tipo de pacote e associa um callback com dados de usuário para quando este tipo de pacote for recebido.
        /// </summary>
        /// <typeparam name="TPacket">Tipo do pacote a ser registrado</typeparam>
        /// <typeparam name="TUserData">Tipo dos dados do usuário</typeparam>
        /// <param name="callback">Ação a ser executada quando um pacote deste tipo for recebido</param>
        void Register<TPacket, TUserData>(Action<TPacket, TUserData> callback) where TPacket : IPacket, new();
    }
}