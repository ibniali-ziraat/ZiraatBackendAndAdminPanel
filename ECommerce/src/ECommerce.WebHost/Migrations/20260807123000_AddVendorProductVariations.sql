IF OBJECT_ID(N'[dbo].[Core_VendorProductVariation]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Core_VendorProductVariation] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [VendorProductId] BIGINT NOT NULL,
        [Name] NVARCHAR(450) NULL,
        [NormalizedName] NVARCHAR(450) NULL,
        [Sku] NVARCHAR(450) NULL,
        [Gtin] NVARCHAR(450) NULL,
        [MaximumRetailPrice] DECIMAL(18,2) NULL,
        [AssignedPrice] DECIMAL(18,2) NULL,
        [OriginalPrice] DECIMAL(18,2) NULL,
        [DiscountPrice] DECIMAL(18,2) NULL,
        [TaxRate] DECIMAL(18,2) NULL,
        [NetSalePriceForZiraat] DECIMAL(18,2) NULL,
        [CreditSaleHikePercentage] DECIMAL(18,2) NULL,
        [SalePriceForCreditor] DECIMAL(18,2) NULL,
        [IsAvailableForCreditBuyers] BIT NOT NULL,
        [StockQuantity] INT NOT NULL,
        [IsActive] BIT NOT NULL,
        [SortIndex] INT NOT NULL,
        [CreatedOn] DATETIMEOFFSET NOT NULL,
        [LatestUpdatedOn] DATETIMEOFFSET NOT NULL,
        CONSTRAINT [PK_Core_VendorProductVariation] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Core_VendorProductVariation_Core_VendorProduct_VendorProductId]
            FOREIGN KEY ([VendorProductId]) REFERENCES [dbo].[Core_VendorProduct] ([Id]) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'[dbo].[Core_VendorProductVariationOption]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Core_VendorProductVariationOption] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [VendorProductVariationId] BIGINT NOT NULL,
        [OptionId] BIGINT NOT NULL,
        [OptionName] NVARCHAR(450) NULL,
        [Value] NVARCHAR(450) NULL,
        [StockQuantity] INT NOT NULL CONSTRAINT [DF_Core_VendorProductVariationOption_StockQuantity] DEFAULT (0),
        [SortIndex] INT NOT NULL,
        CONSTRAINT [PK_Core_VendorProductVariationOption] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Core_VendorProductVariationOption_Core_VendorProductVariation_VendorProductVariationId]
            FOREIGN KEY ([VendorProductVariationId]) REFERENCES [dbo].[Core_VendorProductVariation] ([Id]) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'[dbo].[Core_VendorProductVariationOption]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[Core_VendorProductVariationOption]', N'StockQuantity') IS NULL
BEGIN
    ALTER TABLE [dbo].[Core_VendorProductVariationOption]
        ADD [StockQuantity] INT NOT NULL
            CONSTRAINT [DF_Core_VendorProductVariationOption_StockQuantity] DEFAULT (0);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_Core_VendorProduct_VendorId_ProductId'
        AND [object_id] = OBJECT_ID(N'[dbo].[Core_VendorProduct]')
)
BEGIN
    CREATE INDEX [IX_Core_VendorProduct_VendorId_ProductId]
        ON [dbo].[Core_VendorProduct] ([VendorId], [ProductId]);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_Core_VendorProductVariation_VendorProductId_Sku'
        AND [object_id] = OBJECT_ID(N'[dbo].[Core_VendorProductVariation]')
)
BEGIN
    CREATE INDEX [IX_Core_VendorProductVariation_VendorProductId_Sku]
        ON [dbo].[Core_VendorProductVariation] ([VendorProductId], [Sku]);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_Core_VendorProductVariationOption_VendorProductVariationId_OptionId_Value'
        AND [object_id] = OBJECT_ID(N'[dbo].[Core_VendorProductVariationOption]')
)
BEGIN
    CREATE INDEX [IX_Core_VendorProductVariationOption_VendorProductVariationId_OptionId_Value]
        ON [dbo].[Core_VendorProductVariationOption] ([VendorProductVariationId], [OptionId], [Value]);
END;

IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NOT NULL
    AND NOT EXISTS (
        SELECT 1
        FROM [dbo].[__EFMigrationsHistory]
        WHERE [MigrationId] = N'20260807123000_AddVendorProductVariations'
    )
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260807123000_AddVendorProductVariations', N'8.0.0');
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
