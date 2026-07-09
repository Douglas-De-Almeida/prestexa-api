using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class Realtor
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyNmlsNumber { get; set; } = null!;

        public Company Company { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = null!;

        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? PhoneNumber { get; set; }

        [MaxLength(50)]
        public string? LicenseNumber { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
