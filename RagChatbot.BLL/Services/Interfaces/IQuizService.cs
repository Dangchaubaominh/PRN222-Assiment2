using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RagChatbot.BLL.DTOs;

namespace RagChatbot.BLL.Services.Interfaces
{
    public interface IQuizService
    {
        IEnumerable<QuizDto> GetBySubject(Guid subjectId);
        QuizDto? GetById(int quizId);
        Task<QuizDto?> GenerateAsync(Guid subjectId, Guid? documentId, int questionCount = 5);
        QuizAttemptDto? Submit(int userId, int quizId, IDictionary<int, string> answers);
        QuizAttemptDto? GetLatestAttempt(int userId, int quizId);
        int CountAttempts(int userId, int quizId);
    }
}
