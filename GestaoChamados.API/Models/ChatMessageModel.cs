namespace GestaoChamados.Models
{
    /// <summary>
    /// Modelo de mensagem de chat entre cliente e técnico
    /// Armazena o histórico de conversas associadas a um chamado específico
    /// </summary>
    public class ChatMessageModel
    {
        /// <summary>ID do chamado/ticket ao qual a mensagem pertence</summary>
        public int TicketId { get; set; }
        
        /// <summary>Email do remetente da mensagem</summary>
        public string SenderEmail { get; set; }
        
        /// <summary>Nome do remetente da mensagem</summary>
        public string SenderName { get; set; }
        
        /// <summary>Conteúdo de texto da mensagem</summary>
        public string MessageText { get; set; }
        
        /// <summary>Data e hora do envio da mensagem</summary>
        public DateTime Timestamp { get; set; }
    }
}