using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class MismoImportRecord
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyNmlsNumber { get; set; } = null!;

        public Company Company { get; set; } = null!;

        [Required]
        [MaxLength(64)]
        public string ContentSha256 { get; set; } = null!;

        public int LoanId { get; set; }

        public Loan Loan { get; set; } = null!;

        public int ImportedByUserId { get; set; }

        public int? SourceMismoFileId { get; set; }

        [MaxLength(50)]
        public string? MismoVersion { get; set; }

        public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
