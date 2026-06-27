using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RagChatbot.DAL.Entities
{
    public class ChatSession
    {
        [Key]
        public int Id { get; set; }

        public Guid SubjectId { get; set; }
        public int UserId { get; set; }

        [Required]
        [MaxLength(120)]
        public string Title { get; set; } = "";

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
