using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace GestaoChamados.Hubs
{
    public class ChatHub : Hub
    {
        // Voltando para a versão mais simples do SendMessage com 3 parâmetros
        public async Task SendMessage(string ticketId, string user, string message)
        {
            await Clients.Group(ticketId).SendAsync("ReceiveMessage", user, message);
        }

        // Método para um cliente entrar na "sala de chat" de um ticket específico
        public async Task JoinTicketChat(string ticketId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ticketId);
        }
    }
}