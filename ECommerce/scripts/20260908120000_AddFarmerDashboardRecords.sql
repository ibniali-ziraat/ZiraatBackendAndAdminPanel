-- Farmer dashboard records: SQL Server deployment script.
-- Select the existing API database in SSMS before running the complete script.
-- Equivalent to EF migration 20260908120000_AddFarmerDashboardRecords (EF Core 8.0.0).
-- Does not insert sample data, drop tables, or modify existing orchard records.
-- Run once via this script OR via EF migrations. Successful reruns are skipped.
-- If these tables already exist without the migration-history entry, this script
-- stops for schema reconciliation instead of assuming they match the migration.

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF @@TRANCOUNT <> 0
    THROW 51000, 'Run this script outside an existing transaction.', 1;

IF OBJECT_ID(N'[dbo].[Core_OrchardProfile]', N'U') IS NULL
    THROW 51001, 'Core_OrchardProfile is missing. Select the API database and apply its existing orchard schema first.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[__EFMigrationsHistory]
        (
            [MigrationId] nvarchar(150) NOT NULL,
            [ProductVersion] nvarchar(32) NOT NULL,
            CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
        );
    END;

    IF EXISTS
    (
        SELECT 1 FROM [dbo].[__EFMigrationsHistory]
        WHERE [MigrationId] = N'20260908120000_AddFarmerDashboardRecords'
    )
    BEGIN
        IF OBJECT_ID(N'[dbo].[Core_FarmActivity]', N'U') IS NULL
            OR OBJECT_ID(N'[dbo].[Core_FarmAssessment]', N'U') IS NULL
            THROW 51002, 'Migration history says the dashboard migration is applied, but a dashboard table is missing. Reconcile the database schema.', 1;

        PRINT N'Farmer dashboard migration is already applied; no changes made.';
    END
    ELSE
    BEGIN
        IF OBJECT_ID(N'[dbo].[Core_FarmActivity]', N'U') IS NOT NULL
            OR OBJECT_ID(N'[dbo].[Core_FarmAssessment]', N'U') IS NOT NULL
            THROW 51003, 'Dashboard tables exist without the matching migration-history entry. Compare their schema with the migration before proceeding.', 1;

        CREATE TABLE [dbo].[Core_FarmActivity]
        (
            [Id] bigint IDENTITY(1,1) NOT NULL,
            [OrchardProfileId] bigint NOT NULL,
            [RecordedById] bigint NOT NULL,
            [Kind] nvarchar(32) NOT NULL,
            [OccurredOn] datetimeoffset NOT NULL,
            [ScheduledFor] datetimeoffset NULL,
            [IsCompleted] bit NOT NULL,
            [Quantity] decimal(18,4) NULL,
            [Amount] decimal(18,2) NULL,
            [ExpenseCategory] nvarchar(32) NULL,
            [Notes] nvarchar(2000) NULL,
            [CreatedOn] datetimeoffset NOT NULL,
            [UpdatedOn] datetimeoffset NOT NULL,
            CONSTRAINT [PK_Core_FarmActivity] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_Core_FarmActivity_Core_OrchardProfile_OrchardProfileId]
                FOREIGN KEY ([OrchardProfileId]) REFERENCES [dbo].[Core_OrchardProfile] ([Id])
                ON DELETE CASCADE
        );

        CREATE INDEX [IX_Core_FarmActivity_OrchardProfileId_OccurredOn]
            ON [dbo].[Core_FarmActivity] ([OrchardProfileId], [OccurredOn]);

        CREATE TABLE [dbo].[Core_FarmAssessment]
        (
            [Id] bigint IDENTITY(1,1) NOT NULL,
            [OrchardProfileId] bigint NOT NULL,
            [RecordedById] bigint NOT NULL,
            [ObservedOn] datetimeoffset NOT NULL,
            [TreesAssessed] int NULL,
            [LivingTrees] int NULL,
            [TreesMonitored] int NULL,
            [PestAffectedTrees] int NULL,
            [DiseaseAffectedTrees] int NULL,
            [DeficiencySymptomsCount] int NULL,
            [AverageTreeAgeYears] decimal(18,4) NULL,
            [YieldKgPerTree] decimal(18,4) NULL,
            [YieldKgPerHectare] decimal(18,4) NULL,
            [SoilPh] decimal(18,4) NULL,
            [SoilEc] decimal(18,4) NULL,
            [OrganicMatterPercent] decimal(18,4) NULL,
            [RiskLevel] nvarchar(16) NULL,
            [NitrogenLevel] nvarchar(16) NULL,
            [PhosphorusLevel] nvarchar(16) NULL,
            [PotassiumLevel] nvarchar(16) NULL,
            [PesticideResistanceSuspected] bit NULL,
            [Notes] nvarchar(2000) NULL,
            [CreatedOn] datetimeoffset NOT NULL,
            [UpdatedOn] datetimeoffset NOT NULL,
            CONSTRAINT [PK_Core_FarmAssessment] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_Core_FarmAssessment_Core_OrchardProfile_OrchardProfileId]
                FOREIGN KEY ([OrchardProfileId]) REFERENCES [dbo].[Core_OrchardProfile] ([Id])
                ON DELETE CASCADE
        );

        CREATE INDEX [IX_Core_FarmAssessment_OrchardProfileId_ObservedOn]
            ON [dbo].[Core_FarmAssessment] ([OrchardProfileId], [ObservedOn]);

        INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
        VALUES (N'20260908120000_AddFarmerDashboardRecords', N'8.0.0');

        PRINT N'Farmer dashboard tables, indexes, foreign keys and migration-history entry created.';
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT DB_NAME() AS [DatabaseName], [MigrationId], [ProductVersion]
FROM [dbo].[__EFMigrationsHistory]
WHERE [MigrationId] = N'20260908120000_AddFarmerDashboardRecords';
