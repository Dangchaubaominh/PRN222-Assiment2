using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Data;
using RagChatbot.DAL.Entities;

namespace RagChatbot.BLL.Services.Implements
{
    public class QuizService : IQuizService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAIService _aiService;

        public QuizService(ApplicationDbContext context, IAIService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        public IEnumerable<QuizDto> GetBySubject(Guid subjectId)
            => _context.Quizzes
                .Include(q => q.Questions)
                .AsNoTracking()
                .Where(q => q.SubjectId == subjectId)
                .OrderByDescending(q => q.CreatedAt)
                .Select(ToDto)
                .ToList();

        public QuizDto? GetById(int quizId)
        {
            var quiz = _context.Quizzes
                .Include(q => q.Questions)
                .AsNoTracking()
                .FirstOrDefault(q => q.Id == quizId);

            return quiz == null ? null : ToDto(quiz);
        }

        public async Task<QuizDto?> GenerateAsync(Guid subjectId, Guid? documentId, int questionCount = 5)
        {
            var query = _context.DocumentChunks
                .Include(c => c.Document)
                .AsNoTracking()
                .Where(c => c.Document.SubjectId == subjectId);

            if (documentId.HasValue)
                query = query.Where(c => c.DocumentId == documentId.Value);

            var chunks = query.OrderBy(c => c.DocumentId)
                              .ThenBy(c => c.ChunkIndex)
                              .Take(10)
                              .ToList();

            if (chunks.Count == 0)
                return null;

            string prompt = $$"""
            Tạo {{questionCount}} câu hỏi trắc nghiệm bằng tiếng Việt có dấu từ tài liệu sau.
            Chỉ trả về JSON hợp lệ, không thêm markdown.
            Định dạng:
            [
              {
                "questionText": "Nội dung câu hỏi?",
                "options": ["A. ...", "B. ...", "C. ...", "D. ..."],
                "correctAnswer": "A",
                "explanation": "Giải thích ngắn gọn dựa trên tài liệu."
              }
            ]

            Tài liệu:
            {{string.Join("\n\n---\n\n", chunks.Select(c => c.TextContent))}}
            """;

            string aiText = await CollectAsync(prompt);
            var questions = ParseQuestions(aiText);
            if (questions.Count == 0)
                questions = BuildFallbackQuestions(chunks, questionCount);

            string title = documentId.HasValue
                ? $"Quiz từ {chunks.First().Document.FileName}"
                : $"Quiz môn học {DateTime.Now:dd/MM/yyyy HH:mm}";

            var quiz = new Quiz
            {
                SubjectId = subjectId,
                DocumentId = documentId,
                Title = title,
                CreatedAt = DateTime.UtcNow,
                Questions = questions.Take(questionCount).Select(q => new QuizQuestion
                {
                    QuestionText = q.QuestionText,
                    OptionsJson = JsonSerializer.Serialize(q.Options),
                    CorrectAnswer = NormalizeAnswer(q.CorrectAnswer),
                    Explanation = q.Explanation
                }).ToList()
            };

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            return ToDto(quiz);
        }

        public QuizAttemptDto? Submit(int userId, int quizId, IDictionary<int, string> answers)
        {
            var quiz = _context.Quizzes.Include(q => q.Questions).FirstOrDefault(q => q.Id == quizId);
            if (quiz == null)
                return null;

            int score = quiz.Questions.Count(q =>
                answers.TryGetValue(q.Id, out var answer) &&
                string.Equals(NormalizeAnswer(answer), NormalizeAnswer(q.CorrectAnswer), StringComparison.OrdinalIgnoreCase));

            var attempt = new QuizAttempt
            {
                QuizId = quizId,
                UserId = userId,
                Score = score,
                TotalQuestions = quiz.Questions.Count,
                AnswersJson = JsonSerializer.Serialize(answers),
                TakenAt = DateTime.UtcNow
            };

            _context.QuizAttempts.Add(attempt);
            _context.SaveChanges();

            return ToAttemptDto(attempt, CountAttempts(userId, quizId));
        }

        public QuizAttemptDto? GetLatestAttempt(int userId, int quizId)
        {
            var attempt = _context.QuizAttempts
                .AsNoTracking()
                .Where(a => a.UserId == userId && a.QuizId == quizId)
                .OrderByDescending(a => a.TakenAt)
                .FirstOrDefault();

            return attempt == null ? null : ToAttemptDto(attempt, CountAttempts(userId, quizId));
        }

        public int CountAttempts(int userId, int quizId)
            => _context.QuizAttempts.Count(a => a.UserId == userId && a.QuizId == quizId);

        private async Task<string> CollectAsync(string prompt)
        {
            var sb = new StringBuilder();
            await foreach (var part in _aiService.GenerateChatResponseStreamAsync(prompt))
                sb.Append(part);
            return sb.ToString();
        }

        private static List<GeneratedQuestion> ParseQuestions(string text)
        {
            string json = text.Trim();
            var match = Regex.Match(json, @"\[[\s\S]*\]");
            if (match.Success)
                json = match.Value;

            try
            {
                var questions = JsonSerializer.Deserialize<List<GeneratedQuestion>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return questions?
                    .Where(q => !string.IsNullOrWhiteSpace(q.QuestionText) &&
                                q.Options.Count >= 2 &&
                                !string.IsNullOrWhiteSpace(q.CorrectAnswer))
                    .ToList() ?? new List<GeneratedQuestion>();
            }
            catch
            {
                return new List<GeneratedQuestion>();
            }
        }

        private static List<GeneratedQuestion> BuildFallbackQuestions(IReadOnlyList<DocumentChunk> chunks, int questionCount)
        {
            return chunks.Take(questionCount).Select((chunk, index) => new GeneratedQuestion
            {
                QuestionText = $"Ý chính của đoạn tài liệu số {chunk.ChunkIndex ?? index + 1} là gì?",
                Options = new List<string>
                {
                    "A. Nội dung được trình bày trong đoạn tài liệu liên quan",
                    "B. Một nội dung không xuất hiện trong tài liệu",
                    "C. Một kết luận không có căn cứ",
                    "D. Một thông tin ngoài phạm vi môn học"
                },
                CorrectAnswer = "A",
                Explanation = BuildSnippet(chunk.TextContent)
            }).ToList();
        }

        private static QuizDto ToDto(Quiz quiz)
            => new()
            {
                Id = quiz.Id,
                SubjectId = quiz.SubjectId,
                DocumentId = quiz.DocumentId,
                Title = quiz.Title,
                CreatedAt = quiz.CreatedAt,
                Questions = quiz.Questions.Select(q => new QuizQuestionDto
                {
                    Id = q.Id,
                    QuestionText = q.QuestionText,
                    Options = ParseOptions(q.OptionsJson),
                    CorrectAnswer = q.CorrectAnswer,
                    Explanation = q.Explanation
                }).ToList()
            };

        private static IReadOnlyList<string> ParseOptions(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static QuizAttemptDto ToAttemptDto(QuizAttempt attempt, int attemptNumber)
            => new()
            {
                Id = attempt.Id,
                QuizId = attempt.QuizId,
                Score = attempt.Score,
                TotalQuestions = attempt.TotalQuestions,
                TakenAt = attempt.TakenAt,
                AttemptNumber = attemptNumber
            };

        private static string NormalizeAnswer(string? answer)
            => string.IsNullOrWhiteSpace(answer) ? "" : answer.Trim()[0].ToString().ToUpperInvariant();

        private static string BuildSnippet(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            string normalized = Regex.Replace(text, @"\s+", " ").Trim();
            return normalized.Length <= 180 ? normalized : normalized[..180].TrimEnd() + "...";
        }

        private sealed class GeneratedQuestion
        {
            public string QuestionText { get; set; } = "";
            public List<string> Options { get; set; } = new();
            public string CorrectAnswer { get; set; } = "";
            public string Explanation { get; set; } = "";
        }
    }
}
