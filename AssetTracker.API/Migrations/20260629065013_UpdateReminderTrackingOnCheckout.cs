using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateReminderTrackingOnCheckout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReminderSent",
                table: "Checkouts");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReminderSentUtc",
                table: "Checkouts",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReminderSentUtc",
                table: "Checkouts");

            migrationBuilder.AddColumn<bool>(
                name: "ReminderSent",
                table: "Checkouts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
