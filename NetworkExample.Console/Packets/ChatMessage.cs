using Network.Core.Application.Ports.Outbound;
using Network.Core.Domain.Models;

namespace NetworkExample.Console.Packets;

// Exemplo de pacote para comunicação cliente-servidor
public class ChatMessage : IPacket, ISerializable
{
    public string Sender { get; set; }
    public string Message { get; set; }
            
    public void Serialize(INetworkWriter writer)
    {
        writer.WriteString(Sender);
        writer.WriteString(Message);
    }
            
    public void Deserialize(INetworkReader reader)
    {
        Sender = reader.ReadString();
        Message = reader.ReadString();
    }
}