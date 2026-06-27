using System.ComponentModel.DataAnnotations;

namespace RagChatbot.DAL.Entities
{
    public class QuizQuestion
    {
        [Key]
        public int Id { get; set; }
        public int QuizId { get; set; }
        public Quiz Quiz { get; set; } = default!;
        public string QuestionText { get; set; } = "";
        public string OptionsJson { get; set; } = "[]";
        [MaxLength(10)]
        public string CorrectAnswer { get; set; } = "";
        public string Explanation { get; set; } = "";
    }
}
