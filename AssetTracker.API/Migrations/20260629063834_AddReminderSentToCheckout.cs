using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderSentToCheckout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ReminderSent",
                table: "Checkouts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReminderSent",
                table: "Checkouts");
        }
    }
}
