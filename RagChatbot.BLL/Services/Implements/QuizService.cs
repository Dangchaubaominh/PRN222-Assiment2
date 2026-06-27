using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Data;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;

namespace RagChatbot.BLL.Services.Implements
{
    public class QuizService : IQuizService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAIService _aiService;
        private readonly IDocumentChunkRepository _chunkRepository;

        public QuizService(ApplicationDbContext context, IAIService aiService, IDocumentChunkRepository chunkRepository)
        {
            _context = context;
            _aiService = aiService;
            _chunkRepository = chunkRepository;
        }

        public async Task<QuizDto?> GenerateQuizAsync(Guid documentId, int numberOfQuestions, int userId)
        {
            var chunks = _chunkRepository.GetByDocumentId(documentId).Take(50).ToList();
            if (!chunks.Any())
                throw new Exception("Tài liệu chưa được xử lý hoặc không có nội dung.");

            string documentContent = string.Join("\n\n", chunks.Select(c => c.TextContent));
            if (documentContent.Length > 30000)
                documentContent = documentContent[..30000];

            string prompt = $$"""
            Dựa vào nội dung tài liệu sau, hãy tạo một bài trắc nghiệm gồm {{numberOfQuestions}} câu hỏi bằng tiếng Việt có dấu.
            Chỉ trả về mảng JSON hợp lệ, không kèm markdown.
            Định dạng JSON:
            [
              {
                "Question": "Nội dung câu hỏi?",
                "OptionA": "Đáp án A",
                "OptionB": "Đáp án B",
                "OptionC": "Đáp án C",
                "OptionD": "Đáp án D",
                "CorrectOption": "A",
                "Explanation": "Giải thích ngắn gọn tại sao đáp án đúng."
              }
            ]
            CorrectOption chỉ nhận một trong bốn giá trị: A, B, C, D.

            NỘI DUNG TÀI LIỆU:
            {{documentContent}}
            """;

            string responseJson = await _aiService.GenerateContentAsync(prompt);
            if (string.IsNullOrWhiteSpace(responseJson))
                throw new Exception("Không nhận được phản hồi từ AI.");

            var parsedQuestions = ParseAiQuestions(responseJson);
            if (parsedQuestions.Count == 0)
                parsedQuestions = BuildFallbackQuestions(chunks, numberOfQuestions);

            var document = await _context.Documents.FindAsync(documentId);
            var quiz = new Quiz
            {
                DocumentId = documentId,
                Title = $"Bài tập: {document?.FileName ?? "Tài liệu"}",
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow,
                Questions = parsedQuestions.Take(numberOfQuestions).Select(q => new QuizQuestion
                {
                    Content = q.QuestionText,
                    OptionA = CleanOption(q.OptionA),
                    OptionB = CleanOption(q.OptionB),
                    OptionC = CleanOption(q.OptionC),
                    OptionD = CleanOption(q.OptionD),
                    CorrectOption = NormalizeAnswer(q.CorrectOption),
                    Explanation = q.Explanation ?? ""
                }).ToList()
            };

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            return await GetQuizByIdAsync(quiz.Id);
        }

        public async Task<QuizDto?> GetQuizByIdAsync(int quizId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == quizId);

