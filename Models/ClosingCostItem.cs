namespace PrestexaAPI.Models
{
    public class ClosingCostItem
    {
        public int Id { get; set; }
        public string CompanyNmlsNumber { get; set; } = null!;
        public string FeeName { get; set; } = null!;
        public string? FeeCategory { get; set; }
        public decimal Amount { get; set; }
        public decimal? Percentage { get; set; }
        public string? PaidBy { get; set; }
        public bool IsFinanceCharge { get; set; }
        public bool IsAprFee { get; set; }
        public string? StateApplicability { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
        public int? UpdatedByUserId { get; set; }
    }
}