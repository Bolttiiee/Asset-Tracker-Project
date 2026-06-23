using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetComplianceFieldsAndUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE: This migration is written idempotently because some environments already
            // had DecommissionDate / Notes columns added outside EF's migration history.
            // The guards let it reconcile drifted databases and still build fresh ones correctly.

            migrationBuilder.Sql(@"
IF COL_LENGTH('Assets', 'DateAdded') IS NULL
    ALTER TABLE [Assets] ADD [DateAdded] datetime2 NOT NULL DEFAULT (GETUTCDATE());");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Assets', 'DecommissionDate') IS NULL
    ALTER TABLE [Assets] ADD [DecommissionDate] datetime2 NULL;");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Assets', 'Notes') IS NULL
    ALTER TABLE [Assets] ADD [Notes] nvarchar(max) NULL;");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID('Users'))
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Assets_SerialNumber' AND object_id = OBJECT_ID('Assets'))
    CREATE UNIQUE INDEX [IX_Assets_SerialNumber] ON [Assets] ([SerialNumber]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID('Users'))
    DROP INDEX [IX_Users_Email] ON [Users];");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Assets_SerialNumber' AND object_id = OBJECT_ID('Assets'))
    DROP INDEX [IX_Assets_SerialNumber] ON [Assets];");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Assets', 'DateAdded') IS NOT NULL
    ALTER TABLE [Assets] DROP COLUMN [DateAdded];");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Assets', 'DecommissionDate') IS NOT NULL
    ALTER TABLE [Assets] DROP COLUMN [DecommissionDate];");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Assets', 'Notes') IS NOT NULL
    ALTER TABLE [Assets] DROP COLUMN [Notes];");
        }
    }
}
