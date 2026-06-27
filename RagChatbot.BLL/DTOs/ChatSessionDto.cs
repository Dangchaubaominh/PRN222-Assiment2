using System;

namespace RagChatbot.BLL.DTOs
{
    public class ChatSessionDto
    {
        public int Id { get; set; }
        public Guid SubjectId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int MessageCount { get; set; }
    }
}
