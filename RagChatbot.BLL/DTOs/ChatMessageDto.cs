using System;

namespace RagChatbot.BLL.DTOs
{
    public class ChatMessageDto
    {
        public string Sender { get; set; } = "user";   // "user" | "assistant"
        public string Content { get; set; } = "";
        public IReadOnlyList<SourceCitationDto> Sources { get; set; } = Array.Empty<SourceCitationDto>();
        public DateTime CreatedAt { get; set; }
    }
}
