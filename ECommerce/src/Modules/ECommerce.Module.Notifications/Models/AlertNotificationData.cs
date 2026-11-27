using System;

namespace ECommerce.Module.Notifications.Models
{
    [Serializable]
    public class AlertNotificationData : NotificationData
    {
        public string Title
        {
            get => _title ?? (this[nameof(Title)] as string);
            set
            {
                this[nameof(Title)] = value;
                _title = value;
            }
        }
        private string _title;

        public string Message
        {
            get => _message ?? (this[nameof(Message)] as string);
            set
            {
                this[nameof(Message)] = value;
                _message = value;
            }
        }
        private string _message;

        public string AlertType
        {
            get => _alertType ?? (this[nameof(AlertType)] as string);
            set
            {
                this[nameof(AlertType)] = value;
                _alertType = value;
            }
        }
        private string _alertType;

        public string TargetApp
        {
            get => _targetApp ?? (this[nameof(TargetApp)] as string);
            set
            {
                this[nameof(TargetApp)] = value;
                _targetApp = value;
            }
        }
        private string _targetApp;

        public long? OrderId
        {
            get => GetNullableLong(nameof(OrderId), _orderId);
            set
            {
                this[nameof(OrderId)] = value;
                _orderId = value;
            }
        }
        private long? _orderId;

        public long? VendorId
        {
            get => GetNullableLong(nameof(VendorId), _vendorId);
            set
            {
                this[nameof(VendorId)] = value;
                _vendorId = value;
            }
        }
        private long? _vendorId;

        public long? CustomerId
        {
            get => GetNullableLong(nameof(CustomerId), _customerId);
            set
            {
                this[nameof(CustomerId)] = value;
                _customerId = value;
            }
        }
        private long? _customerId;

        public decimal? Balance
        {
            get => GetNullableDecimal(nameof(Balance), _balance);
            set
            {
                this[nameof(Balance)] = value;
                _balance = value;
            }
        }
        private decimal? _balance;

        public DateTime? DueDate
        {
            get => GetNullableDateTime(nameof(DueDate), _dueDate);
            set
            {
                this[nameof(DueDate)] = value;
                _dueDate = value;
            }
        }
        private DateTime? _dueDate;

        private long? GetNullableLong(string key, long? cached)
        {
            if (cached.HasValue)
            {
                return cached;
            }

            return Properties.TryGetValue(key, out var value) && value != null
                ? Convert.ToInt64(value)
                : null;
        }

        private decimal? GetNullableDecimal(string key, decimal? cached)
        {
            if (cached.HasValue)
            {
                return cached;
            }

            return Properties.TryGetValue(key, out var value) && value != null
                ? Convert.ToDecimal(value)
                : null;
        }

        private DateTime? GetNullableDateTime(string key, DateTime? cached)
        {
            if (cached.HasValue)
            {
                return cached;
            }

            return Properties.TryGetValue(key, out var value) && value != null
                ? Convert.ToDateTime(value)
                : null;
        }
    }
}
