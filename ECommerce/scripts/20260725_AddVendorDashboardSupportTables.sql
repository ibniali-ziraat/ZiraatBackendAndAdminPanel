/* Vendor dashboard support tables
   Safe to run multiple times on SQL Server.
   Covers tables used by VendorDashboardController / VendorDashboardService.
*/

IF OBJECT_ID(N'[dbo].[Core_VendorProduct]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Core_VendorProduct] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [VendorId] BIGINT NOT NULL,
        [ProductId] BIGINT NOT NULL,
        [ProductName] NVARCHAR(MAX) NULL,
        [ProductSku] NVARCHAR(MAX) NULL,
        [BatchNumber] NVARCHAR(MAX) NULL,
        [ManufacturingDate] DATETIME2 NULL,
        [MaximumRetailPrice] DECIMAL(18,2) NULL,
        [AssignedPrice] DECIMAL(18,2) NULL,
        [OriginalPrice] DECIMAL(18,2) NULL,
        [DiscountPrice] DECIMAL(18,2) NULL,
        [TaxRate] DECIMAL(18,2) NULL,
        [NetSalePriceForZiraat] DECIMAL(18,2) NULL,
        [CreditSaleHikePercentage] DECIMAL(18,2) NULL,
        [SalePriceForCreditor] DECIMAL(18,2) NULL,
        [IsAvailableForCreditBuyers] BIT NOT NULL CONSTRAINT [DF_Core_VendorProduct_IsAvailableForCreditBuyers] DEFAULT (0),
        [StockQuantity] INT NOT NULL CONSTRAINT [DF_Core_VendorProduct_StockQuantity] DEFAULT (0),
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Core_VendorProduct_IsActive] DEFAULT (1),
        [CreatedOn] DATETIMEOFFSET NOT NULL CONSTRAINT [DF_Core_VendorProduct_CreatedOn] DEFAULT (SYSDATETIMEOFFSET()),
        [LatestUpdatedOn] DATETIMEOFFSET NOT NULL CONSTRAINT [DF_Core_VendorProduct_LatestUpdatedOn] DEFAULT (SYSDATETIMEOFFSET()),
        [CreatedById] BIGINT NULL,
        [LatestUpdatedById] BIGINT NULL,
        CONSTRAINT [PK_Core_VendorProduct] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Core_VendorProduct_Core_Vendor_VendorId')
BEGIN
    ALTER TABLE [dbo].[Core_VendorProduct]
    ADD CONSTRAINT [FK_Core_VendorProduct_Core_Vendor_VendorId]
        FOREIGN KEY ([VendorId]) REFERENCES [dbo].[Core_Vendor] ([Id]) ON DELETE NO ACTION;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Core_VendorProduct_Catalog_Product_ProductId')
BEGIN
    ALTER TABLE [dbo].[Core_VendorProduct]
    ADD CONSTRAINT [FK_Core_VendorProduct_Catalog_Product_ProductId]
        FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Catalog_Product] ([Id]) ON DELETE NO ACTION;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Core_VendorProduct_Core_User_CreatedById')
BEGIN
    ALTER TABLE [dbo].[Core_VendorProduct]
    ADD CONSTRAINT [FK_Core_VendorProduct_Core_User_CreatedById]
        FOREIGN KEY ([CreatedById]) REFERENCES [dbo].[Core_User] ([Id]) ON DELETE NO ACTION;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Core_VendorProduct_Core_User_LatestUpdatedById')
BEGIN
    ALTER TABLE [dbo].[Core_VendorProduct]
    ADD CONSTRAINT [FK_Core_VendorProduct_Core_User_LatestUpdatedById]
        FOREIGN KEY ([LatestUpdatedById]) REFERENCES [dbo].[Core_User] ([Id]) ON DELETE NO ACTION;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Core_VendorProduct_VendorId_ProductId_IsActive' AND [object_id] = OBJECT_ID(N'[dbo].[Core_VendorProduct]'))
BEGIN
    CREATE INDEX [IX_Core_VendorProduct_VendorId_ProductId_IsActive]
    ON [dbo].[Core_VendorProduct] ([VendorId], [ProductId], [IsActive]);
END
GO

