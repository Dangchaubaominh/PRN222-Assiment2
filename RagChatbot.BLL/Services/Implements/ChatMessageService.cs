using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;

namespace RagChatbot.BLL.Services.Implements
{
    public class ChatMessageService : IChatMessageService
    {
        private readonly IChatMessageRepository _repository;

        public ChatMessageService(IChatMessageRepository repository)
        {
            _repository = repository;
        }

        public void Save(int userId, Guid subjectId, int sessionId, string sender, string content, IReadOnlyList<SourceCitationDto>? sources = null)
        {
            if (string.IsNullOrWhiteSpace(content)) return;
            _repository.Add(new ChatMessage
            {
                UserId    = userId,
                SubjectId = subjectId,
                SessionId = sessionId,
                Sender    = sender,
                Content   = content,
                SourcesJson = sources is { Count: > 0 } ? JsonSerializer.Serialize(sources) : null,
                CreatedAt = DateTime.UtcNow
            });
        }

        public IEnumerable<ChatMessageDto> GetHistory(int userId, Guid subjectId, int sessionId, int take = 50)
            => _repository.GetHistory(userId, subjectId, sessionId, take).Select(m => new ChatMessageDto
            {
                Sender    = m.Sender,
                Content   = m.Content,
                Sources   = DeserializeSources(m.SourcesJson),
                CreatedAt = m.CreatedAt
            });

        public void ClearHistory(int userId, Guid subjectId, int sessionId)
            => _repository.DeleteHistory(userId, subjectId, sessionId);

        private static IReadOnlyList<SourceCitationDto> DeserializeSources(string? sourcesJson)
        {
            if (string.IsNullOrWhiteSpace(sourcesJson))
                return Array.Empty<SourceCitationDto>();

            try
            {
                var sources = JsonSerializer.Deserialize<List<SourceCitationDto>>(sourcesJson);
                if (sources == null)
                    return Array.Empty<SourceCitationDto>();

                return sources;
            }
            catch (JsonException)
            {
                return Array.Empty<SourceCitationDto>();
            }
        }
    }
}
