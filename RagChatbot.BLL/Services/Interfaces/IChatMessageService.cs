using System;
using System.Collections.Generic;
using RagChatbot.BLL.DTOs;
using RagChatbot.DAL.Entities;

namespace RagChatbot.BLL.Services.Interfaces
{
    public interface IChatMessageService
    {
        int Save(int userId, Guid subjectId, int sessionId, string sender, string content, IReadOnlyList<SourceCitationDto>? sources = null);
        void UpdateFeedback(int messageId, int userId, FeedbackType? feedback);
        IEnumerable<ChatMessageDto> GetHistory(int userId, Guid subjectId, int sessionId, int take = 50);
        void ClearHistory(int userId, Guid subjectId, int sessionId);
    }
}
