using System;
using System.ComponentModel.DataAnnotations;

namespace RagChatbot.DAL.Entities
{
    public class QuizAttempt
    {
        [Key]
        public int Id { get; set; }
        public int QuizId { get; set; }
        public Quiz Quiz { get; set; } = default!;
        public int UserId { get; set; }
        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public string AnswersJson { get; set; } = "{}";
        public DateTime TakenAt { get; set; }
    }
}
