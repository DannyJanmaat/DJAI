// Create a new file: Models/Message.cs
using System;

namespace DJAI.Models
{
    public class Message
    {
        public string Content { get; set; } = string.Empty;
        public string Role { get; set; } = "user";
        public bool IsUser { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Factory methods for creating different types of messages
        public static Message FromUser(string content)
        {
            return new Message
            {
                Content = content,
                Role = "user",
                IsUser = true,
                Timestamp = DateTime.Now
            };
        }

        public static Message FromAssistant(string content)
        {
            return new Message
            {
                Content = content,
                Role = "assistant",
                IsUser = false,
                Timestamp = DateTime.Now
            };
        }

        public static Message FromSystem(string content)
        {
            return new Message
            {
                Content = content,
                Role = "system",
                IsUser = false,
                Timestamp = DateTime.Now
            };
        }
    }
}
