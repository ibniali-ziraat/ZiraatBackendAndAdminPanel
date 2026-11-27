IF OBJECT_ID(N'[dbo].[ShoppingCart_CartItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[ShoppingCart_CartItem]', N'VendorProductId') IS NULL
BEGIN
    ALTER TABLE [dbo].[ShoppingCart_CartItem]
        ADD [VendorProductId] BIGINT NULL;
END;

IF OBJECT_ID(N'[dbo].[ShoppingCart_CartItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[ShoppingCart_CartItem]', N'VendorProductVariationId') IS NULL
BEGIN
    ALTER TABLE [dbo].[ShoppingCart_CartItem]
        ADD [VendorProductVariationId] BIGINT NULL;
END;

IF OBJECT_ID(N'[dbo].[Checkouts_CheckoutItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[Checkouts_CheckoutItem]', N'VendorId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Checkouts_CheckoutItem]
        ADD [VendorId] BIGINT NULL;
END;

IF OBJECT_ID(N'[dbo].[Checkouts_CheckoutItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[Checkouts_CheckoutItem]', N'VendorProductId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Checkouts_CheckoutItem]
        ADD [VendorProductId] BIGINT NULL;
END;

IF OBJECT_ID(N'[dbo].[Checkouts_CheckoutItem]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[Checkouts_CheckoutItem]', N'VendorProductVariationId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Checkouts_CheckoutItem]
        ADD [VendorProductVariationId] BIGINT NULL;
END;

IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NOT NULL
    AND NOT EXISTS (
        SELECT 1
        FROM [dbo].[__EFMigrationsHistory]
        WHERE [MigrationId] = N'20260812120000_AddVendorProductVariationToCartAndCheckoutItems'
    )
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812120000_AddVendorProductVariationToCartAndCheckoutItems', N'8.0.0');
END;
