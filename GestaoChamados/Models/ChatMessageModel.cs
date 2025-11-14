namespace GestaoChamados.Models
{
    public class ChatMessageModel
    {
        public int TicketId { get; set; }
        public string SenderEmail { get; set; }
        public string SenderName { get; set; }
        public string MessageText { get; set; }
        public DateTime Timestamp { get; set; }
    }
}