using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Data;
using RagChatbot.DAL.Entities;

namespace RagChatbot.BLL.Services.Implements
{
    public class DocumentSummaryService : IDocumentSummaryService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAIService _aiService;

        public DocumentSummaryService(ApplicationDbContext context, IAIService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        public DocumentSummaryDto? GetByDocument(Guid documentId)
        {
            var summary = _context.DocumentSummaries.AsNoTracking().FirstOrDefault(s => s.DocumentId == documentId);
            return summary == null ? null : ToDto(summary);
        }

        public async Task<DocumentSummaryDto?> GenerateAsync(Guid documentId)
        {
            var document = _context.Documents.AsNoTracking().FirstOrDefault(d => d.Id == documentId);
            if (document == null)
                return null;

            var chunks = _context.DocumentChunks
                .AsNoTracking()
                .Where(c => c.DocumentId == documentId)
                .OrderBy(c => c.ChunkIndex)
                .Take(8)
                .Select(c => c.TextContent)
                .ToList();

            if (chunks.Count == 0)
                return null;

            string prompt = $"""
            Hãy tạo bản tóm tắt học tập bằng tiếng Việt có dấu cho tài liệu "{document.FileName}".

            Trả lời đúng 4 mục sau:
            TÓM TẮT:
            Ý CHÍNH:
            MỤC TIÊU BÀI HỌC:
            THUẬT NGỮ QUAN TRỌNG:

            Nội dung tài liệu:
            {string.Join("\n\n---\n\n", chunks)}
            """;

            string text = await CollectAsync(prompt);
            var now = DateTime.UtcNow;
            var entity = _context.DocumentSummaries.FirstOrDefault(s => s.DocumentId == documentId);

            if (entity == null)
            {
                entity = new DocumentSummary
                {
                    DocumentId = documentId,
                    CreatedAt = now
                };
                _context.DocumentSummaries.Add(entity);
            }

            entity.Summary = ExtractSection(text, "TÓM TẮT:", "Ý CHÍNH:");
            entity.KeyPoints = ExtractSection(text, "Ý CHÍNH:", "MỤC TIÊU BÀI HỌC:");
            entity.LearningObjectives = ExtractSection(text, "MỤC TIÊU BÀI HỌC:", "THUẬT NGỮ QUAN TRỌNG:");
            entity.ImportantTerms = ExtractSection(text, "THUẬT NGỮ QUAN TRỌNG:", null);
            entity.UpdatedAt = now;

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        private async Task<string> CollectAsync(string prompt)
        {
            var sb = new StringBuilder();
            await foreach (var part in _aiService.GenerateChatResponseStreamAsync(prompt))
                sb.Append(part);
            return sb.ToString();
        }

        private static string ExtractSection(string text, string start, string? end)
        {
            int startIndex = text.IndexOf(start, StringComparison.OrdinalIgnoreCase);
            if (startIndex < 0)
                return text.Trim();

            startIndex += start.Length;
            int endIndex = end == null ? -1 : text.IndexOf(end, startIndex, StringComparison.OrdinalIgnoreCase);
            string value = endIndex < 0 ? text[startIndex..] : text[startIndex..endIndex];
            return value.Trim();
        }

        private static DocumentSummaryDto ToDto(DocumentSummary summary)
            => new()
            {
                Id = summary.Id,
                DocumentId = summary.DocumentId,
                Summary = summary.Summary,
                KeyPoints = summary.KeyPoints,
                LearningObjectives = summary.LearningObjectives,
                ImportantTerms = summary.ImportantTerms,
                UpdatedAt = summary.UpdatedAt
            };
    }
}
