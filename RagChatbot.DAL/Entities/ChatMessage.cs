using System;
using System.ComponentModel.DataAnnotations;

namespace RagChatbot.DAL.Entities
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        public Guid SubjectId { get; set; }
        public int UserId { get; set; }
        public int? SessionId { get; set; }
        public ChatSession? Session { get; set; }

        [MaxLength(20)]
        public string Sender { get; set; } = "user";

        [Required]
        public string Content { get; set; } = "";

        public string? SourcesJson { get; set; }

        public FeedbackType? Feedback { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
