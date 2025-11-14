using GestaoChamados.Models;
using GestaoChamados.Services;

namespace GestaoChamados.Services
{
    public interface IChatbotService
    {
        Task<ChatbotResponse> ProcessMessageAsync(string userMessage, List<ChatbotMessage> conversationHistory);
        Task<bool> ShouldCreateTicketAsync(List<ChatbotMessage> conversationHistory);
        Task<TicketSuggestion> AnalyzeTicketRequirementAsync(List<ChatbotMessage> conversationHistory);
        Task<bool> AnalyzeProblemResolutionAsync(List<ChatbotMessage> conversationHistory);
    }

    public class ChatbotResponse
    {
        public string Message { get; set; }
        public bool IsITRelated { get; set; }
        public bool SuggestTicketCreation { get; set; }
        public string Priority { get; set; } // "Baixa", "M�dia", "Alta", "Cr�tica"
        public string Category { get; set; }
        public bool EndConversation { get; set; }
    }

    public class ChatbotMessage
    {
        public string Sender { get; set; } // "user" ou "bot"
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class TicketSuggestion
    {
        public bool ShouldCreateTicket { get; set; }
        public string SuggestedTitle { get; set; }
        public string SuggestedDescription { get; set; }
        public string Priority { get; set; }
        public string Category { get; set; }
        public string Reason { get; set; }
    }
}