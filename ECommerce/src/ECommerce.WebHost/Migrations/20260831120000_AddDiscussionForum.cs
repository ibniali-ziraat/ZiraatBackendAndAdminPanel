using ECommerce.Module.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.WebHost.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(SimplDbContext))]
    [Migration("20260831120000_AddDiscussionForum")]
    public partial class AddDiscussionForum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Forum_UserProfile]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Forum_UserProfile] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [UserId] BIGINT NOT NULL,
        [Avatar] NVARCHAR(450) NULL,
        [Bio] NVARCHAR(MAX) NULL,
        [Reputation] INT NOT NULL CONSTRAINT [DF_Forum_UserProfile_Reputation] DEFAULT 0,
        [GoldBadges] INT NOT NULL CONSTRAINT [DF_Forum_UserProfile_GoldBadges] DEFAULT 0,
        [SilverBadges] INT NOT NULL CONSTRAINT [DF_Forum_UserProfile_SilverBadges] DEFAULT 0,
        [BronzeBadges] INT NOT NULL CONSTRAINT [DF_Forum_UserProfile_BronzeBadges] DEFAULT 0,
        [Location] NVARCHAR(450) NULL,
        [Website] NVARCHAR(450) NULL,
        [IsOnline] BIT NOT NULL CONSTRAINT [DF_Forum_UserProfile_IsOnline] DEFAULT 0,
        [JoinedAt] DATETIMEOFFSET NOT NULL,
        [LastSeen] DATETIMEOFFSET NOT NULL,
        CONSTRAINT [PK_Forum_UserProfile] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[dbo].[Forum_Tag]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Forum_Tag] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(128) NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [Color] NVARCHAR(32) NULL,
        [QuestionsCount] INT NOT NULL CONSTRAINT [DF_Forum_Tag_QuestionsCount] DEFAULT 0,
        CONSTRAINT [PK_Forum_Tag] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[dbo].[Forum_Question]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Forum_Question] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [Title] NVARCHAR(450) NOT NULL,
        [Body] NVARCHAR(MAX) NOT NULL,
        [UserId] BIGINT NOT NULL,
        [Votes] INT NOT NULL CONSTRAINT [DF_Forum_Question_Votes] DEFAULT 0,
        [Views] INT NOT NULL CONSTRAINT [DF_Forum_Question_Views] DEFAULT 0,
        [AnswersCount] INT NOT NULL CONSTRAINT [DF_Forum_Question_AnswersCount] DEFAULT 0,
        [AcceptedAnswerId] BIGINT NULL,
        [Status] NVARCHAR(32) NULL,
        [IsBounty] BIT NOT NULL CONSTRAINT [DF_Forum_Question_IsBounty] DEFAULT 0,
        [BountyAmount] INT NOT NULL CONSTRAINT [DF_Forum_Question_BountyAmount] DEFAULT 0,
        [IsClosed] BIT NOT NULL CONSTRAINT [DF_Forum_Question_IsClosed] DEFAULT 0,
        [IsProtected] BIT NOT NULL CONSTRAINT [DF_Forum_Question_IsProtected] DEFAULT 0,
        [Favorites] INT NOT NULL CONSTRAINT [DF_Forum_Question_Favorites] DEFAULT 0,
        [CreatedAt] DATETIMEOFFSET NOT NULL,
        [UpdatedAt] DATETIMEOFFSET NOT NULL,
        [LastActivityAt] DATETIMEOFFSET NOT NULL,
        CONSTRAINT [PK_Forum_Question] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[dbo].[Forum_QuestionTag]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Forum_QuestionTag] (
        [QuestionId] BIGINT NOT NULL,
        [TagId] BIGINT NOT NULL,
        CONSTRAINT [PK_Forum_QuestionTag] PRIMARY KEY ([QuestionId], [TagId])
    );
END;

