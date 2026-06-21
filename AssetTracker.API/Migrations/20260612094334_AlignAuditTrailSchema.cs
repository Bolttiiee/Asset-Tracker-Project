using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class AlignAuditTrailSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[AuditTrail]', N'U') IS NULL
BEGIN
    CREATE TABLE [AuditTrail] (
        [AuditLogID] INT IDENTITY(1,1) NOT NULL,
        [Action] NVARCHAR(50) NOT NULL,
        [AssetID] INT NOT NULL CONSTRAINT [DF_AuditTrail_AssetID] DEFAULT(0),
        [AssetName] NVARCHAR(100) NOT NULL CONSTRAINT [DF_AuditTrail_AssetName] DEFAULT(N''),
        [PerformedByEmail] NVARCHAR(256) NOT NULL CONSTRAINT [DF_AuditTrail_PerformedByEmail] DEFAULT(N''),
        [Timestamp] DATETIME2 NOT NULL,
        [Details] NVARCHAR(MAX) NOT NULL,
        CONSTRAINT [PK_AuditTrail] PRIMARY KEY ([AuditLogID])
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'AuditTrail', N'AssetID') IS NULL
        ALTER TABLE [AuditTrail] ADD [AssetID] INT NOT NULL CONSTRAINT [DF_AuditTrail_AssetID] DEFAULT(0);

    IF COL_LENGTH(N'AuditTrail', N'AssetName') IS NULL
        ALTER TABLE [AuditTrail] ADD [AssetName] NVARCHAR(100) NOT NULL CONSTRAINT [DF_AuditTrail_AssetName] DEFAULT(N'');

    IF COL_LENGTH(N'AuditTrail', N'PerformedByEmail') IS NULL
        ALTER TABLE [AuditTrail] ADD [PerformedByEmail] NVARCHAR(256) NOT NULL CONSTRAINT [DF_AuditTrail_PerformedByEmail] DEFAULT(N'');
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[AuditTrail]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'AuditTrail', N'PerformedByUserID') IS NULL
    BEGIN
        DROP TABLE [AuditTrail];
    END
    ELSE
    BEGIN
        IF COL_LENGTH(N'AuditTrail', N'AssetID') IS NOT NULL
        BEGIN
            IF OBJECT_ID(N'[DF_AuditTrail_AssetID]', N'D') IS NOT NULL
                ALTER TABLE [AuditTrail] DROP CONSTRAINT [DF_AuditTrail_AssetID];
            ALTER TABLE [AuditTrail] DROP COLUMN [AssetID];
        END

        IF COL_LENGTH(N'AuditTrail', N'AssetName') IS NOT NULL
        BEGIN
            IF OBJECT_ID(N'[DF_AuditTrail_AssetName]', N'D') IS NOT NULL
                ALTER TABLE [AuditTrail] DROP CONSTRAINT [DF_AuditTrail_AssetName];
            ALTER TABLE [AuditTrail] DROP COLUMN [AssetName];
        END

        IF COL_LENGTH(N'AuditTrail', N'PerformedByEmail') IS NOT NULL
        BEGIN
            IF OBJECT_ID(N'[DF_AuditTrail_PerformedByEmail]', N'D') IS NOT NULL
                ALTER TABLE [AuditTrail] DROP CONSTRAINT [DF_AuditTrail_PerformedByEmail];
            ALTER TABLE [AuditTrail] DROP COLUMN [PerformedByEmail];
        END
    END
END
");
        }
    }
}
