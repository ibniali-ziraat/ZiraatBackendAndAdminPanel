using System;
using System.ComponentModel.DataAnnotations;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Notifications.Models
{
    public class NotificationDevice : EntityBase
    {
        public const int MaxTokenLength = 2048;

        public const int MaxPlatformLength = 32;

        [Required]
        [MaxLength(MaxTokenLength)]
        public string Token { get; set; }

        [Required]
        [MaxLength(64)]
        public string TokenHash { get; set; }

        public long? UserId { get; set; }

        [MaxLength(MaxPlatformLength)]
        public string Platform { get; set; } = "android";

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        [MaxLength(256)]
        public string LocationName { get; set; }

        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;

        public DateTimeOffset UpdatedOn { get; set; } = DateTimeOffset.Now;

        public bool IsDeleted { get; set; }
    }

    public class WeatherAlertNotificationLog : EntityBase
    {
        [Required]
        [MaxLength(256)]
        public string AlertKey { get; set; }

        public long? UserId { get; set; }

        [Required]
        [MaxLength(64)]
        public string TokenHash { get; set; }

        [MaxLength(128)]
        public string Event { get; set; }

        [MaxLength(64)]
        public string Severity { get; set; }

        public DateTimeOffset SentOn { get; set; } = DateTimeOffset.Now;
    }
}

