using ECommerce.Module.Notifications.Models;

namespace ECommerce.Module.Notifications
{
    /// <summary>
    /// Pre-defined notifications.
    /// </summary>
    public static class NotificationDefinitions
    {
        public static class Names
        {
            public const string NewUserRegistered = "NewUserRegistered";

            public const string VendorOrderPlaced = "VendorOrderPlaced";

            public const string CustomerOrderCompleted = "CustomerOrderCompleted";

            public const string CustomerOrderStatusChanged = "CustomerOrderStatusChanged";

            public const string CustomerCreditPaymentDue = "CustomerCreditPaymentDue";

            public const string CustomerCreditPaymentOverdue = "CustomerCreditPaymentOverdue";

            public const string CustomerCreditPaymentNearDue = "CustomerCreditPaymentNearDue";

            public const string ManualCustomerNotification = "ManualCustomerNotification";
        }

        public static NotificationDefinition[] DefaultItems()
        {
            var notifications = new[]
            {
                new NotificationDefinition(Names.NewUserRegistered, forRoles: new[] { "admin" }),
                new NotificationDefinition(
                    Names.VendorOrderPlaced,
                    forRoles: new[] { "admin", "vendor" }
                ),
                new NotificationDefinition(Names.CustomerOrderCompleted),
                new NotificationDefinition(Names.CustomerOrderStatusChanged),
                new NotificationDefinition(Names.CustomerCreditPaymentDue),
                new NotificationDefinition(Names.CustomerCreditPaymentOverdue),
                new NotificationDefinition(Names.CustomerCreditPaymentNearDue),
                new NotificationDefinition(Names.ManualCustomerNotification),
            };

            return notifications;
        }
    }
}
