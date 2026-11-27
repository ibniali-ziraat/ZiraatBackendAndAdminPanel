-- Phase 1. Review and run against a backed-up staging database first.
-- Do not run concurrently with EF migrations. New delivery worker must remain disabled.
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'[__EFMigrationsHistory]', N'U') IS NULL
        THROW 51000, 'Apply the existing API migrations before this script.', 1;
    IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260929120000_AddFarmWorkflowFoundation')
    BEGIN
CREATE TABLE [Core_FarmAdvisory] (
    [Id] uniqueidentifier NOT NULL,
    [Version] uniqueidentifier NOT NULL,
    [Kind] nvarchar(32) NOT NULL,
    [Title] nvarchar(160) NOT NULL,
    [Recommendation] nvarchar(4000) NOT NULL,
    [Crop] nvarchar(100) NULL,
    [Variety] nvarchar(100) NULL,
    [GrowthStage] nvarchar(100) NULL,
    [TargetOrchardsJson] nvarchar(max) NOT NULL,
    [Status] nvarchar(16) NOT NULL,
    [WindowStart] datetimeoffset NOT NULL,
    [WindowEnd] datetimeoffset NOT NULL,
    [CreatedOn] datetimeoffset NOT NULL,
    [PublishedOn] datetimeoffset NULL,
    [CancelledOn] datetimeoffset NULL,
    [CreatedById] bigint NOT NULL,
    [CancellationReason] nvarchar(1000) NULL,
    CONSTRAINT [PK_Core_FarmAdvisory] PRIMARY KEY ([Id])
);
CREATE INDEX [IX_Core_FarmAdvisory_Status_CreatedOn] ON [Core_FarmAdvisory] ([Status], [CreatedOn]);

CREATE TABLE [Core_FarmAdvisoryRecipient] (
    [Id] uniqueidentifier NOT NULL,
    [AdvisoryId] uniqueidentifier NOT NULL,
    [OrchardProfileId] bigint NOT NULL,
    [FarmerId] bigint NOT NULL,
    [OrchardName] nvarchar(500) NULL,
    [Version] uniqueidentifier NOT NULL,
    [ReadOn] datetimeoffset NULL,
    [AcknowledgedOn] datetimeoffset NULL,
    [CompletedOn] datetimeoffset NULL,
    CONSTRAINT [PK_Core_FarmAdvisoryRecipient] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Core_FarmAdvisoryRecipient_Core_FarmAdvisory_AdvisoryId] FOREIGN KEY ([AdvisoryId]) REFERENCES [Core_FarmAdvisory] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Core_FarmAdvisoryRecipient_Core_OrchardProfile_OrchardProfileId] FOREIGN KEY ([OrchardProfileId]) REFERENCES [Core_OrchardProfile] ([Id]) ON DELETE NO ACTION
);
CREATE UNIQUE INDEX [IX_Core_FarmAdvisoryRecipient_AdvisoryId_OrchardProfileId] ON [Core_FarmAdvisoryRecipient] ([AdvisoryId], [OrchardProfileId]);
CREATE INDEX [IX_Core_FarmAdvisoryRecipient_FarmerId_OrchardProfileId] ON [Core_FarmAdvisoryRecipient] ([FarmerId], [OrchardProfileId]);
CREATE INDEX [IX_Core_FarmAdvisoryRecipient_OrchardProfileId] ON [Core_FarmAdvisoryRecipient] ([OrchardProfileId]);

CREATE TABLE [Core_FarmAcknowledgment] (
    [Id] uniqueidentifier NOT NULL,
    [RecipientId] uniqueidentifier NOT NULL,
    [ActorId] bigint NOT NULL,
    [CreatedOn] datetimeoffset NOT NULL,
    [OpenOperationForm] bit NOT NULL,
    CONSTRAINT [PK_Core_FarmAcknowledgment] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Core_FarmAcknowledgment_Core_FarmAdvisoryRecipient_RecipientId] FOREIGN KEY ([RecipientId]) REFERENCES [Core_FarmAdvisoryRecipient] ([Id]) ON DELETE NO ACTION
);
CREATE UNIQUE INDEX [IX_Core_FarmAcknowledgment_RecipientId] ON [Core_FarmAcknowledgment] ([RecipientId]);

