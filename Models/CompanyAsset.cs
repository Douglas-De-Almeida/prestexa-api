using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class CompanyAsset
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyNmlsNumber { get; set; } = null!;

        public Company Company { get; set; } = null!;

        public int UploadedByUserId { get; set; }

        public CompanyAssetCategory Category { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = null!;

        [Required]
        [MaxLength(1000)]
        public string FilePath { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = null!;

        public long FileSize { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}