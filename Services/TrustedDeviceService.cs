using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;
using PrestexaAPI.Models;

namespace PrestexaAPI.Services
{
    public class TrustedDeviceService : ITrustedDeviceService
    {
        private static readonly TimeSpan ActivityRefreshThreshold = TimeSpan.FromMinutes(15);
        private readonly AppDbContext _context;

        public TrustedDeviceService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsTrustedAsync(
            int userId,
            string portal,
            string? deviceId)
        {
            var normalizedDeviceId = NormalizeDeviceId(deviceId);
            var normalizedPortal = NormalizePortal(portal);

            if (normalizedDeviceId == null)
                return false;

            var trustedDevice = await _context.TrustedMfaDevices
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.DeviceId == normalizedDeviceId &&
                    x.Portal == normalizedPortal);

            if (trustedDevice == null)
                return false;

            if (trustedDevice.Status != TrustedDeviceStatus.Approved)
                return false;

            return trustedDevice.ExpiresAt > DateTime.UtcNow;
        }

        public async Task RememberAsync(
            int userId,
            string portal,
            string? deviceId,
            TimeSpan duration)
        {
            await RefreshActivityAsync(userId, portal, deviceId, duration);
        }

        public async Task RefreshActivityAsync(
            int userId,
            string portal,
            string? deviceId,
            TimeSpan? duration = null)
        {
            var normalizedDeviceId = NormalizeDeviceId(deviceId);
            var normalizedPortal = NormalizePortal(portal);

            if (normalizedDeviceId == null)
                return;

            var now = DateTime.UtcNow;
            var effectiveDuration = duration ?? TimeSpan.FromHours(72);
            var expiresAt = now.Add(effectiveDuration);

            var trustedDevice = _context.TrustedMfaDevices.Local
                .FirstOrDefault(x =>
                    x.UserId == userId &&
                    x.DeviceId == normalizedDeviceId &&
                    x.Portal == normalizedPortal);

            trustedDevice ??= await _context.TrustedMfaDevices
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.DeviceId == normalizedDeviceId &&
                    x.Portal == normalizedPortal);

            if (trustedDevice == null)
            {
                var companyNmlsNumber = await _context.Users
                    .Where(x => x.Id == userId)
                    .Select(x => x.CompanyNmlsNumber)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                    return;

                trustedDevice = new TrustedMfaDevice
                {
                    UserId = userId,
                    CompanyNmlsNumber = companyNmlsNumber,
                    DeviceId = normalizedDeviceId,
                    DeviceName = "Trusted Device",
                    Portal = normalizedPortal,
                    Status = TrustedDeviceStatus.Approved,
                    FirstRegisteredAt = now,
                    LastLoginAt = now,
                    LastActivityAt = now,
                    LastMfaAt = now,
                    ExpiresAt = expiresAt,
                    ApprovedAtUtc = now,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.TrustedMfaDevices.Add(trustedDevice);
                await _context.SaveChangesAsync();
                return;
            }

            var shouldRefreshActivity = trustedDevice.LastActivityAt is null ||
                trustedDevice.LastActivityAt.Value.Add(ActivityRefreshThreshold) <= now;

            if (!shouldRefreshActivity)
                return;

            trustedDevice.LastMfaAt = now;
            trustedDevice.LastActivityAt = now;
            trustedDevice.ExpiresAt = expiresAt;
            trustedDevice.Status = TrustedDeviceStatus.Approved;
            trustedDevice.UpdatedAt = now;
            trustedDevice.ApprovedAtUtc ??= now;
            trustedDevice.LastLoginAt ??= now;

            await _context.SaveChangesAsync();
        }

        private static string NormalizePortal(string portal)
        {
            var normalized = portal.Trim().ToLowerInvariant();

            return normalized switch
            {
                "borrower" => "borrower",
                "rea" => "rea",
                "los" => "los",
                _ => "los"
            };
        }

        private static string? NormalizeDeviceId(string? deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return null;

            var normalized = deviceId.Trim();

            if (normalized.Length > 100)
                return null;

            return normalized;
        }
    }
}