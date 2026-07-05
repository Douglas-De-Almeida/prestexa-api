using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class TrustedMfaDevice
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string DeviceId { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Portal { get; set; } = "los";

        public DateTime LastMfaAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public User? User { get; set; }
    }
}