IF OBJECT_ID(N'[dbo].[Core_VendorCustomerAssociation]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Core_VendorCustomerAssociation] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [VendorId] BIGINT NOT NULL,
        [CustomerId] BIGINT NOT NULL,
        [CustomerName] NVARCHAR(MAX) NULL,
        [CustomerEmail] NVARCHAR(MAX) NULL,
        [CustomerPhoneNumber] NVARCHAR(MAX) NULL,
        [AllowCredit] BIT NOT NULL CONSTRAINT [DF_Core_VendorCustomerAssociation_AllowCredit] DEFAULT (0),
        [MaxCreditAmount] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_Core_VendorCustomerAssociation_MaxCreditAmount] DEFAULT (0),
        [CurrentBalance] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_Core_VendorCustomerAssociation_CurrentBalance] DEFAULT (0),
        [StartDate] DATETIME2 NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Core_VendorCustomerAssociation_IsActive] DEFAULT (1),
        [CreatedOn] DATETIMEOFFSET NOT NULL CONSTRAINT [DF_Core_VendorCustomerAssociation_CreatedOn] DEFAULT (SYSDATETIMEOFFSET()),
        [CreatedById] BIGINT NOT NULL,
        [LatestUpdatedOn] DATETIMEOFFSET NOT NULL CONSTRAINT [DF_Core_VendorCustomerAssociation_LatestUpdatedOn] DEFAULT (SYSDATETIMEOFFSET()),
        [LatestUpdatedById] BIGINT NOT NULL,
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_Core_VendorCustomerAssociation_IsDeleted] DEFAULT (0),
        CONSTRAINT [PK_Core_VendorCustomerAssociation] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Core_VendorCustomerAssociation_Core_Vendor_VendorId')
BEGIN
    ALTER TABLE [dbo].[Core_VendorCustomerAssociation]
    ADD CONSTRAINT [FK_Core_VendorCustomerAssociation_Core_Vendor_VendorId]
        FOREIGN KEY ([VendorId]) REFERENCES [dbo].[Core_Vendor] ([Id]) ON DELETE NO ACTION;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Core_VendorCustomerAssociation_Core_User_CustomerId')
BEGIN
    ALTER TABLE [dbo].[Core_VendorCustomerAssociation]
    ADD CONSTRAINT [FK_Core_VendorCustomerAssociation_Core_User_CustomerId]
        FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Core_User] ([Id]) ON DELETE NO ACTION;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Core_VendorCustomerAssociation_VendorId_CustomerId_IsDeleted' AND [object_id] = OBJECT_ID(N'[dbo].[Core_VendorCustomerAssociation]'))
BEGIN
    CREATE INDEX [IX_Core_VendorCustomerAssociation_VendorId_CustomerId_IsDeleted]
    ON [dbo].[Core_VendorCustomerAssociation] ([VendorId], [CustomerId], [IsDeleted]);
END
GO

IF OBJECT_ID(N'[dbo].[Core_CustomerSettlement]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Core_CustomerSettlement] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [VendorId] BIGINT NOT NULL,
        [VendorCustomerAssociationId] BIGINT NOT NULL,
        [CustomerId] BIGINT NOT NULL,
        [AmountPaid] DECIMAL(18,2) NOT NULL,
        [PaymentMethod] NVARCHAR(MAX) NULL,
        [TransactionId] NVARCHAR(MAX) NULL,
        [BalanceAfter] DECIMAL(18,2) NOT NULL,
        [SettlementDate] DATETIME2 NOT NULL,
        [SettlementReminderDate] DATETIME2 NULL,
        [Note] NVARCHAR(MAX) NULL,
        [CreatedOn] DATETIMEOFFSET NOT NULL CONSTRAINT [DF_Core_CustomerSettlement_CreatedOn] DEFAULT (SYSDATETIMEOFFSET()),
        [CreatedById] BIGINT NOT NULL,
        [LatestUpdatedOn] DATETIMEOFFSET NOT NULL CONSTRAINT [DF_Core_CustomerSettlement_LatestUpdatedOn] DEFAULT (SYSDATETIMEOFFSET()),
        [LatestUpdatedById] BIGINT NOT NULL,
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_Core_CustomerSettlement_IsDeleted] DEFAULT (0),
        CONSTRAINT [PK_Core_CustomerSettlement] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Core_CustomerSettlement_Core_Vendor_VendorId')
BEGIN
    ALTER TABLE [dbo].[Core_CustomerSettlement]
    ADD CONSTRAINT [FK_Core_CustomerSettlement_Core_Vendor_VendorId]
        FOREIGN KEY ([VendorId]) REFERENCES [dbo].[Core_Vendor] ([Id]) ON DELETE NO ACTION;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Core_CustomerSettlement_Core_VendorCustomerAssociation_VendorCustomerAssociationId')
BEGIN
    ALTER TABLE [dbo].[Core_CustomerSettlement]
    ADD CONSTRAINT [FK_Core_CustomerSettlement_Core_VendorCustomerAssociation_VendorCustomerAssociationId]
        FOREIGN KEY ([VendorCustomerAssociationId]) REFERENCES [dbo].[Core_VendorCustomerAssociation] ([Id]) ON DELETE NO ACTION;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Core_CustomerSettlement_Core_User_CreatedById')
