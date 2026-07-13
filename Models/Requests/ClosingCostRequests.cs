using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models.Requests
{
    public class CreateClosingCostItemRequest
    {
        [Required]
        [MaxLength(200)]
        public string FeeName { get; set; } = null!;

        [MaxLength(100)]
        public string? FeeCategory { get; set; }

        public decimal Amount { get; set; }

        public decimal? Percentage { get; set; }

        [MaxLength(100)]
        public string? PaidBy { get; set; }

        public bool IsFinanceCharge { get; set; }

        public bool IsAprFee { get; set; }

        [MaxLength(100)]
        public string? StateApplicability { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateClosingCostItemRequest : CreateClosingCostItemRequest
    {
    }
}