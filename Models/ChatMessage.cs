namespace DJAI.Models
{
    public enum MessageRole
    {
        User,
        Assistant,
        System
    }

    public class ChatMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public MessageRole Role { get; set; }
        public required string Content { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsComplete { get; set; } = true;
        public required string ConversationId { get; set; }
    }
}