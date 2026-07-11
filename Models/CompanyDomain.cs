using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class CompanyDomain
    {
        public int Id { get; set; }

        public int CompanyId { get; set; }

        public Company Company { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Subdomain { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? CustomDomain { get; set; }

        [MaxLength(50)]
        public string? SslStatus { get; set; }

        [MaxLength(50)]
        public string? DnsStatus { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsVerified { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public int? CreatedByUserId { get; set; }

        public User? CreatedByUser { get; set; }

        public int? UpdatedByUserId { get; set; }

        public User? UpdatedByUser { get; set; }
    }
}
