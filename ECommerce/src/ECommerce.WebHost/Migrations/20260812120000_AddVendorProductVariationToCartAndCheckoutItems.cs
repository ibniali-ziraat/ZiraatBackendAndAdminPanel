using ECommerce.Module.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.WebHost.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(SimplDbContext))]
    [Migration("20260812120000_AddVendorProductVariationToCartAndCheckoutItems")]
    public partial class AddVendorProductVariationToCartAndCheckoutItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[ShoppingCart_CartItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[ShoppingCart_CartItem]', N'VendorProductId') IS NULL
BEGIN
    ALTER TABLE [dbo].[ShoppingCart_CartItem] ADD [VendorProductId] BIGINT NULL;
END;

IF OBJECT_ID(N'[dbo].[ShoppingCart_CartItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[ShoppingCart_CartItem]', N'VendorProductVariationId') IS NULL
BEGIN
    ALTER TABLE [dbo].[ShoppingCart_CartItem] ADD [VendorProductVariationId] BIGINT NULL;
END;

IF OBJECT_ID(N'[dbo].[Checkouts_CheckoutItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[Checkouts_CheckoutItem]', N'VendorId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Checkouts_CheckoutItem] ADD [VendorId] BIGINT NULL;
END;

IF OBJECT_ID(N'[dbo].[Checkouts_CheckoutItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[Checkouts_CheckoutItem]', N'VendorProductId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Checkouts_CheckoutItem] ADD [VendorProductId] BIGINT NULL;
END;

IF OBJECT_ID(N'[dbo].[Checkouts_CheckoutItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[Checkouts_CheckoutItem]', N'VendorProductVariationId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Checkouts_CheckoutItem] ADD [VendorProductVariationId] BIGINT NULL;
END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Checkouts_CheckoutItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[Checkouts_CheckoutItem]', N'VendorProductVariationId') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Checkouts_CheckoutItem] DROP COLUMN [VendorProductVariationId];
END;

IF OBJECT_ID(N'[dbo].[Checkouts_CheckoutItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[Checkouts_CheckoutItem]', N'VendorProductId') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Checkouts_CheckoutItem] DROP COLUMN [VendorProductId];
END;

IF OBJECT_ID(N'[dbo].[Checkouts_CheckoutItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[Checkouts_CheckoutItem]', N'VendorId') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Checkouts_CheckoutItem] DROP COLUMN [VendorId];
END;

IF OBJECT_ID(N'[dbo].[ShoppingCart_CartItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[ShoppingCart_CartItem]', N'VendorProductVariationId') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[ShoppingCart_CartItem] DROP COLUMN [VendorProductVariationId];
END;

IF OBJECT_ID(N'[dbo].[ShoppingCart_CartItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[ShoppingCart_CartItem]', N'VendorProductId') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[ShoppingCart_CartItem] DROP COLUMN [VendorProductId];
END;");
        }
    }
}
