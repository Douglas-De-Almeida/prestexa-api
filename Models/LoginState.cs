using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class LoginState
    {
        [Key]
        public Guid StateId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string ReturnUrl { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Portal { get; set; } = "los";

        [MaxLength(255)]
        public string? SourceHost { get; set; }

        [MaxLength(100)]
        public string? TenantSlug { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime? UsedAt { get; set; }

        public int? UsedByUserId { get; set; }
        
        
    }
}