namespace PrestexaAPI.Models.Responses
{
    public class EmailTrackingItemResponse
    {
        public int Id { get; set; }
        public string? LoanNumber { get; set; }
        public string RecipientEmail { get; set; } = null!;
        public string? RecipientName { get; set; }
        public string RecipientType { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public string TemplateKey { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime? SentAtUtc { get; set; }
        public DateTime? DeliveredAtUtc { get; set; }
        public DateTime? FailedAtUtc { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ProviderMessageId { get; set; }
        public string TriggeredByEvent { get; set; } = null!;
    }

    public class EmailTrackingListResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public IReadOnlyList<EmailTrackingItemResponse> Items { get; set; } = [];
    }

    public class SmsTrackingListResponse
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalCount { get; set; }
        public IReadOnlyList<object> Items { get; set; } = [];
    }
}