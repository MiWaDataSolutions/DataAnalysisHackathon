using Microsoft.AspNetCore.SignalR;

namespace DataAnalystBackend.Hubs
{
    public class DataSessionHub : Hub
    {
        public async Task Join(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        public async Task SendDataSessionName(string userId, string dataSessionName)
        {
            await Clients.Group(userId).SendAsync(dataSessionName);
        }
    }
}