IF OBJECT_ID(N'[dbo].[Forum_Answer]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Forum_Answer] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [QuestionId] BIGINT NOT NULL,
        [UserId] BIGINT NOT NULL,
        [Body] NVARCHAR(MAX) NOT NULL,
        [Votes] INT NOT NULL CONSTRAINT [DF_Forum_Answer_Votes] DEFAULT 0,
        [IsAccepted] BIT NOT NULL CONSTRAINT [DF_Forum_Answer_IsAccepted] DEFAULT 0,
        [CreatedAt] DATETIMEOFFSET NOT NULL,
        [UpdatedAt] DATETIMEOFFSET NOT NULL,
        CONSTRAINT [PK_Forum_Answer] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[dbo].[Forum_Comment]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Forum_Comment] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [UserId] BIGINT NOT NULL,
        [QuestionId] BIGINT NULL,
        [AnswerId] BIGINT NULL,
        [Body] NVARCHAR(MAX) NOT NULL,
        [Votes] INT NOT NULL CONSTRAINT [DF_Forum_Comment_Votes] DEFAULT 0,
        [CreatedAt] DATETIMEOFFSET NOT NULL,
        CONSTRAINT [PK_Forum_Comment] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[dbo].[Forum_Vote]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Forum_Vote] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [UserId] BIGINT NOT NULL,
        [TargetType] NVARCHAR(32) NOT NULL,
        [TargetId] BIGINT NOT NULL,
        [Value] INT NOT NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL,
        CONSTRAINT [PK_Forum_Vote] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[dbo].[Forum_Media]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Forum_Media] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [QuestionId] BIGINT NULL,
        [AnswerId] BIGINT NULL,
        [Type] NVARCHAR(32) NOT NULL,
        [Url] NVARCHAR(2048) NOT NULL,
        [Thumbnail] NVARCHAR(2048) NULL,
        [AltText] NVARCHAR(450) NULL,
        [Width] INT NULL,
        [Height] INT NULL,
        [Duration] NVARCHAR(64) NULL,
        [Platform] NVARCHAR(128) NULL,
        [FileSize] BIGINT NULL,
        [MimeType] NVARCHAR(128) NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL,
        CONSTRAINT [PK_Forum_Media] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[dbo].[Forum_Bookmark]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Forum_Bookmark] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [UserId] BIGINT NOT NULL,
        [QuestionId] BIGINT NOT NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL,
        CONSTRAINT [PK_Forum_Bookmark] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[dbo].[Forum_Notification]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Forum_Notification] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [UserId] BIGINT NOT NULL,
        [Type] NVARCHAR(32) NOT NULL,
        [Message] NVARCHAR(MAX) NOT NULL,
        [QuestionId] BIGINT NULL,
        [AnswerId] BIGINT NULL,
        [IsRead] BIT NOT NULL CONSTRAINT [DF_Forum_Notification_IsRead] DEFAULT 0,
        [CreatedAt] DATETIMEOFFSET NOT NULL,
        CONSTRAINT [PK_Forum_Notification] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[dbo].[Forum_LinkedQuestion]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Forum_LinkedQuestion] (
        [QuestionId] BIGINT NOT NULL,
        [LinkedQuestionId] BIGINT NOT NULL,
        CONSTRAINT [PK_Forum_LinkedQuestion] PRIMARY KEY ([QuestionId], [LinkedQuestionId])
    );
END;

IF OBJECT_ID(N'[dbo].[Forum_UserTag]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Forum_UserTag] (
        [UserId] BIGINT NOT NULL,
        [TagId] BIGINT NOT NULL,
        CONSTRAINT [PK_Forum_UserTag] PRIMARY KEY ([UserId], [TagId])
    );
END;

