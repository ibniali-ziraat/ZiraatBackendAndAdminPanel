using System;
using ECommerce.Module.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.WebHost.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(SimplDbContext))]
    [Migration("20260807123000_AddVendorProductVariations")]
    public partial class AddVendorProductVariations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Core_VendorProductVariation",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VendorProductId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Sku = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Gtin = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    MaximumRetailPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AssignedPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OriginalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DiscountPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TaxRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NetSalePriceForZiraat = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreditSaleHikePercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SalePriceForCreditor = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsAvailableForCreditBuyers = table.Column<bool>(type: "bit", nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortIndex = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LatestUpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Core_VendorProductVariation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Core_VendorProductVariation_Core_VendorProduct_VendorProductId",
                        column: x => x.VendorProductId,
                        principalTable: "Core_VendorProduct",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Core_VendorProductVariationOption",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VendorProductVariationId = table.Column<long>(type: "bigint", nullable: false),
                    OptionId = table.Column<long>(type: "bigint", nullable: false),
                    OptionName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Value = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Core_VendorProductVariationOption", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Core_VendorProductVariationOption_Core_VendorProductVariation_VendorProductVariationId",
                        column: x => x.VendorProductVariationId,
                        principalTable: "Core_VendorProductVariation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Core_VendorProduct_VendorId_ProductId",
                table: "Core_VendorProduct",
                columns: new[] { "VendorId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_Core_VendorProductVariation_VendorProductId_Sku",
                table: "Core_VendorProductVariation",
                columns: new[] { "VendorProductId", "Sku" });

            migrationBuilder.CreateIndex(
                name: "IX_Core_VendorProductVariationOption_VendorProductVariationId_OptionId_Value",
                table: "Core_VendorProductVariationOption",
                columns: new[] { "VendorProductVariationId", "OptionId", "Value" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Core_VendorProductVariationOption");

            migrationBuilder.DropTable(
                name: "Core_VendorProductVariation");

            migrationBuilder.DropIndex(
                name: "IX_Core_VendorProduct_VendorId_ProductId",
                table: "Core_VendorProduct");
        }
    }
}
