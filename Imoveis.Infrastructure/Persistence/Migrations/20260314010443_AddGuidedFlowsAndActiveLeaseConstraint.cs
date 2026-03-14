using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imoveis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGuidedFlowsAndActiveLeaseConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_lease_contracts_PropertyId",
                table: "lease_contracts");

            migrationBuilder.AddColumn<bool>(
                name: "EnableGuidedFlows",
                table: "system_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_lease_contracts_PropertyId_active",
                table: "lease_contracts",
                column: "PropertyId",
                unique: true,
                filter: "\"Status\" = 'ACTIVE'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_lease_contracts_PropertyId_active",
                table: "lease_contracts");

            migrationBuilder.DropColumn(
                name: "EnableGuidedFlows",
                table: "system_settings");

            migrationBuilder.CreateIndex(
                name: "IX_lease_contracts_PropertyId",
                table: "lease_contracts",
                column: "PropertyId");
        }
    }
}
