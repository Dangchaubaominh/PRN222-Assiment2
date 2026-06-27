using System;
using System.Collections.Generic;
using System.Linq;

namespace RagChatbot.BLL.DTOs
{
    public class QuizDto
    {
        public int Id { get; set; }
        public Guid DocumentId { get; set; }
        public string Title { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public int CreatedById { get; set; }
        public List<QuizQuestionDto> Questions { get; set; } = new();
    }

    public partial class QuizQuestionDto
    {
        public string QuestionText
        {
            get => Content ?? Question ?? "";
            set => Content = value;
        }

        public IReadOnlyList<string> Options
            => new[] { OptionA, OptionB, OptionC, OptionD }
                .Where(option => !string.IsNullOrWhiteSpace(option))
                .Select((option, index) => $"{(char)('A' + index)}. {option}")
                .ToList();

        public string CorrectAnswer
        {
            get => CorrectOption ?? "";
            set => CorrectOption = value;
        }
    }
}
