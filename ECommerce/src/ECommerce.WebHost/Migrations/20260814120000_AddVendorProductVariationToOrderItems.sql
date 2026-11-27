IF OBJECT_ID(N'[dbo].[Orders_OrderItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[Orders_OrderItem]', N'VendorId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Orders_OrderItem]
        ADD [VendorId] BIGINT NULL;
END;

IF OBJECT_ID(N'[dbo].[Orders_OrderItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[Orders_OrderItem]', N'VendorProductId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Orders_OrderItem]
        ADD [VendorProductId] BIGINT NULL;
END;

IF OBJECT_ID(N'[dbo].[Orders_OrderItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[Orders_OrderItem]', N'VendorProductVariationId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Orders_OrderItem]
        ADD [VendorProductVariationId] BIGINT NULL;
END;

IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NOT NULL
    AND NOT EXISTS (
        SELECT 1
        FROM [dbo].[__EFMigrationsHistory]
        WHERE [MigrationId] = N'20260814120000_AddVendorProductVariationToOrderItems'
    )
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260814120000_AddVendorProductVariationToOrderItems', N'8.0.0');
END;
