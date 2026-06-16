using System;
using System.Collections.Generic;
using System.Linq;
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

        public void Save(int userId, Guid subjectId, string sender, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;
            _repository.Add(new ChatMessage
            {
                UserId    = userId,
                SubjectId = subjectId,
                Sender    = sender,
                Content   = content,
                CreatedAt = DateTime.UtcNow
            });
        }

        public IEnumerable<ChatMessageDto> GetHistory(int userId, Guid subjectId, int take = 50)
            => _repository.GetHistory(userId, subjectId, take).Select(m => new ChatMessageDto
            {
                Sender    = m.Sender,
                Content   = m.Content,
                CreatedAt = m.CreatedAt
            });
    }
}
