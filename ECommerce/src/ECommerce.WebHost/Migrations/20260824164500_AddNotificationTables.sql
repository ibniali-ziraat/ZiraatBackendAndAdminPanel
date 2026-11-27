IF OBJECT_ID(N'[dbo].[Notifications_NotificationScheme]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Notifications_NotificationScheme] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [NotificationName] NVARCHAR(96) NOT NULL,
        [Data] NVARCHAR(MAX) NULL,
        [DataTypeName] NVARCHAR(512) NULL,
        [EntityTypeName] NVARCHAR(250) NULL,
        [EntityTypeAssemblyQualifiedName] NVARCHAR(512) NULL,
        [EntityId] NVARCHAR(96) NULL,
        [Severity] INT NOT NULL,
        [UserIds] NVARCHAR(MAX) NULL,
        [ExcludedUserIds] NVARCHAR(MAX) NULL,
        [CreatedOn] DATETIMEOFFSET NOT NULL,
        [IsDeleted] BIT NOT NULL,
        CONSTRAINT [PK_Notifications_NotificationScheme] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[dbo].[Notifications_NotificationDetail]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Notifications_NotificationDetail] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [NotificationName] NVARCHAR(96) NOT NULL,
        [Data] NVARCHAR(MAX) NULL,
        [DataTypeName] NVARCHAR(512) NULL,
        [EntityTypeName] NVARCHAR(250) NULL,
        [EntityTypeAssemblyQualifiedName] NVARCHAR(512) NULL,
        [EntityId] NVARCHAR(96) NULL,
        [Severity] INT NOT NULL,
        [CreatedOn] DATETIMEOFFSET NOT NULL,
        [IsDeleted] BIT NOT NULL,
        CONSTRAINT [PK_Notifications_NotificationDetail] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[dbo].[Notifications_UserNotification]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Notifications_UserNotification] (
        [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_Notifications_UserNotification_Id] DEFAULT NEWID(),
        [UserId] BIGINT NOT NULL,
        [NotificationDetailId] BIGINT NOT NULL,
        [State] INT NOT NULL,
        [CreatedOn] DATETIMEOFFSET NOT NULL,
        [IsDeleted] BIT NOT NULL,
        CONSTRAINT [PK_Notifications_UserNotification] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[dbo].[Notifications_NotificationSubscription]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Notifications_NotificationSubscription] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [UserId] BIGINT NOT NULL,
        [NotificationName] NVARCHAR(96) NULL,
        [EntityTypeName] NVARCHAR(250) NULL,
        [EntityTypeAssemblyQualifiedName] NVARCHAR(512) NULL,
        [EntityId] NVARCHAR(96) NULL,
        [CreatedOn] DATETIMEOFFSET NOT NULL,
        [IsDeleted] BIT NOT NULL,
        CONSTRAINT [PK_Notifications_NotificationSubscription] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[dbo].[Notifications_UserNotification]', N'U') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Notifications_UserNotification_UserId_State' AND [object_id] = OBJECT_ID(N'[dbo].[Notifications_UserNotification]'))
BEGIN
    CREATE INDEX [IX_Notifications_UserNotification_UserId_State]
        ON [dbo].[Notifications_UserNotification] ([UserId], [State]);
END;

IF OBJECT_ID(N'[dbo].[Notifications_UserNotification]', N'U') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Notifications_UserNotification_NotificationDetailId' AND [object_id] = OBJECT_ID(N'[dbo].[Notifications_UserNotification]'))
BEGIN
    CREATE INDEX [IX_Notifications_UserNotification_NotificationDetailId]
        ON [dbo].[Notifications_UserNotification] ([NotificationDetailId]);
END;

IF OBJECT_ID(N'[dbo].[Notifications_NotificationSubscription]', N'U') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Notifications_NotificationSubscription_UserId' AND [object_id] = OBJECT_ID(N'[dbo].[Notifications_NotificationSubscription]'))
BEGIN
    CREATE INDEX [IX_Notifications_NotificationSubscription_UserId]
        ON [dbo].[Notifications_NotificationSubscription] ([UserId]);
END;

IF OBJECT_ID(N'[dbo].[Notifications_NotificationSubscription]', N'U') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Notifications_NotificationSubscription_Lookup' AND [object_id] = OBJECT_ID(N'[dbo].[Notifications_NotificationSubscription]'))
BEGIN
    CREATE INDEX [IX_Notifications_NotificationSubscription_Lookup]
        ON [dbo].[Notifications_NotificationSubscription] ([NotificationName], [EntityTypeName], [EntityId]);
END;

IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NOT NULL
    AND NOT EXISTS (
        SELECT 1
        FROM [dbo].[__EFMigrationsHistory]
        WHERE [MigrationId] = N'20260824164500_AddNotificationTables'
    )
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260824164500_AddNotificationTables', N'8.0.0');
END;
