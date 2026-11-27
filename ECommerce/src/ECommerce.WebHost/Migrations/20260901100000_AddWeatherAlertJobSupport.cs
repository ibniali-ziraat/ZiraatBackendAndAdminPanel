using ECommerce.Module.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.WebHost.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(SimplDbContext))]
    [Migration("20260901100000_AddWeatherAlertJobSupport")]
    public partial class AddWeatherAlertJobSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Notifications_Device]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[Notifications_Device]', N'Latitude') IS NULL
        ALTER TABLE [dbo].[Notifications_Device] ADD [Latitude] DECIMAL(9,6) NULL;

    IF COL_LENGTH(N'[dbo].[Notifications_Device]', N'Longitude') IS NULL
        ALTER TABLE [dbo].[Notifications_Device] ADD [Longitude] DECIMAL(9,6) NULL;

    IF COL_LENGTH(N'[dbo].[Notifications_Device]', N'LocationName') IS NULL
        ALTER TABLE [dbo].[Notifications_Device] ADD [LocationName] NVARCHAR(256) NULL;
END;

IF OBJECT_ID(N'[dbo].[Notifications_WeatherAlertLog]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Notifications_WeatherAlertLog] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [AlertKey] NVARCHAR(256) NOT NULL,
        [UserId] BIGINT NULL,
        [TokenHash] NVARCHAR(64) NOT NULL,
        [Event] NVARCHAR(128) NULL,
        [Severity] NVARCHAR(64) NULL,
        [SentOn] DATETIMEOFFSET NOT NULL,
        CONSTRAINT [PK_Notifications_WeatherAlertLog] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[dbo].[Notifications_Device]', N'U') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Notifications_Device_Latitude_Longitude' AND [object_id] = OBJECT_ID(N'[dbo].[Notifications_Device]'))
BEGIN
    CREATE INDEX [IX_Notifications_Device_Latitude_Longitude]
        ON [dbo].[Notifications_Device] ([Latitude], [Longitude]);
END;

IF OBJECT_ID(N'[dbo].[Notifications_WeatherAlertLog]', N'U') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Notifications_WeatherAlertLog_AlertKey_TokenHash' AND [object_id] = OBJECT_ID(N'[dbo].[Notifications_WeatherAlertLog]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Notifications_WeatherAlertLog_AlertKey_TokenHash]
        ON [dbo].[Notifications_WeatherAlertLog] ([AlertKey], [TokenHash]);
END;

IF OBJECT_ID(N'[dbo].[Notifications_WeatherAlertLog]', N'U') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Notifications_WeatherAlertLog_SentOn' AND [object_id] = OBJECT_ID(N'[dbo].[Notifications_WeatherAlertLog]'))
BEGIN
    CREATE INDEX [IX_Notifications_WeatherAlertLog_SentOn]
        ON [dbo].[Notifications_WeatherAlertLog] ([SentOn]);
END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Notifications_WeatherAlertLog]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[Notifications_WeatherAlertLog];
END;

IF OBJECT_ID(N'[dbo].[Notifications_Device]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Notifications_Device_Latitude_Longitude' AND [object_id] = OBJECT_ID(N'[dbo].[Notifications_Device]'))
        DROP INDEX [IX_Notifications_Device_Latitude_Longitude] ON [dbo].[Notifications_Device];

    IF COL_LENGTH(N'[dbo].[Notifications_Device]', N'LocationName') IS NOT NULL
        ALTER TABLE [dbo].[Notifications_Device] DROP COLUMN [LocationName];

    IF COL_LENGTH(N'[dbo].[Notifications_Device]', N'Longitude') IS NOT NULL
        ALTER TABLE [dbo].[Notifications_Device] DROP COLUMN [Longitude];

    IF COL_LENGTH(N'[dbo].[Notifications_Device]', N'Latitude') IS NOT NULL
        ALTER TABLE [dbo].[Notifications_Device] DROP COLUMN [Latitude];
END;
");
        }
    }
}
