using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace netcore_api_rbac_starter.Migrations
{
    /// <inheritdoc />
    public partial class ResetIsActiveProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "positions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "departments",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_active",
                table: "positions");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "departments");
        }
    }
}
