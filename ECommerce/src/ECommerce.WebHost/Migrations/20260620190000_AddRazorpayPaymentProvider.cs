using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.WebHost.Migrations
{
    /// <inheritdoc />
    public partial class AddRazorpayPaymentProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Payments_PaymentProvider",
                columns: new[] { "Id", "AdditionalSettings", "ConfigureUrl", "IsEnabled", "LandingViewComponentName", "Name" },
                values: new object[]
                {
                    "Razorpay",
                    "{\"KeyId\":\"\",\"KeySecret\":\"\",\"WebhookSecret\":\"\",\"Currency\":\"INR\"}",
                    "payments-razor-config",
                    true,
                    "RazorpayLanding",
                    "Razorpay"
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Payments_PaymentProvider",
                keyColumn: "Id",
                keyValue: "Razorpay");
        }
    }
}
