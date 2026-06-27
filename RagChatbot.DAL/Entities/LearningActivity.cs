using System;
using System.ComponentModel.DataAnnotations;

namespace RagChatbot.DAL.Entities
{
    public class LearningActivity
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public Guid SubjectId { get; set; }
        public Guid? DocumentId { get; set; }
        [MaxLength(40)]
        public string ActivityType { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
