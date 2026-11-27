-- Apply after 20260929120000_AddFarmWorkflowFoundation in backed-up staging.
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId]=N'20260929160000_AddPhase3FarmOperations')
 BEGIN
  ALTER TABLE [Core_FarmOperation] ADD
   [OrchardProfileId] bigint NOT NULL CONSTRAINT [DF_Core_FarmOperation_OrchardProfileId] DEFAULT 0,
   [FarmerId] bigint NOT NULL CONSTRAINT [DF_Core_FarmOperation_FarmerId] DEFAULT 0,
   [Status] nvarchar(16) NOT NULL CONSTRAINT [DF_Core_FarmOperation_Status] DEFAULT N'posted',
   [CoverageValue] decimal(18,4) NOT NULL CONSTRAINT [DF_Core_FarmOperation_CoverageValue] DEFAULT 0,
   [CoverageUnit] nvarchar(16) NOT NULL CONSTRAINT [DF_Core_FarmOperation_CoverageUnit] DEFAULT N'ha',
   [LabourHours] decimal(18,4) NULL, [NumberOfLabourers] int NULL, [EquipmentJson] nvarchar(max) NULL,
   [EquipmentHours] decimal(18,4) NULL, [Cost] decimal(18,2) NULL, [Currency] nvarchar(3) NULL,
   [MaterialName] nvarchar(200) NULL, [MaterialQuantity] decimal(18,4) NULL, [MaterialUnit] nvarchar(16) NULL,
   [Target] nvarchar(200) NULL, [DoseValue] decimal(18,4) NULL, [DoseUnit] nvarchar(16) NULL, [MixtureTotalLitres] decimal(18,4) NULL,
   [IrrigationTotalLitres] decimal(18,4) NULL, [PruningType] nvarchar(32) NULL, [HarvestQuantityKg] decimal(18,4) NULL,
   [Notes] nvarchar(2000) NULL, [UpdatedOn] datetimeoffset NOT NULL CONSTRAINT [DF_Core_FarmOperation_UpdatedOn] DEFAULT SYSUTCDATETIME(),
   [VoidedOn] datetimeoffset NULL, [VoidedById] bigint NULL, [VoidReason] nvarchar(1000) NULL;
  CREATE INDEX [IX_Core_FarmOperation_OrchardProfileId_OperationDate] ON [Core_FarmOperation]([OrchardProfileId],[OperationDate]);
  CREATE INDEX [IX_Core_FarmOperation_FarmerId_Status] ON [Core_FarmOperation]([FarmerId],[Status]);
  CREATE TABLE [Core_FarmHarvestSaleLot]([Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Core_FarmHarvestSaleLot] PRIMARY KEY,
   [OperationId] uniqueidentifier NOT NULL, [QuantityKg] decimal(18,4) NOT NULL, [PricePerKg] decimal(18,4) NULL,
   [Income] decimal(18,2) NULL, [IsSold] bit NOT NULL, [BuyerOrMandi] nvarchar(200) NULL, [CreatedOn] datetimeoffset NOT NULL,
   CONSTRAINT [FK_Core_FarmHarvestSaleLot_Core_FarmOperation_OperationId] FOREIGN KEY([OperationId]) REFERENCES [Core_FarmOperation]([Id]));
  CREATE INDEX [IX_Core_FarmHarvestSaleLot_OperationId] ON [Core_FarmHarvestSaleLot]([OperationId]);
  INSERT INTO [__EFMigrationsHistory]([MigrationId],[ProductVersion]) VALUES(N'20260929160000_AddPhase3FarmOperations',N'8.0.0');
 END;
 COMMIT;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 THROW;
END CATCH;
