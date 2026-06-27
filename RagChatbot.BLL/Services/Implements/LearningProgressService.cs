using System;
using System.Linq;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Data;
using RagChatbot.DAL.Entities;

namespace RagChatbot.BLL.Services.Implements
{
    public class LearningProgressService : ILearningProgressService
    {
        private readonly ApplicationDbContext _context;

        public LearningProgressService(ApplicationDbContext context)
        {
            _context = context;
        }

        public void RecordDocumentView(int userId, Guid subjectId, Guid documentId)
            => Add(userId, subjectId, documentId, "ViewDocument");

        public void RecordChatQuestion(int userId, Guid subjectId)
            => Add(userId, subjectId, null, "AskQuestion");

        public void RecordQuizAttempt(int userId, Guid subjectId, int quizId)
            => Add(userId, subjectId, null, "QuizAttempt");

        public LearningDashboardDto GetDashboard(int userId)
        {
            var activities = _context.LearningActivities.Where(a => a.UserId == userId).ToList();
            var attempts = _context.QuizAttempts.Where(a => a.UserId == userId).ToList();
            var subjects = _context.Subjects.ToList();

            return new LearningDashboardDto
            {
                AskedQuestions = activities.Count(a => a.ActivityType == "AskQuestion"),
                ViewedDocuments = activities.Where(a => a.ActivityType == "ViewDocument" && a.DocumentId.HasValue)
                                            .Select(a => a.DocumentId!.Value)
                                            .Distinct()
                                            .Count(),
                QuizAttempts = attempts.Count,
                AverageQuizScore = attempts.Count == 0 ? 0 : attempts.Average(a => Percent(a.Score, a.TotalQuestions)),
                Subjects = subjects.Select(subject =>
                {
                    var subjectActivities = activities.Where(a => a.SubjectId == subject.Id).ToList();
                    var quizIds = _context.Quizzes.Where(q => q.SubjectId == subject.Id).Select(q => q.Id).ToList();
                    var subjectAttempts = attempts.Where(a => quizIds.Contains(a.QuizId)).ToList();

                    return new SubjectLearningStatDto
                    {
                        SubjectCode = subject.Code,
                        SubjectName = subject.Name,
                        AskedQuestions = subjectActivities.Count(a => a.ActivityType == "AskQuestion"),
                        ViewedDocuments = subjectActivities.Where(a => a.ActivityType == "ViewDocument" && a.DocumentId.HasValue)
                                                           .Select(a => a.DocumentId!.Value)
                                                           .Distinct()
                                                           .Count(),
                        QuizAttempts = subjectAttempts.Count,
                        AverageQuizScore = subjectAttempts.Count == 0 ? 0 : subjectAttempts.Average(a => Percent(a.Score, a.TotalQuestions))
                    };
                }).Where(s => s.AskedQuestions > 0 || s.ViewedDocuments > 0 || s.QuizAttempts > 0).ToList()
            };
        }

        private void Add(int userId, Guid subjectId, Guid? documentId, string type)
        {
            _context.LearningActivities.Add(new LearningActivity
            {
                UserId = userId,
                SubjectId = subjectId,
                DocumentId = documentId,
                ActivityType = type,
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();
        }

        private static double Percent(int score, int total)
            => total <= 0 ? 0 : Math.Round(score * 100.0 / total, 1);
    }
}
