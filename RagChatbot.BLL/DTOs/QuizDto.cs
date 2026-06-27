using System;
using System.Collections.Generic;

namespace RagChatbot.BLL.DTOs
{
    public class QuizDto
    {
        public int Id { get; set; }
        public Guid SubjectId { get; set; }
        public Guid? DocumentId { get; set; }
        public string Title { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public IReadOnlyList<QuizQuestionDto> Questions { get; set; } = new List<QuizQuestionDto>();
    }

    public class QuizQuestionDto
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = "";
        public IReadOnlyList<string> Options { get; set; } = new List<string>();
        public string CorrectAnswer { get; set; } = "";
        public string Explanation { get; set; } = "";
    }

    public class QuizAttemptDto
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public DateTime TakenAt { get; set; }
        public int AttemptNumber { get; set; }
    }
}
