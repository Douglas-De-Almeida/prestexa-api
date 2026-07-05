namespace PrestexaAPI.Services
{
    public interface ICurrentUserService
    {
        int? UserId { get; }
        string? CompanyNmlsNumber { get; }
        string? Role { get; }
        bool IsSuperAdmin { get; }
    }
}