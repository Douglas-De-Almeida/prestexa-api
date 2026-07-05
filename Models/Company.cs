using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class Company
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string NmlsNumber { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(200)]
        public string? StreetAddress { get; set; }

        [MaxLength(100)]
        public string? AptUnit { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(2)]
        public string? State { get; set; }

        [MaxLength(10)]
        public string? ZipCode { get; set; }

        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(255)]
        public string? WebsiteUrl { get; set; }

        [MaxLength(20)]
        public string? PrimaryColor { get; set; }

        public bool DbaBrandingEnabled { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}