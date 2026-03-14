using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imoveis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveCleaningIncludedToLease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CleaningIncluded",
                table: "lease_contracts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE lease_contracts AS lc
                SET "CleaningIncluded" = COALESCE(p."CleaningIncluded", FALSE)
                FROM properties AS p
                WHERE lc."PropertyId" = p."Id"
                  AND lc."Status" = 'ACTIVE';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CleaningIncluded",
                table: "lease_contracts");
        }
    }
}
