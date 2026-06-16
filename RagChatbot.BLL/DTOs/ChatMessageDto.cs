using System;

namespace RagChatbot.BLL.DTOs
{
    public class ChatMessageDto
    {
        public string Sender { get; set; } = "user";   // "user" | "assistant"
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
