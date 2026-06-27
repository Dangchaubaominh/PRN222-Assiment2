using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RagChatbot.DAL.Entities
{
    public class Quiz
    {
        [Key]
        public int Id { get; set; }
        public Guid SubjectId { get; set; }
        public Guid? DocumentId { get; set; }
        [MaxLength(200)]
        public string Title { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
        public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
    }
}
