namespace PrestexaAPI.Models.Responses
{
    public class LoanActivityAttachmentDto
    {
        public int Id { get; set; }
        public string AttachmentType { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFilePath { get; set; } = string.Empty;
        public string? ThumbnailFilePath { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAtUtc { get; set; }
    }

    public class LoanActivityItemDto
    {
        public int Id { get; set; }
        public string LoanNumber { get; set; } = string.Empty;
        public int? ParentActivityId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Message { get; set; }
        public bool NotifyLoanTeam { get; set; }
        public string Visibility { get; set; } = string.Empty;
        public int? ActorUserId { get; set; }
        public string? ActorName { get; set; }
        public string? ActorRole { get; set; }
        public string ActorType { get; set; } = string.Empty;
        public string? MetadataJson { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public int ReplyCount { get; set; }
        public List<LoanActivityAttachmentDto> Attachments { get; set; } = new();
        public List<LoanActivityItemDto> Replies { get; set; } = new();
    }

    public class LoanActivitiesResponseDto
    {
        public List<LoanActivityItemDto> Items { get; set; } = new();
        public string? NextCursor { get; set; }
    }
}
