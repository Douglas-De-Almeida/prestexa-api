using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class BorrowerAsset
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
        public string AssetType { get; set; } = null!;

        [Range(0, 100000000)]
        public decimal AssetCashOrMarketValueAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
