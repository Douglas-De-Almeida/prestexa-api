using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class LoanActivityAttachment
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyNmlsNumber { get; set; } = null!;

        public Company Company { get; set; } = null!;

        public int LoanId { get; set; }

        public Loan Loan { get; set; } = null!;

        public int LoanActivityId { get; set; }

        public LoanActivity LoanActivity { get; set; } = null!;

        public LoanActivityAttachmentType AttachmentType { get; set; } = LoanActivityAttachmentType.Document;

        [Required]
        [MaxLength(255)]
        public string OriginalFileName { get; set; } = null!;

        [Required]
        [MaxLength(1000)]
        public string StoredFilePath { get; set; } = null!;

        [MaxLength(1000)]
        public string? ThumbnailFilePath { get; set; }

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = null!;

        public long FileSize { get; set; }

        public int? UploadedByUserId { get; set; }

        public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
