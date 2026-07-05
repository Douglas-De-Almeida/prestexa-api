using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models.Requests
{
    public class CreateLoanRequest
    {
        [Required]
        [MaxLength(200)]
        public string Subject_Street_Address { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Subject_City { get; set; } = null!;

        [Required]
        [MaxLength(2)]
        public string Subject_State { get; set; } = null!;

        [Required]
        [MaxLength(10)]
        public string Subject_ZipCode { get; set; } = null!;

        [Range(1, 100000000)]
        public decimal LoanAmount { get; set; }
    }
}