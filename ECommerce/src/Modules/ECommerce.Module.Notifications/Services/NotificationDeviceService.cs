using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Notifications.Areas.Notifications.ViewModels;
using ECommerce.Module.Notifications.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Module.Notifications.Services
{
    public class NotificationDeviceService : INotificationDeviceService
    {
        private static readonly string[] AllowedPlatforms = { "android", "ios", "web" };

        private readonly IRepository<NotificationDevice> _deviceRepository;

        public NotificationDeviceService(IRepository<NotificationDevice> deviceRepository)
        {
            _deviceRepository = deviceRepository;
        }

        public async Task<NotificationDevice> SaveTokenAsync(string token, string platform, long? userId, decimal? latitude = null, decimal? longitude = null, string locationName = null)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("token required", nameof(token));
            }

            ValidateCoordinates(latitude, longitude);

            var normalizedToken = token.Trim();
            var normalizedPlatform = NormalizePlatform(platform);
            var tokenHash = ComputeTokenHash(normalizedToken);
            var normalizedLocationName = NormalizeLocationName(locationName);

            var device = await _deviceRepository.Query().FirstOrDefaultAsync(x => x.TokenHash == tokenHash);
            if (device == null)
            {
                device = new NotificationDevice
                {
                    Token = normalizedToken,
                    TokenHash = tokenHash,
                    Platform = normalizedPlatform,
                    UserId = userId,
                    Latitude = latitude,
                    Longitude = longitude,
                    LocationName = normalizedLocationName,
                    CreatedOn = DateTimeOffset.Now,
                    UpdatedOn = DateTimeOffset.Now
                };
                _deviceRepository.Add(device);
            }
            else
            {
                device.UserId = userId;
                device.Token = normalizedToken;
                device.TokenHash = tokenHash;
                device.Platform = normalizedPlatform;
                device.Latitude = latitude;
                device.Longitude = longitude;
                device.LocationName = normalizedLocationName;
                device.UpdatedOn = DateTimeOffset.Now;
                device.IsDeleted = false;
            }

            await _deviceRepository.SaveChangesAsync();
            return device;
        }

        public async Task<IList<NotificationDeviceDto>> GetDevicesAsync(long? userId = null)
        {
            var query = _deviceRepository.Query().Where(x => !x.IsDeleted);
            if (userId.HasValue)
            {
                query = query.Where(x => x.UserId == userId.Value);
            }

            return await query
                .OrderByDescending(x => x.UpdatedOn)
                .Select(x => new NotificationDeviceDto
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    Token = x.Token,
                    Platform = x.Platform,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    LocationName = x.LocationName,
                    CreatedOn = x.CreatedOn,
                    UpdatedOn = x.UpdatedOn
                })
                .ToListAsync();
        }

        public async Task<IList<string>> GetTokensForUsersAsync(IEnumerable<long> userIds)
        {
            var ids = userIds?.Distinct().ToArray() ?? Array.Empty<long>();
            if (ids.Length == 0)
            {
                return Array.Empty<string>();
            }

            return await _deviceRepository.Query()
                .Where(x => !x.IsDeleted && x.UserId.HasValue && ids.Contains(x.UserId.Value))
                .Select(x => x.Token)
                .Distinct()
                .ToListAsync();
        }

        private static void ValidateCoordinates(decimal? latitude, decimal? longitude)
        {
            if (latitude.HasValue != longitude.HasValue)
            {
                throw new ArgumentException("latitude and longitude must be supplied together");
            }

            if (latitude.HasValue && (latitude.Value < -90 || latitude.Value > 90 || longitude.Value < -180 || longitude.Value > 180))
            {
                throw new ArgumentException("valid latitude/longitude required");
            }
        }

        private static string NormalizeLocationName(string locationName)
        {
            return string.IsNullOrWhiteSpace(locationName) ? null : locationName.Trim();
        }

        private static string ComputeTokenHash(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }

        private static string NormalizePlatform(string platform)
        {
            var normalizedPlatform = string.IsNullOrWhiteSpace(platform)
                ? "android"
                : platform.Trim().ToLowerInvariant();

            return AllowedPlatforms.Contains(normalizedPlatform) ? normalizedPlatform : "android";
        }
    }
}