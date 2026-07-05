using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class Branch
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyNmlsNumber { get; set; } = null!;

        public Company Company { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(20)]
        public string? BranchNmlsNumber { get; set; }

        [MaxLength(200)]
        public string? StreetAddress { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(2)]
        public string? State { get; set; }

        [MaxLength(10)]
        public string? ZipCode { get; set; }

        public bool IsHq { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}