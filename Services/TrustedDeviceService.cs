using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;
using PrestexaAPI.Models;

namespace PrestexaAPI.Services
{
    public class TrustedDeviceService : ITrustedDeviceService
    {
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
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.DeviceId == normalizedDeviceId &&
                    x.Portal == normalizedPortal);

            if (trustedDevice == null)
                return false;

            return trustedDevice.ExpiresAt > DateTime.UtcNow;
        }

        public async Task RememberAsync(
            int userId,
            string portal,
            string? deviceId,
            TimeSpan duration)
        {
            var normalizedDeviceId = NormalizeDeviceId(deviceId);
            var normalizedPortal = NormalizePortal(portal);

            if (normalizedDeviceId == null)
                return;

            var now = DateTime.UtcNow;
            var expiresAt = now.Add(duration);

            var trustedDevice = await _context.TrustedMfaDevices
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.DeviceId == normalizedDeviceId &&
                    x.Portal == normalizedPortal);

            if (trustedDevice == null)
            {
                trustedDevice = new TrustedMfaDevice
                {
                    UserId = userId,
                    DeviceId = normalizedDeviceId,
                    Portal = normalizedPortal,
                    LastMfaAt = now,
                    ExpiresAt = expiresAt,
                    CreatedAt = now
                };

                _context.TrustedMfaDevices.Add(trustedDevice);
            }
            else
            {
                trustedDevice.LastMfaAt = now;
                trustedDevice.ExpiresAt = expiresAt;
                trustedDevice.UpdatedAt = now;
            }
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