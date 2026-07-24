namespace PrestexaAPI.Services
{
    public class TrustedDeviceService : ITrustedDeviceService
    {
        public async Task<bool> IsTrustedAsync(
            int userId,
            string portal,
            string? deviceId)
        {
            await Task.CompletedTask;
            return false;
        }

        public async Task RememberAsync(
            int userId,
            string portal,
            string? deviceId,
            TimeSpan duration)
        {
            await Task.CompletedTask;
        }

        public async Task RefreshActivityAsync(
            int userId,
            string portal,
            string? deviceId,
            TimeSpan? duration = null)
        {
            await Task.CompletedTask;
        }
    }
}