IF OBJECT_ID(N'[dbo].[Forum_SearchSuggestion]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Forum_SearchSuggestion] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [Term] NVARCHAR(256) NOT NULL,
        [Weight] INT NOT NULL CONSTRAINT [DF_Forum_SearchSuggestion_Weight] DEFAULT 1,
        CONSTRAINT [PK_Forum_SearchSuggestion] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[dbo].[Forum_QuestionView]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Forum_QuestionView] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [QuestionId] BIGINT NOT NULL,
        [UserId] BIGINT NOT NULL,
        [ViewedAt] DATETIMEOFFSET NOT NULL,
        CONSTRAINT [PK_Forum_QuestionView] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Forum_UserProfile_UserId' AND [object_id] = OBJECT_ID(N'[dbo].[Forum_UserProfile]'))
    CREATE UNIQUE INDEX [IX_Forum_UserProfile_UserId] ON [dbo].[Forum_UserProfile] ([UserId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Forum_Tag_Name' AND [object_id] = OBJECT_ID(N'[dbo].[Forum_Tag]'))
    CREATE UNIQUE INDEX [IX_Forum_Tag_Name] ON [dbo].[Forum_Tag] ([Name]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Forum_Question_UserId' AND [object_id] = OBJECT_ID(N'[dbo].[Forum_Question]'))
    CREATE INDEX [IX_Forum_Question_UserId] ON [dbo].[Forum_Question] ([UserId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Forum_Question_Status' AND [object_id] = OBJECT_ID(N'[dbo].[Forum_Question]'))
    CREATE INDEX [IX_Forum_Question_Status] ON [dbo].[Forum_Question] ([Status]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Forum_Question_Votes' AND [object_id] = OBJECT_ID(N'[dbo].[Forum_Question]'))
    CREATE INDEX [IX_Forum_Question_Votes] ON [dbo].[Forum_Question] ([Votes]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Forum_Question_CreatedAt' AND [object_id] = OBJECT_ID(N'[dbo].[Forum_Question]'))
    CREATE INDEX [IX_Forum_Question_CreatedAt] ON [dbo].[Forum_Question] ([CreatedAt]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Forum_Question_LastActivityAt' AND [object_id] = OBJECT_ID(N'[dbo].[Forum_Question]'))
    CREATE INDEX [IX_Forum_Question_LastActivityAt] ON [dbo].[Forum_Question] ([LastActivityAt]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Forum_Answer_QuestionId' AND [object_id] = OBJECT_ID(N'[dbo].[Forum_Answer]'))
    CREATE INDEX [IX_Forum_Answer_QuestionId] ON [dbo].[Forum_Answer] ([QuestionId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Forum_Answer_UserId' AND [object_id] = OBJECT_ID(N'[dbo].[Forum_Answer]'))
    CREATE INDEX [IX_Forum_Answer_UserId] ON [dbo].[Forum_Answer] ([UserId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Forum_Answer_Votes' AND [object_id] = OBJECT_ID(N'[dbo].[Forum_Answer]'))
    CREATE INDEX [IX_Forum_Answer_Votes] ON [dbo].[Forum_Answer] ([Votes]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Forum_Comment_QuestionId' AND [object_id] = OBJECT_ID(N'[dbo].[Forum_Comment]'))
    CREATE INDEX [IX_Forum_Comment_QuestionId] ON [dbo].[Forum_Comment] ([QuestionId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Forum_Comment_AnswerId' AND [object_id] = OBJECT_ID(N'[dbo].[Forum_Comment]'))
    CREATE INDEX [IX_Forum_Comment_AnswerId] ON [dbo].[Forum_Comment] ([AnswerId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Forum_Vote_User_Target' AND [object_id] = OBJECT_ID(N'[dbo].[Forum_Vote]'))
    CREATE UNIQUE INDEX [IX_Forum_Vote_User_Target] ON [dbo].[Forum_Vote] ([UserId], [TargetType], [TargetId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Forum_Media_QuestionId' AND [object_id] = OBJECT_ID(N'[dbo].[Forum_Media]'))
    CREATE INDEX [IX_Forum_Media_QuestionId] ON [dbo].[Forum_Media] ([QuestionId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Forum_Media_AnswerId' AND [object_id] = OBJECT_ID(N'[dbo].[Forum_Media]'))
    CREATE INDEX [IX_Forum_Media_AnswerId] ON [dbo].[Forum_Media] ([AnswerId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Forum_Bookmark_User_Question' AND [object_id] = OBJECT_ID(N'[dbo].[Forum_Bookmark]'))
    CREATE UNIQUE INDEX [IX_Forum_Bookmark_User_Question] ON [dbo].[Forum_Bookmark] ([UserId], [QuestionId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Forum_Notification_User_Read' AND [object_id] = OBJECT_ID(N'[dbo].[Forum_Notification]'))
    CREATE INDEX [IX_Forum_Notification_User_Read] ON [dbo].[Forum_Notification] ([UserId], [IsRead]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Forum_SearchSuggestion_Term' AND [object_id] = OBJECT_ID(N'[dbo].[Forum_SearchSuggestion]'))
    CREATE UNIQUE INDEX [IX_Forum_SearchSuggestion_Term] ON [dbo].[Forum_SearchSuggestion] ([Term]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Forum_QuestionView_QuestionId' AND [object_id] = OBJECT_ID(N'[dbo].[Forum_QuestionView]'))
    CREATE INDEX [IX_Forum_QuestionView_QuestionId] ON [dbo].[Forum_QuestionView] ([QuestionId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Forum_QuestionView_UserId' AND [object_id] = OBJECT_ID(N'[dbo].[Forum_QuestionView]'))
    CREATE INDEX [IX_Forum_QuestionView_UserId] ON [dbo].[Forum_QuestionView] ([UserId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Forum_QuestionView_Question_User' AND [object_id] = OBJECT_ID(N'[dbo].[Forum_QuestionView]'))
    CREATE UNIQUE INDEX [IX_Forum_QuestionView_Question_User] ON [dbo].[Forum_QuestionView] ([QuestionId], [UserId]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Forum_QuestionView]', N'U') IS NOT NULL DROP TABLE [dbo].[Forum_QuestionView];
IF OBJECT_ID(N'[dbo].[Forum_SearchSuggestion]', N'U') IS NOT NULL DROP TABLE [dbo].[Forum_SearchSuggestion];
IF OBJECT_ID(N'[dbo].[Forum_UserTag]', N'U') IS NOT NULL DROP TABLE [dbo].[Forum_UserTag];
IF OBJECT_ID(N'[dbo].[Forum_LinkedQuestion]', N'U') IS NOT NULL DROP TABLE [dbo].[Forum_LinkedQuestion];
IF OBJECT_ID(N'[dbo].[Forum_Notification]', N'U') IS NOT NULL DROP TABLE [dbo].[Forum_Notification];
IF OBJECT_ID(N'[dbo].[Forum_Bookmark]', N'U') IS NOT NULL DROP TABLE [dbo].[Forum_Bookmark];
IF OBJECT_ID(N'[dbo].[Forum_Media]', N'U') IS NOT NULL DROP TABLE [dbo].[Forum_Media];
IF OBJECT_ID(N'[dbo].[Forum_Vote]', N'U') IS NOT NULL DROP TABLE [dbo].[Forum_Vote];
IF OBJECT_ID(N'[dbo].[Forum_Comment]', N'U') IS NOT NULL DROP TABLE [dbo].[Forum_Comment];
IF OBJECT_ID(N'[dbo].[Forum_Answer]', N'U') IS NOT NULL DROP TABLE [dbo].[Forum_Answer];
IF OBJECT_ID(N'[dbo].[Forum_QuestionTag]', N'U') IS NOT NULL DROP TABLE [dbo].[Forum_QuestionTag];
IF OBJECT_ID(N'[dbo].[Forum_Question]', N'U') IS NOT NULL DROP TABLE [dbo].[Forum_Question];
IF OBJECT_ID(N'[dbo].[Forum_Tag]', N'U') IS NOT NULL DROP TABLE [dbo].[Forum_Tag];
IF OBJECT_ID(N'[dbo].[Forum_UserProfile]', N'U') IS NOT NULL DROP TABLE [dbo].[Forum_UserProfile];
");
        }
    }
}
