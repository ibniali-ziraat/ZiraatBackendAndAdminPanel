IF COL_LENGTH(N'dbo.Core_VendorProduct', N'ExpiryDate') IS NULL
BEGIN
    ALTER TABLE [dbo].[Core_VendorProduct]
    ADD [ExpiryDate] DATETIME2 NULL;
END
GO
