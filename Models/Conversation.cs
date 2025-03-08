using System;
using System.Collections.Generic;

namespace DJAI.Models
{
    public class Conversation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = $"Nieuw gesprek {DateTime.Now:g}";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastUpdatedAt { get; set; } = DateTime.Now;
        public List<ChatMessage> Messages { get; set; } = [];
        public string SelectedProvider { get; set; } = "Anthropic"; // Default provider
    }
}