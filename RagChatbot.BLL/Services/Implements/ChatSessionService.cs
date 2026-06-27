using System;
using System.Collections.Generic;
using System.Linq;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;

namespace RagChatbot.BLL.Services.Implements
{
    public class ChatSessionService : IChatSessionService
    {
        private readonly IChatSessionRepository _repository;

        public ChatSessionService(IChatSessionRepository repository)
        {
            _repository = repository;
        }

        public IEnumerable<ChatSessionDto> GetSessions(int userId, Guid subjectId)
            => _repository.GetBySubject(userId, subjectId).Select(ToDto);

        public ChatSessionDto GetOrCreateActiveSession(int userId, Guid subjectId, int? sessionId)
        {
            if (sessionId.HasValue)
            {
                var requested = _repository.GetById(sessionId.Value);
                if (requested != null && requested.UserId == userId && requested.SubjectId == subjectId)
                    return ToDto(requested);
            }

            var latest = _repository.GetBySubject(userId, subjectId).FirstOrDefault();
            return latest != null ? ToDto(latest) : CreateSession(userId, subjectId);
        }

        public ChatSessionDto CreateSession(int userId, Guid subjectId)
        {
            int nextNumber = _repository.CountBySubject(userId, subjectId) + 1;
            var now = DateTime.UtcNow;

            var session = _repository.Add(new ChatSession
            {
                UserId = userId,
                SubjectId = subjectId,
                Title = $"Phiên chat {nextNumber}",
                CreatedAt = now,
                UpdatedAt = now
            });

            return ToDto(session);
        }

        public bool BelongsToUserSubject(int userId, Guid subjectId, int sessionId)
        {
            var session = _repository.GetById(sessionId);
            return session != null && session.UserId == userId && session.SubjectId == subjectId;
        }

        public void TouchSession(int userId, Guid subjectId, int sessionId)
        {
            var session = _repository.GetById(sessionId);
            if (session == null || session.UserId != userId || session.SubjectId != subjectId)
                return;

            session.UpdatedAt = DateTime.UtcNow;
            _repository.Update(session);
        }

        public bool DeleteSession(int userId, Guid subjectId, int sessionId)
        {
            var session = _repository.GetById(sessionId);
            if (session == null || session.UserId != userId || session.SubjectId != subjectId)
                return false;

            _repository.Delete(session);
            return true;
        }

        private static ChatSessionDto ToDto(ChatSession session)
            => new()
            {
                Id = session.Id,
                UserId = session.UserId,
                SubjectId = session.SubjectId,
                Title = session.Title,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt,
                MessageCount = session.Messages?.Count ?? 0
            };
    }
}
