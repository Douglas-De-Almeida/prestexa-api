using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class UserBranchRole
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyNmlsNumber { get; set; } = null!;

        public Company Company { get; set; } = null!;

        public int UserId { get; set; }

        public User User { get; set; } = null!;

        public int BranchId { get; set; }

        public Branch Branch { get; set; } = null!;

        public int RoleId { get; set; }

        public Role Role { get; set; } = null!;

        public bool IsDefaultBranch { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}