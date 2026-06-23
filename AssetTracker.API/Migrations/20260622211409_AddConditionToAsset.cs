using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class AddConditionToAsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Condition",
                table: "Assets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Good");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Condition",
                table: "Assets");
        }
    }
}
