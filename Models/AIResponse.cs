namespace DJAI.Models
{
    public class AIResponse
    {
        // Changed from required members to properties with default values
        public string Text { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
        public string ConversationId { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public bool ReachedRateLimit { get; set; }
        public bool ReachedTokenLimit { get; set; }
    }
}