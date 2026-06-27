using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RagChatbot.DAL.Data;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;

namespace RagChatbot.DAL.Repositories.Implements
{
    public class ChatSessionRepository : IChatSessionRepository
    {
        private readonly ApplicationDbContext _context;

        public ChatSessionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public ChatSession Add(ChatSession session)
        {
            _context.ChatSessions.Add(session);
            _context.SaveChanges();
            return session;
        }

        public ChatSession? GetById(int sessionId)
            => _context.ChatSessions
                       .Include(s => s.Messages)
                       .FirstOrDefault(s => s.Id == sessionId);

        public IEnumerable<ChatSession> GetBySubject(int userId, Guid subjectId)
            => _context.ChatSessions
                       .Include(s => s.Messages)
                       .Where(s => s.UserId == userId && s.SubjectId == subjectId)
                       .OrderByDescending(s => s.UpdatedAt)
                       .ThenByDescending(s => s.Id)
                       .ToList();

        public int CountBySubject(int userId, Guid subjectId)
            => _context.ChatSessions.Count(s => s.UserId == userId && s.SubjectId == subjectId);

        public void Update(ChatSession session)
        {
            _context.ChatSessions.Update(session);
            _context.SaveChanges();
        }

        public void Delete(ChatSession session)
        {
            _context.ChatSessions.Remove(session);
            _context.SaveChanges();
        }
    }
}
