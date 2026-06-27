using System;
using System.Collections.Generic;
using RagChatbot.DAL.Entities;

namespace RagChatbot.DAL.Repositories.Interfaces
{
    public interface IChatMessageRepository
    {
        void Add(ChatMessage message);
        ChatMessage? GetById(int id);
        void Update(ChatMessage message);
        IEnumerable<ChatMessage> GetHistory(int userId, Guid subjectId, int sessionId, int take);
        void DeleteHistory(int userId, Guid subjectId, int sessionId);
    }
}
