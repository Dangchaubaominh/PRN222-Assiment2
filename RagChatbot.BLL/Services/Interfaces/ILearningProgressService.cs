using System;
using RagChatbot.BLL.DTOs;

namespace RagChatbot.BLL.Services.Interfaces
{
    public interface ILearningProgressService
    {
        void RecordDocumentView(int userId, Guid subjectId, Guid documentId);
        void RecordChatQuestion(int userId, Guid subjectId);
        void RecordQuizAttempt(int userId, Guid subjectId, int quizId);
        LearningDashboardDto GetDashboard(int userId);
    }
}
