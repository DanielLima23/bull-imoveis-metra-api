using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imoveis.Infrastructure.Persistence.Migrations;

public partial class SetSandLightAsDefaultThemePreset : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "ThemePreset",
            table: "system_settings",
            type: "character varying(40)",
            maxLength: 40,
            nullable: false,
            defaultValue: "SAND_LIGHT",
            oldClrType: typeof(string),
            oldType: "character varying(40)",
            oldMaxLength: 40,
            oldDefaultValue: "AURORA_LIGHT");

        migrationBuilder.Sql("""
            UPDATE system_settings
            SET "ThemePreset" = 'SAND_LIGHT',
                "PrimaryColor" = '#8F6A3A',
                "SecondaryColor" = '#5E4525',
                "AccentColor" = '#C69A5D'
            WHERE "ThemePreset" = 'AURORA_LIGHT'
              AND "PrimaryColor" = '#1176EE'
              AND "SecondaryColor" = '#0A58BA'
              AND "AccentColor" = '#06B6D4';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE system_settings
            SET "ThemePreset" = 'AURORA_LIGHT',
                "PrimaryColor" = '#1176EE',
                "SecondaryColor" = '#0A58BA',
                "AccentColor" = '#06B6D4'
            WHERE "ThemePreset" = 'SAND_LIGHT'
              AND "PrimaryColor" = '#8F6A3A'
              AND "SecondaryColor" = '#5E4525'
              AND "AccentColor" = '#C69A5D';
            """);

        migrationBuilder.AlterColumn<string>(
            name: "ThemePreset",
            table: "system_settings",
            type: "character varying(40)",
            maxLength: 40,
            nullable: false,
            defaultValue: "AURORA_LIGHT",
            oldClrType: typeof(string),
            oldType: "character varying(40)",
            oldMaxLength: 40,
            oldDefaultValue: "SAND_LIGHT");
    }
}
