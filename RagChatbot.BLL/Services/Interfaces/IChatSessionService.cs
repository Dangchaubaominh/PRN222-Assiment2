using System;
using System.Collections.Generic;
using RagChatbot.BLL.DTOs;

namespace RagChatbot.BLL.Services.Interfaces
{
    public interface IChatSessionService
    {
        IEnumerable<ChatSessionDto> GetSessions(int userId, Guid subjectId);
        ChatSessionDto GetOrCreateActiveSession(int userId, Guid subjectId, int? sessionId);
        ChatSessionDto CreateSession(int userId, Guid subjectId);
        bool BelongsToUserSubject(int userId, Guid subjectId, int sessionId);
        void TouchSession(int userId, Guid subjectId, int sessionId);
        bool DeleteSession(int userId, Guid subjectId, int sessionId);
    }
}
