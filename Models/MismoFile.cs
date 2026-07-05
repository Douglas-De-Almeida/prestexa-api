using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class MismoFile
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyNmlsNumber { get; set; } = null!;

        public Company Company { get; set; } = null!;

        public int LoanId { get; set; }

        public Loan Loan { get; set; } = null!;

        public int UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = null!;

        [Required]
        [MaxLength(1000)]
        public string FilePath { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = "application/xml";

        [MaxLength(50)]
        public string? MismoVersion { get; set; }

        public long FileSize { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}