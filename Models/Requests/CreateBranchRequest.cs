using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models.Requests
{
    public class CreateBranchRequest
    {
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
    }
}