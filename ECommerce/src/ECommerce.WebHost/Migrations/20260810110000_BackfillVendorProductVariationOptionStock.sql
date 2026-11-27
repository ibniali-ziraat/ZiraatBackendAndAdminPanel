IF OBJECT_ID(N'[dbo].[Core_VendorProductVariationOption]', N'U') IS NOT NULL
    AND OBJECT_ID(N'[dbo].[Core_VendorProductVariation]', N'U') IS NOT NULL
    AND COL_LENGTH(N'[dbo].[Core_VendorProductVariationOption]', N'StockQuantity') IS NOT NULL
BEGIN
    UPDATE variationOption
    SET [StockQuantity] = variation.[StockQuantity]
    FROM [dbo].[Core_VendorProductVariationOption] variationOption
    INNER JOIN [dbo].[Core_VendorProductVariation] variation
        ON variation.[Id] = variationOption.[VendorProductVariationId]
    WHERE variationOption.[StockQuantity] = 0
        AND variation.[StockQuantity] > 0;
END;
