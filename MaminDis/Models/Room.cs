using System.Net.WebSockets;

namespace MaminDis.Models
{
    public class Room
    {
        public string Title { get; set; }
        public List<User>? Members { get; set; }
        public int Id { get; set; }
        public List<WebSocket> UsersListReceivers { get; set; }

        public Room()
        {
            UsersListReceivers = new List<WebSocket>();
        }
    }
}
