using GestaoChamados.Controllers;
using GestaoChamados.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace GestaoChamados.Hubs
{
    public class SupportHub : Hub
    {
        public async Task SendMessage(string ticketId, string userName, string userEmail, string message)
        {
            try
            {
                Console.WriteLine($"[SupportHub] Mensagem recebida - Ticket: {ticketId}, Usuario: {userName}, Email: {userEmail}, Mensagem: {message}");
                
                // 1. Salva a mensagem na lista de histórico
                var chatMessage = new ChatMessageModel
                {
                    TicketId = int.Parse(ticketId),
                    SenderName = userName,
                    SenderEmail = userEmail,
                    MessageText = message,
                    Timestamp = DateTime.Now
                };
                ChamadoController._chatMessages.Add(chatMessage);
                Console.WriteLine($"[SupportHub] Mensagem salva no histórico. Total de mensagens: {ChamadoController._chatMessages.Count}");

                // 2. Envia a mensagem para todos no grupo do ticket
                await Clients.Group($"ticket-{ticketId}").SendAsync("ReceiveMessage", userName, userEmail, message);
                Console.WriteLine($"[SupportHub] Mensagem enviada para o grupo ticket-{ticketId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SupportHub] Erro ao processar mensagem: {ex.Message}");
                throw;
            }
        }

        public async Task JoinTechnicianGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Tecnicos");
            Console.WriteLine($"[SupportHub] Usuário {Context.ConnectionId} entrou no grupo Tecnicos");
        }

        public async Task JoinTicketGroup(string ticketId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"ticket-{ticketId}");
            Console.WriteLine($"[SupportHub] Usuário {Context.ConnectionId} entrou no grupo ticket-{ticketId}");
        }

        public async Task SendSatisfactionSurvey(string ticketId, string userEmail)
        {
            // Envia evento para o grupo do ticket (cliente)
            await Clients.Group($"ticket-{ticketId}").SendAsync("ShowSatisfactionSurvey", ticketId, userEmail);
        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"[SupportHub] Nova conexão: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"[SupportHub] Conexão desconectada: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}