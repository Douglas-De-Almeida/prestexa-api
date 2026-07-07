using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class UserSession
    {
        [Key]
        public Guid SessionId { get; set; }

        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Portal { get; set; } = "los";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        [MaxLength(255)]
        public string? RevocationReason { get; set; }

        public User? User { get; set; }
    }
}