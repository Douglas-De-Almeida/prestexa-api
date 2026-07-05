using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class BorrowerAddress
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyNmlsNumber { get; set; } = null!;

        public Company Company { get; set; } = null!;

        public int BorrowerId { get; set; }

        public Borrower Borrower { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string AddressType { get; set; } = "Current";

        [Required]
        [MaxLength(200)]
        public string StreetAddress { get; set; } = null!;

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(2)]
        public string? State { get; set; }

        [MaxLength(10)]
        public string? ZipCode { get; set; }

        [MaxLength(100)]
        public string? OccupancyType { get; set; }

        public int? YearsSpent { get; set; }

        public int? MonthsSpent { get; set; }
    }
}