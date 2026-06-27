using System;
using System.ComponentModel.DataAnnotations;

namespace RagChatbot.DAL.Entities
{
    public class DocumentSummary
    {
        [Key]
        public int Id { get; set; }
        public Guid DocumentId { get; set; }
        public Document Document { get; set; } = default!;
        public string Summary { get; set; } = "";
        public string KeyPoints { get; set; } = "";
        public string LearningObjectives { get; set; } = "";
        public string ImportantTerms { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
