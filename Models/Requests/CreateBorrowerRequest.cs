using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models.Requests
{
    public class CreateBorrowerRequest
    {
        [Required]
        [MaxLength(50)]
        public string BorrowerType { get; set; } = "PrimaryBorrower";

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;

        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? CellPhone { get; set; }

        public DateTime? DateOfBirth { get; set; }
    }
}