BEGIN
    ALTER TABLE [dbo].[Core_CustomerSettlement]
    ADD CONSTRAINT [FK_Core_CustomerSettlement_Core_User_CreatedById]
        FOREIGN KEY ([CreatedById]) REFERENCES [dbo].[Core_User] ([Id]) ON DELETE NO ACTION;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Core_CustomerSettlement_Core_User_LatestUpdatedById')
BEGIN
    ALTER TABLE [dbo].[Core_CustomerSettlement]
    ADD CONSTRAINT [FK_Core_CustomerSettlement_Core_User_LatestUpdatedById]
        FOREIGN KEY ([LatestUpdatedById]) REFERENCES [dbo].[Core_User] ([Id]) ON DELETE NO ACTION;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Core_CustomerSettlement_VendorId_CustomerId_IsDeleted' AND [object_id] = OBJECT_ID(N'[dbo].[Core_CustomerSettlement]'))
BEGIN
    CREATE INDEX [IX_Core_CustomerSettlement_VendorId_CustomerId_IsDeleted]
    ON [dbo].[Core_CustomerSettlement] ([VendorId], [CustomerId], [IsDeleted]);
END
GO

IF OBJECT_ID(N'[dbo].[Core_VendorOffer]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Core_VendorOffer] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [VendorId] BIGINT NOT NULL,
        [OfferType] NVARCHAR(100) NOT NULL,
        [Title] NVARCHAR(450) NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [TermsAndConditions] NVARCHAR(MAX) NULL,
        [DiscountType] NVARCHAR(50) NULL,
        [DiscountValue] DECIMAL(18,2) NOT NULL,
        [MaxDiscountAmount] DECIMAL(18,2) NULL,
        [MinimumOrderAmount] DECIMAL(18,2) NULL,
        [MinimumQuantity] INT NULL,
        [CouponCode] NVARCHAR(100) NULL,
        [StartOn] DATETIMEOFFSET NULL,
        [EndOn] DATETIMEOFFSET NULL,
        [IsActive] BIT NOT NULL,
        [IsDeleted] BIT NOT NULL,
        [CreatedOn] DATETIMEOFFSET NOT NULL,
        [CreatedById] BIGINT NOT NULL,
        [LatestUpdatedOn] DATETIMEOFFSET NOT NULL,
        [LatestUpdatedById] BIGINT NOT NULL,
        CONSTRAINT [PK_Core_VendorOffer] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Core_VendorOffer_Core_Vendor_VendorId')
BEGIN
    ALTER TABLE [dbo].[Core_VendorOffer]
    ADD CONSTRAINT [FK_Core_VendorOffer_Core_Vendor_VendorId]
        FOREIGN KEY ([VendorId]) REFERENCES [dbo].[Core_Vendor] ([Id]) ON DELETE NO ACTION;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Core_VendorOffer_Core_User_CreatedById')
BEGIN
    ALTER TABLE [dbo].[Core_VendorOffer]
    ADD CONSTRAINT [FK_Core_VendorOffer_Core_User_CreatedById]
        FOREIGN KEY ([CreatedById]) REFERENCES [dbo].[Core_User] ([Id]) ON DELETE NO ACTION;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Core_VendorOffer_Core_User_LatestUpdatedById')
BEGIN
    ALTER TABLE [dbo].[Core_VendorOffer]
    ADD CONSTRAINT [FK_Core_VendorOffer_Core_User_LatestUpdatedById]
        FOREIGN KEY ([LatestUpdatedById]) REFERENCES [dbo].[Core_User] ([Id]) ON DELETE NO ACTION;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Core_VendorOffer_VendorId_OfferType_IsDeleted' AND [object_id] = OBJECT_ID(N'[dbo].[Core_VendorOffer]'))
BEGIN
    CREATE INDEX [IX_Core_VendorOffer_VendorId_OfferType_IsDeleted]
    ON [dbo].[Core_VendorOffer] ([VendorId], [OfferType], [IsDeleted]);
END
GO

IF COL_LENGTH(N'[dbo].[Orders_OrderItem]', N'VendorId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Orders_OrderItem] ADD [VendorId] BIGINT NULL;
END
GO

IF COL_LENGTH(N'[dbo].[Orders_OrderItem]', N'VendorProductId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Orders_OrderItem] ADD [VendorProductId] BIGINT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Orders_OrderItem_VendorId' AND [object_id] = OBJECT_ID(N'[dbo].[Orders_OrderItem]'))
BEGIN
    CREATE INDEX [IX_Orders_OrderItem_VendorId]
    ON [dbo].[Orders_OrderItem] ([VendorId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Orders_OrderItem_VendorProductId' AND [object_id] = OBJECT_ID(N'[dbo].[Orders_OrderItem]'))
BEGIN
    CREATE INDEX [IX_Orders_OrderItem_VendorProductId]
    ON [dbo].[Orders_OrderItem] ([VendorProductId]);
END
GO
