using GestaoChamados.Models;
using GestaoChamados.Data;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace GestaoChamados.Hubs
{
    /// <summary>
    /// Hub SignalR para comunicação em tempo real entre clientes e técnicos
    /// Gerencia salas de chat por ticket, notificações e grupos de técnicos
    /// </summary>
    public class SupportHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public SupportHub(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Envia uma mensagem de chat para um ticket específico
        /// A mensagem é salva no banco e transmitida para todos os participantes do grupo
        /// </summary>
        /// <param name="ticketId">ID do ticket/chamado</param>
        /// <param name="userName">Nome do remetente</param>
        /// <param name="userEmail">Email do remetente</param>
        /// <param name="message">Conteúdo da mensagem</param>
        public async Task SendMessage(string ticketId, string userName, string userEmail, string message)
        {
            try
            {
                Console.WriteLine($"[SupportHub] Mensagem recebida - Ticket: {ticketId}, Usuario: {userName}, Email: {userEmail}, Mensagem: {message}");
                
                // 1. Salva a mensagem no banco de dados
                var chatMessage = new ChatMessageModel
                {
                    TicketId = int.Parse(ticketId),
                    SenderName = userName,
                    SenderEmail = userEmail,
                    MessageText = message,
                    Timestamp = DateTime.Now
                };
                _context.ChatMessages.Add(chatMessage);
                await _context.SaveChangesAsync();
                Console.WriteLine($"[SupportHub] Mensagem salva no banco de dados");

                // 2. Envia a mensagem para todos no grupo do ticket (incluindo o remetente para confirmação)
                await Clients.Group($"ticket-{ticketId}").SendAsync("ReceiveMessage", userName, userEmail, message);
                Console.WriteLine($"[SupportHub] Mensagem enviada para o grupo ticket-{ticketId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SupportHub] Erro ao processar mensagem: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Adiciona a conexão atual ao grupo de técnicos
        /// Técnicos neste grupo recebem notificações de novos chamados na fila
        /// </summary>
        public async Task JoinTechnicianGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Tecnicos");
            Console.WriteLine($"[SupportHub] Usuário {Context.ConnectionId} entrou no grupo Tecnicos");
        }

        /// <summary>
        /// Adiciona a conexão atual ao grupo de um ticket específico
        /// Permite que o cliente/técnico receba mensagens do chat daquele ticket
        /// </summary>
        /// <param name="ticketId">ID do ticket para entrar</param>
        public async Task JoinTicketGroup(string ticketId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"ticket-{ticketId}");
            Console.WriteLine($"[SupportHub] Usuário {Context.ConnectionId} entrou no grupo ticket-{ticketId}");
        }

        /// <summary>
        /// Envia uma solicitação de pesquisa de satisfação para o cliente de um ticket
        /// </summary>
        /// <param name="ticketId">ID do ticket</param>
        /// <param name="userEmail">Email do usuário que deve responder a pesquisa</param>
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