            return quiz == null ? null : ToDto(quiz);
        }

        public async Task<List<QuizDto>> GetQuizzesByDocumentAsync(Guid documentId)
            => await _context.Quizzes
                .Include(q => q.Questions)
                .AsNoTracking()
                .Where(q => q.DocumentId == documentId)
                .OrderByDescending(q => q.CreatedAt)
                .Select(q => ToDto(q))
                .ToListAsync();

        public async Task<List<QuizDto>> GetQuizzesBySubjectAsync(Guid subjectId)
            => await _context.Quizzes
                .Include(q => q.Document)
                .Include(q => q.Questions)
                .AsNoTracking()
                .Where(q => q.Document.SubjectId == subjectId)
                .OrderByDescending(q => q.CreatedAt)
                .Select(q => ToDto(q))
                .ToListAsync();

        public async Task<QuizResultDto> SubmitQuizAsync(int quizId, int userId, Dictionary<int, string> answers)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null)
                throw new Exception("Không tìm thấy bài quiz.");

            int score = quiz.Questions.Count(q =>
                answers.TryGetValue(q.Id, out string? userAnswer) &&
                NormalizeAnswer(userAnswer) == NormalizeAnswer(q.CorrectOption));

            var result = new QuizResult
            {
                QuizId = quizId,
                UserId = userId,
                Score = score,
                TotalQuestions = quiz.Questions.Count,
                CompletedAt = DateTime.UtcNow
            };

            _context.QuizResults.Add(result);
            await _context.SaveChangesAsync();

            return ToResultDto(result);
        }

        public async Task<List<QuizResultDto>> GetUserQuizResultsAsync(int userId, Guid? documentId = null)
        {
            var query = _context.QuizResults.Include(r => r.Quiz).AsQueryable();

            if (documentId.HasValue)
                query = query.Where(r => r.Quiz.DocumentId == documentId.Value);

            var results = await query
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CompletedAt)
                .ToListAsync();

            return results.Select(ToResultDto).ToList();
        }

        public async Task<QuizResultDto?> GetLatestResultAsync(int userId, int quizId)
        {
            var result = await _context.QuizResults
                .AsNoTracking()
                .Where(r => r.UserId == userId && r.QuizId == quizId)
                .OrderByDescending(r => r.CompletedAt)
                .FirstOrDefaultAsync();

            return result == null ? null : ToResultDto(result);
        }

        public Task<int> CountAttemptsAsync(int userId, int quizId)
            => _context.QuizResults.CountAsync(r => r.UserId == userId && r.QuizId == quizId);

        private static QuizDto ToDto(Quiz quiz)
            => new()
            {
                Id = quiz.Id,
                DocumentId = quiz.DocumentId,
                Title = quiz.Title,
                CreatedAt = quiz.CreatedAt,
                CreatedById = quiz.CreatedById,
                Questions = quiz.Questions.Select(q => new QuizQuestionDto
                {
                    Id = q.Id,
                    QuizId = q.QuizId,
                    Content = q.Content,
                    Question = q.Content,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD,
                    CorrectOption = q.CorrectOption,
                    Explanation = q.Explanation
                }).ToList()
            };

        private static QuizResultDto ToResultDto(QuizResult result)
            => new()
            {
                Id = result.Id,
                QuizId = result.QuizId,
                UserId = result.UserId,
                Score = result.Score,
                TotalQuestions = result.TotalQuestions,
                CompletedAt = result.CompletedAt
            };

        private static List<GeneratedQuestion> ParseAiQuestions(string text)
        {
            string json = text.Trim();
            json = Regex.Replace(json, "^```json", "", RegexOptions.IgnoreCase).Trim();
            json = Regex.Replace(json, "^```", "").Trim();
            json = Regex.Replace(json, "```$", "").Trim();

            var match = Regex.Match(json, @"\[[\s\S]*\]");
            if (match.Success)
                json = match.Value;

            try
            {
                return JsonSerializer.Deserialize<List<GeneratedQuestion>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })?.Where(q => !string.IsNullOrWhiteSpace(q.QuestionText) ||
                              !string.IsNullOrWhiteSpace(q.Question) ||
                              !string.IsNullOrWhiteSpace(q.Content))
                    .Select(q =>
                    {
                        q.QuestionText = q.QuestionText ?? q.Question ?? q.Content ?? "";
                        return q;
                    })
                    .ToList() ?? new List<GeneratedQuestion>();
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi đọc JSON từ AI: " + ex.Message);
            }
        }

        private static List<GeneratedQuestion> BuildFallbackQuestions(IReadOnlyList<DocumentChunk> chunks, int questionCount)
        {
            return chunks.Take(questionCount).Select((chunk, index) => new GeneratedQuestion
            {
                QuestionText = $"Ý chính của đoạn tài liệu số {chunk.ChunkIndex ?? index + 1} là gì?",
                OptionA = "Nội dung được trình bày trong đoạn tài liệu liên quan",
                OptionB = "Một nội dung không xuất hiện trong tài liệu",
                OptionC = "Một kết luận không có căn cứ",
                OptionD = "Một thông tin ngoài phạm vi môn học",
                CorrectOption = "A",
                Explanation = BuildSnippet(chunk.TextContent)
            }).ToList();
        }

        private static string NormalizeAnswer(string? answer)
            => string.IsNullOrWhiteSpace(answer) ? "A" : answer.Trim()[0].ToString().ToUpperInvariant();

        private static string CleanOption(string? option)
            => Regex.Replace(option ?? "", @"^[A-Da-d]\.\s*", "").Trim();

        private static string BuildSnippet(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            string normalized = Regex.Replace(text, @"\s+", " ").Trim();
            return normalized.Length <= 180 ? normalized : normalized[..180].TrimEnd() + "...";
        }

        private sealed class GeneratedQuestion
        {
            public string? QuestionText { get; set; }
            public string? Question { get; set; }
            public string? Content { get; set; }
            public string? OptionA { get; set; }
            public string? OptionB { get; set; }
            public string? OptionC { get; set; }
            public string? OptionD { get; set; }
            public string? CorrectOption { get; set; }
            public string? Explanation { get; set; }
        }
    }
}
