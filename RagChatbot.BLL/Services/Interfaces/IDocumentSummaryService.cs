using System;
using System.Threading.Tasks;
using RagChatbot.BLL.DTOs;

namespace RagChatbot.BLL.Services.Interfaces
{
    public interface IDocumentSummaryService
    {
        DocumentSummaryDto? GetByDocument(Guid documentId);
        Task<DocumentSummaryDto?> GenerateAsync(Guid documentId);
    }
}
