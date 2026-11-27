using ECommerce.Module.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.WebHost.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(SimplDbContext))]
    [Migration("20260814120000_AddVendorProductVariationToOrderItems")]
    public partial class AddVendorProductVariationToOrderItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Orders_OrderItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[Orders_OrderItem]', N'VendorId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Orders_OrderItem] ADD [VendorId] BIGINT NULL;
END;

IF OBJECT_ID(N'[dbo].[Orders_OrderItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[Orders_OrderItem]', N'VendorProductId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Orders_OrderItem] ADD [VendorProductId] BIGINT NULL;
END;

IF OBJECT_ID(N'[dbo].[Orders_OrderItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[Orders_OrderItem]', N'VendorProductVariationId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Orders_OrderItem] ADD [VendorProductVariationId] BIGINT NULL;
END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Orders_OrderItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[Orders_OrderItem]', N'VendorProductVariationId') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Orders_OrderItem] DROP COLUMN [VendorProductVariationId];
END;

IF OBJECT_ID(N'[dbo].[Orders_OrderItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[Orders_OrderItem]', N'VendorProductId') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Orders_OrderItem] DROP COLUMN [VendorProductId];
END;

IF OBJECT_ID(N'[dbo].[Orders_OrderItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[Orders_OrderItem]', N'VendorId') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Orders_OrderItem] DROP COLUMN [VendorId];
END;");
        }
    }
}
