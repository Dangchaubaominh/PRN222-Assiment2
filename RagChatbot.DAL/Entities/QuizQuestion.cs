using System.ComponentModel.DataAnnotations;

namespace RagChatbot.DAL.Entities
{
    public class QuizQuestion
    {
        [Key]
        public int Id { get; set; }

        public int QuizId { get; set; }
        public Quiz Quiz { get; set; } = default!;

        [Required]
        public string Content { get; set; } = "";

        [Required]
        public string OptionA { get; set; } = "";

        [Required]
        public string OptionB { get; set; } = "";

        [Required]
        public string OptionC { get; set; } = "";

        [Required]
        public string OptionD { get; set; } = "";

        [Required]
        [MaxLength(1)]
        public string CorrectOption { get; set; } = "A";

        public string Explanation { get; set; } = "";
    }
}
