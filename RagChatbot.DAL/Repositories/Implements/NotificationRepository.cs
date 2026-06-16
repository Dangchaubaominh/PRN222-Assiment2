using System.Collections.Generic;
using System.Linq;
using RagChatbot.DAL.Data;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;

namespace RagChatbot.DAL.Repositories.Implements
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _context;

        public NotificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(Notification notification)
        {
            _context.Notifications.Add(notification);
            _context.SaveChanges();
        }

        public IEnumerable<Notification> GetByUser(int userId, int take)
        {
            return _context.Notifications
                           .Where(n => n.UserId == userId)
                           .OrderByDescending(n => n.CreatedAt)
                           .Take(take)
                           .ToList();
        }

        public int CountUnread(int userId)
        {
            return _context.Notifications.Count(n => n.UserId == userId && !n.IsRead);
        }

        public void MarkAllRead(int userId)
        {
            var unread = _context.Notifications
                                 .Where(n => n.UserId == userId && !n.IsRead)
                                 .ToList();
            if (unread.Count == 0) return;

            foreach (var n in unread) n.IsRead = true;
            _context.SaveChanges();
        }
    }
}
