namespace PrestexaAPI.Models
{
    public class MediaAsset
    {
        public int Id { get; set; }

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public string StoragePath { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }
}