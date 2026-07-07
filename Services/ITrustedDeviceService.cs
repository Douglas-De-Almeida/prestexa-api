namespace PrestexaAPI.Services
{
    public interface ITrustedDeviceService
    {
        Task<bool> IsTrustedAsync(int userId, string portal, string? deviceId);

        Task RememberAsync(
            int userId,
            string portal,
            string? deviceId,
            TimeSpan duration);
    }
}