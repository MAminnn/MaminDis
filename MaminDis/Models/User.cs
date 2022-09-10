using System.Net.WebSockets;

namespace MaminDis.Models
{
    public class User
    {
        public string Name { get; set; }
        public WebSocket VoiceChatSocket { get; set; }
    }
}
