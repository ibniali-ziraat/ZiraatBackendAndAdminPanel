using ECommerce.Module.Core.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ECommerce.WebHost.Migrations
{
    [DbContext(typeof(SimplDbContext))]
    [Migration("20260930160000_AddFarmDelegationAndSupport")]
    public class AddFarmDelegationAndSupport : Migration
    {
        protected override void Up(MigrationBuilder m) => m.Sql(@"
CREATE TABLE [Core_FarmVendorDelegation]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[Version] uniqueidentifier NOT NULL,[VendorId] bigint NOT NULL,[FarmerId] bigint NOT NULL,[OrchardProfileId] bigint NOT NULL,[PermissionsJson] nvarchar(max) NOT NULL,[Status] nvarchar(16) NOT NULL,[CreatedOn] datetimeoffset NOT NULL,[CreatedById] bigint NOT NULL,[ExpiresOn] datetimeoffset NULL,[RevokedOn] datetimeoffset NULL,[RevokedById] bigint NULL,[RevocationReason] nvarchar(1000) NULL);
CREATE INDEX [IX_Core_FarmVendorDelegation_VendorId_OrchardProfileId_Status] ON [Core_FarmVendorDelegation]([VendorId],[OrchardProfileId],[Status]);
CREATE INDEX [IX_Core_FarmVendorDelegation_FarmerId_OrchardProfileId] ON [Core_FarmVendorDelegation]([FarmerId],[OrchardProfileId]);
CREATE TABLE [Core_FarmSupportCase]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[Version] uniqueidentifier NOT NULL,[OrchardProfileId] bigint NOT NULL,[FarmerId] bigint NOT NULL,[Kind] nvarchar(32) NOT NULL,[Status] nvarchar(16) NOT NULL,[Title] nvarchar(160) NOT NULL,[Detail] nvarchar(4000) NULL,[ExpertResponse] nvarchar(4000) NULL,[CreatedOn] datetimeoffset NOT NULL,[ScheduledOn] datetimeoffset NULL,[FirstResponseOn] datetimeoffset NULL,[ResolvedOn] datetimeoffset NULL,[CancelledOn] datetimeoffset NULL,[CreatedById] bigint NOT NULL,[AssignedToId] bigint NULL);
CREATE INDEX [IX_Core_FarmSupportCase_OrchardProfileId_Kind_CreatedOn] ON [Core_FarmSupportCase]([OrchardProfileId],[Kind],[CreatedOn]);
CREATE INDEX [IX_Core_FarmSupportCase_FarmerId_Status] ON [Core_FarmSupportCase]([FarmerId],[Status]);
CREATE TABLE [Core_FarmArticleRead]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[UserId] bigint NOT NULL,[ArticleId] nvarchar(200) NOT NULL,[ReadOn] datetimeoffset NOT NULL);
CREATE UNIQUE INDEX [IX_Core_FarmArticleRead_UserId_ArticleId] ON [Core_FarmArticleRead]([UserId],[ArticleId]);
CREATE TABLE [Core_FarmAutomationRule]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[Version] uniqueidentifier NOT NULL,[Kind] nvarchar(64) NOT NULL,[ConfigurationJson] nvarchar(max) NOT NULL,[Evidence] nvarchar(2000) NOT NULL,[Status] nvarchar(16) NOT NULL,[CreatedOn] datetimeoffset NOT NULL,[CreatedById] bigint NOT NULL,[ApprovedOn] datetimeoffset NULL,[ApprovedById] bigint NULL);
CREATE INDEX [IX_Core_FarmAutomationRule_Kind_Status] ON [Core_FarmAutomationRule]([Kind],[Status]);");
        protected override void Down(MigrationBuilder m) => m.Sql(@"DROP TABLE [Core_FarmAutomationRule]; DROP TABLE [Core_FarmArticleRead]; DROP TABLE [Core_FarmSupportCase]; DROP TABLE [Core_FarmVendorDelegation];");
    }
}
