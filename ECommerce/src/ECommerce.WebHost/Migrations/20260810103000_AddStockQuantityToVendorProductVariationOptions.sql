IF OBJECT_ID(N'[dbo].[Core_VendorProductVariationOption]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[Core_VendorProductVariationOption]', N'StockQuantity') IS NULL
BEGIN
    ALTER TABLE [dbo].[Core_VendorProductVariationOption]
        ADD [StockQuantity] INT NOT NULL
            CONSTRAINT [DF_Core_VendorProductVariationOption_StockQuantity] DEFAULT (0);
END;

IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NOT NULL
    AND NOT EXISTS (
        SELECT 1
        FROM [dbo].[__EFMigrationsHistory]
        WHERE [MigrationId] = N'20260810103000_AddStockQuantityToVendorProductVariationOptions'
    )
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810103000_AddStockQuantityToVendorProductVariationOptions', N'8.0.0');
END;
