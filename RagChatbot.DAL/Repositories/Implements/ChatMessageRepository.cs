using System;
using System.Collections.Generic;
using System.Linq;
using RagChatbot.DAL.Data;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;

namespace RagChatbot.DAL.Repositories.Implements
{
    public class ChatMessageRepository : IChatMessageRepository
    {
        private readonly ApplicationDbContext _context;

        public ChatMessageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(ChatMessage message)
        {
            _context.ChatMessages.Add(message);
            _context.SaveChanges();
        }

        public IEnumerable<ChatMessage> GetHistory(int userId, Guid subjectId, int sessionId, int take)
            => _context.ChatMessages
                       .Where(m => m.UserId == userId &&
                                   m.SubjectId == subjectId &&
                                   m.SessionId == sessionId)
                       .OrderByDescending(m => m.Id)
                       .Take(take)
                       .OrderBy(m => m.Id)
                       .ToList();

        public void DeleteHistory(int userId, Guid subjectId, int sessionId)
        {
            var messages = _context.ChatMessages
                                   .Where(m => m.UserId == userId &&
                                               m.SubjectId == subjectId &&
                                               m.SessionId == sessionId)
                                   .ToList();

            if (messages.Count == 0)
                return;

            _context.ChatMessages.RemoveRange(messages);
            _context.SaveChanges();
        }
    }
}
