using ECommerce.Module.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.WebHost.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(SimplDbContext))]
    [Migration("20260805093000_AddVendorIdToCartRules")]
    public partial class AddVendorIdToCartRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "VendorId",
                table: "Pricing_CartRule",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pricing_CartRule_VendorId",
                table: "Pricing_CartRule",
                column: "VendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pricing_CartRule_Core_Vendor_VendorId",
                table: "Pricing_CartRule",
                column: "VendorId",
                principalTable: "Core_Vendor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pricing_CartRule_Core_Vendor_VendorId",
                table: "Pricing_CartRule");

            migrationBuilder.DropIndex(
                name: "IX_Pricing_CartRule_VendorId",
                table: "Pricing_CartRule");

            migrationBuilder.DropColumn(
                name: "VendorId",
                table: "Pricing_CartRule");
        }
    }
}
