using ECommerce.Module.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.WebHost.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(SimplDbContext))]
    [Migration("20260827162000_AddNotificationDevices")]
    public partial class AddNotificationDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Notifications_Device]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Notifications_Device] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [Token] NVARCHAR(2048) NOT NULL,
        [TokenHash] NVARCHAR(64) NOT NULL,
        [UserId] BIGINT NULL,
        [Platform] NVARCHAR(32) NULL,
        [CreatedOn] DATETIMEOFFSET NOT NULL,
        [UpdatedOn] DATETIMEOFFSET NOT NULL,
        [IsDeleted] BIT NOT NULL,
        CONSTRAINT [PK_Notifications_Device] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[dbo].[Notifications_Device]', N'U') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Notifications_Device_TokenHash' AND [object_id] = OBJECT_ID(N'[dbo].[Notifications_Device]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Notifications_Device_TokenHash]
        ON [dbo].[Notifications_Device] ([TokenHash]);
END;

IF OBJECT_ID(N'[dbo].[Notifications_Device]', N'U') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Notifications_Device_UserId' AND [object_id] = OBJECT_ID(N'[dbo].[Notifications_Device]'))
BEGIN
    CREATE INDEX [IX_Notifications_Device_UserId]
        ON [dbo].[Notifications_Device] ([UserId]);
END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Notifications_Device]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[Notifications_Device];
END;");
        }
    }
}
