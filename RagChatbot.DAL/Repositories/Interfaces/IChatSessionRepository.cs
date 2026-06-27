using System;
using System.Collections.Generic;
using RagChatbot.DAL.Entities;

namespace RagChatbot.DAL.Repositories.Interfaces
{
    public interface IChatSessionRepository
    {
        ChatSession Add(ChatSession session);
        ChatSession? GetById(int sessionId);
        IEnumerable<ChatSession> GetBySubject(int userId, Guid subjectId);
        int CountBySubject(int userId, Guid subjectId);
        void Update(ChatSession session);
        void Delete(ChatSession session);
    }
}
