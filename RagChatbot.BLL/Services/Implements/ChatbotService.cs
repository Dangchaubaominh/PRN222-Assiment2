using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RagChatbot.BLL.Services.Implements
{
    public class ChatbotService : IChatbotService
    {
        private readonly IDocumentChunkRepository _chunkRepository;
        private readonly IAIService _aiService;

        public ChatbotService(IDocumentChunkRepository chunkRepository, IAIService aiService)
        {
            _chunkRepository = chunkRepository;
            _aiService = aiService;
        }

        public async Task<ChatResult> AskAsync(Guid subjectId, string userMessage, CancellationToken cancellationToken = default)
        {
            // Giai đoạn chuẩn bị: nhúng câu hỏi + tìm chunk gần nhất (kèm nguồn)
            string? prepError = null;
            List<DocumentChunk> chunks = new();
            try
            {
                float[] questionVector = await _aiService.GenerateEmbeddingAsync(userMessage);
                chunks = (await _chunkRepository.SearchSimilarChunksAsync(subjectId, questionVector, topK: 3)).ToList();
            }
            catch (Exception ex)
            {
                prepError = $"Hệ thống gặp lỗi nội bộ: {ex.Message}";
            }

            if (prepError != null)
                return new ChatResult { Answer = Single(prepError) };

            if (!chunks.Any())
                return new ChatResult { Answer = Single("Môn học này hiện chưa có tài liệu nào. Vui lòng upload tài liệu trước khi hỏi.") };

            // Ghép context + danh sách nguồn (tên tài liệu, không trùng)
            string contextText = string.Join("\n\n---\n\n", chunks.Select(c => c.TextContent));
            var sources = chunks
                .Select(c => c.Document?.FileName)
                .Where(f => !string.IsNullOrEmpty(f))
                .Distinct()
                .Cast<string>()
                .ToList();

            string finalPrompt = $"TÀI LIỆU CUNG CẤP:\n{contextText}\n\nCÂU HỎI CỦA NGƯỜI DÙNG:\n{userMessage}";

            return new ChatResult
            {
                Sources = sources,
                Answer = _aiService.GenerateChatResponseStreamAsync(finalPrompt, cancellationToken)
            };
        }

        // Bọc một thông báo đơn thành luồng 1 phần tử
        private static async IAsyncEnumerable<string> Single(string message)
        {
            yield return message;
            await Task.CompletedTask;
        }
    }
}