CREATE TABLE [Core_FarmOperation] (
    [Id] uniqueidentifier NOT NULL,
    [RecipientId] uniqueidentifier NOT NULL,
    [Version] uniqueidentifier NOT NULL,
    [RecordedById] bigint NOT NULL,
    [Kind] nvarchar(32) NOT NULL,
    [OperationDate] date NOT NULL,
    [CreatedOn] datetimeoffset NOT NULL,
    [CompletedOn] datetimeoffset NULL,
    CONSTRAINT [PK_Core_FarmOperation] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Core_FarmOperation_Core_FarmAdvisoryRecipient_RecipientId] FOREIGN KEY ([RecipientId]) REFERENCES [Core_FarmAdvisoryRecipient] ([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_Core_FarmOperation_RecipientId] ON [Core_FarmOperation] ([RecipientId]);

CREATE TABLE [Core_FarmWorkflowAudit] (
    [Id] uniqueidentifier NOT NULL,
    [SubjectId] uniqueidentifier NOT NULL,
    [ActorId] bigint NOT NULL,
    [Action] nvarchar(32) NOT NULL,
    [Detail] nvarchar(4000) NULL,
    [CreatedOn] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Core_FarmWorkflowAudit] PRIMARY KEY ([Id])
);
CREATE INDEX [IX_Core_FarmWorkflowAudit_SubjectId_CreatedOn] ON [Core_FarmWorkflowAudit] ([SubjectId], [CreatedOn]);

CREATE TABLE [Core_FarmWorkflowRequest] (
    [Id] uniqueidentifier NOT NULL,
    [ActorId] bigint NOT NULL,
    [Scope] nvarchar(100) NOT NULL,
    [Key] nvarchar(100) NOT NULL,
    [BodyHash] nvarchar(64) NOT NULL,
    [ResponseJson] nvarchar(max) NOT NULL,
    [CreatedOn] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Core_FarmWorkflowRequest] PRIMARY KEY ([Id])
);
CREATE UNIQUE INDEX [IX_Core_FarmWorkflowRequest_ActorId_Scope_Key] ON [Core_FarmWorkflowRequest] ([ActorId], [Scope], [Key]);

CREATE TABLE [Core_FarmDeliveryEvent] (
    [Id] uniqueidentifier NOT NULL,
    [RecipientId] uniqueidentifier NOT NULL,
    [Kind] nvarchar(32) NOT NULL,
    [Version] uniqueidentifier NOT NULL,
    [CreatedOn] datetimeoffset NOT NULL,
    [NextAttemptOn] datetimeoffset NOT NULL,
    [DeliveredOn] datetimeoffset NULL,
    [LeaseUntil] datetimeoffset NULL,
    [Attempts] int NOT NULL,
    [LastError] nvarchar(500) NULL,
    [Outcome] nvarchar(100) NULL,
    CONSTRAINT [PK_Core_FarmDeliveryEvent] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Core_FarmDeliveryEvent_Core_FarmAdvisoryRecipient_RecipientId] FOREIGN KEY ([RecipientId]) REFERENCES [Core_FarmAdvisoryRecipient] ([Id]) ON DELETE NO ACTION
);
CREATE UNIQUE INDEX [IX_Core_FarmDeliveryEvent_RecipientId_Kind] ON [Core_FarmDeliveryEvent] ([RecipientId], [Kind]);
CREATE INDEX [IX_Core_FarmDeliveryEvent_DeliveredOn_NextAttemptOn] ON [Core_FarmDeliveryEvent] ([DeliveredOn], [NextAttemptOn]);
        INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
        VALUES (N'20260929120000_AddFarmWorkflowFoundation', N'8.0.0');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
