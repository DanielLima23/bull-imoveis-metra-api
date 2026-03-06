using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imoveis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddThemePresetToSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThemePreset",
                table: "system_settings",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThemePreset",
                table: "system_settings");
        }
    }
}
