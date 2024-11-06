using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace QLBanDoAnNhanh.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string userLogin, string message)
        {
            // Gửi tin nhắn từ userLogin
            await Clients.All.SendAsync("ReceiveMessage", userLogin, message);
        }
    }
}
