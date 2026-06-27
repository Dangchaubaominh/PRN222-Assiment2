using System.Collections.Generic;

namespace RagChatbot.BLL.DTOs
{
    public class LearningDashboardDto
    {
        public int AskedQuestions { get; set; }
        public int ViewedDocuments { get; set; }
        public int QuizAttempts { get; set; }
        public double AverageQuizScore { get; set; }
        public IReadOnlyList<SubjectLearningStatDto> Subjects { get; set; } = new List<SubjectLearningStatDto>();
    }

    public class SubjectLearningStatDto
    {
        public string SubjectCode { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public int AskedQuestions { get; set; }
        public int ViewedDocuments { get; set; }
        public int QuizAttempts { get; set; }
        public double AverageQuizScore { get; set; }
    }
}
