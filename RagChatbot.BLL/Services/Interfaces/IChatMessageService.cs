using System;
using System.Collections.Generic;
using RagChatbot.BLL.DTOs;

namespace RagChatbot.BLL.Services.Interfaces
{
    public interface IChatMessageService
    {
        void Save(int userId, Guid subjectId, string sender, string content, IReadOnlyList<SourceCitationDto>? sources = null);
        IEnumerable<ChatMessageDto> GetHistory(int userId, Guid subjectId, int take = 50);
    }
}
