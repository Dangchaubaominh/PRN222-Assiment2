using System;

namespace RagChatbot.BLL.DTOs
{
    public class DocumentSummaryDto
    {
        public int Id { get; set; }
        public Guid DocumentId { get; set; }
        public string Summary { get; set; } = "";
        public string KeyPoints { get; set; } = "";
        public string LearningObjectives { get; set; } = "";
        public string ImportantTerms { get; set; } = "";
        public DateTime UpdatedAt { get; set; }
    }
}
