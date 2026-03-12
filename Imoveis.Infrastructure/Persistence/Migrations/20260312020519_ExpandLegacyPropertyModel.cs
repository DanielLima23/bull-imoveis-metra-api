using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imoveis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandLegacyPropertyModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "properties",
                newName: "Observation");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "properties",
                newName: "OccupancyStatus");

            migrationBuilder.AddColumn<string>(
                name: "AdministrateTax",
                table: "properties",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Administrator",
                table: "properties",
                type: "character varying(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdministratorEmail",
                table: "properties",
                type: "character varying(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdministratorPhone",
                table: "properties",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssetState",
                table: "properties",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "READY");

            migrationBuilder.AddColumn<bool>(
                name: "CleaningIncluded",
                table: "properties",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Elevator",
                table: "properties",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Garage",
                table: "properties",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Lawyer",
                table: "properties",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LawyerData",
                table: "properties",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumOfRooms",
                table: "properties",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Proprietary",
                table: "properties",
                type: "character varying(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Registration",
                table: "properties",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationCertification",
                table: "properties",
                type: "character varying(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scripture",
                table: "properties",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "UnoccupiedSince",
                table: "properties",
                type: "date",
                nullable: true);

            migrationBuilder.RenameColumn(
                name: "Acronym",
                table: "pendency_types",
                newName: "Code");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "pendency_types",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.Sql("""
                UPDATE properties
                SET "AssetState" = CASE
                    WHEN "OccupancyStatus" = 'PREPARATION' THEN 'PREPARATION'
                    ELSE 'READY'
                END;
                """);

            migrationBuilder.Sql("""
                UPDATE properties
                SET "OccupancyStatus" = CASE
                    WHEN "OccupancyStatus" = 'LEASED' THEN 'OCCUPIED'
                    ELSE 'VACANT'
                END;
                """);

            migrationBuilder.Sql("""
                UPDATE pendency_types
                SET "Code" = CASE
                    WHEN "Name" = 'Documento' THEN 'DOC'
                    WHEN "Name" = 'Informacao Essencial' THEN 'INFO'
                    WHEN "Name" = 'Conta Atrasada' THEN 'FIN'
                    WHEN "Name" = 'Vistoria' THEN 'VIST'
                    ELSE 'PEND' || SUBSTRING(REPLACE(CAST("Id" AS text), '-', '') FROM 1 FOR 8)
                END
                WHERE COALESCE("Code", '') = '';
                """);

            migrationBuilder.AddColumn<string>(
                name: "ContractRegistration",
                table: "lease_contracts",
                type: "character varying(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractWith",
                table: "lease_contracts",
                type: "character varying(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuarantorDocument",
                table: "lease_contracts",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuarantorName",
                table: "lease_contracts",
                type: "character varying(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuarantorPhone",
                table: "lease_contracts",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Insurance",
                table: "lease_contracts",
                type: "character varying(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OptionalContactName",
                table: "lease_contracts",
                type: "character varying(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OptionalContactPhone",
                table: "lease_contracts",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.RenameColumn(
                name: "AdjustmentIndex",
                table: "lease_contracts",
                newName: "ReadjustmentIndex");

            migrationBuilder.AddColumn<string>(
                name: "SignatureRecognition",
                table: "lease_contracts",
                type: "character varying(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "expense_installments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaidBy",
                table: "expense_installments",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "lease_receivable_installments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaseContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaidBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lease_receivable_installments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lease_receivable_installments_lease_contracts_LeaseContract~",
                        column: x => x.LeaseContractId,
                        principalTable: "lease_contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "property_attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    ResourceLocation = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReferenceDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_property_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_property_attachments_properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "property_charge_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DefaultAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DueDay = table.Column<int>(type: "integer", nullable: true),
                    DefaultResponsibility = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ProviderInformation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_property_charge_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_property_charge_templates_properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "property_history_entries",
                newName: "Content");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "property_history_entries",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(180)",
                oldMaxLength: 180);

            migrationBuilder.Sql("""
                UPDATE property_history_entries
                SET "Content" = CASE
                    WHEN COALESCE("Content", '') = '' THEN COALESCE("Description", '')
                    WHEN COALESCE("Description", '') = '' THEN "Content"
                    ELSE "Content" || E'\n\n' || "Description"
                END;
                """);

            migrationBuilder.DropColumn(
                name: "Description",
                table: "property_history_entries");

            migrationBuilder.CreateIndex(
                name: "IX_pendency_types_Code",
                table: "pendency_types",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lease_receivable_installments_LeaseContractId_CompetenceDate",
                table: "lease_receivable_installments",
                columns: new[] { "LeaseContractId", "CompetenceDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_property_attachments_PropertyId",
                table: "property_attachments",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_property_charge_templates_PropertyId",
                table: "property_charge_templates",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_property_history_entries_PropertyId",
                table: "property_history_entries",
                column: "PropertyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lease_receivable_installments");

            migrationBuilder.DropTable(
                name: "property_attachments");

            migrationBuilder.DropTable(
                name: "property_charge_templates");

            migrationBuilder.DropIndex(
                name: "IX_pendency_types_Code",
                table: "pendency_types");

            migrationBuilder.DropIndex(
                name: "IX_property_history_entries_PropertyId",
                table: "property_history_entries");

            migrationBuilder.DropColumn(
                name: "AdministrateTax",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "Administrator",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "AdministratorEmail",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "AdministratorPhone",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "AssetState",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "CleaningIncluded",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "Elevator",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "Garage",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "Lawyer",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "LawyerData",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "NumOfRooms",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "Proprietary",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "Registration",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "RegistrationCertification",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "Scripture",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "UnoccupiedSince",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "ContractRegistration",
                table: "lease_contracts");

            migrationBuilder.DropColumn(
                name: "ContractWith",
                table: "lease_contracts");

            migrationBuilder.DropColumn(
                name: "GuarantorDocument",
                table: "lease_contracts");

            migrationBuilder.DropColumn(
                name: "GuarantorName",
                table: "lease_contracts");

            migrationBuilder.DropColumn(
                name: "GuarantorPhone",
                table: "lease_contracts");

            migrationBuilder.DropColumn(
                name: "Insurance",
                table: "lease_contracts");

            migrationBuilder.DropColumn(
                name: "OptionalContactName",
                table: "lease_contracts");

            migrationBuilder.DropColumn(
                name: "OptionalContactPhone",
                table: "lease_contracts");

            migrationBuilder.DropColumn(
                name: "SignatureRecognition",
                table: "lease_contracts");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "expense_installments");

            migrationBuilder.DropColumn(
                name: "PaidBy",
                table: "expense_installments");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "property_history_entries",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE property_history_entries
                SET "Content" = LEFT(COALESCE("Content", ''), 180),
                    "Description" = NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "property_history_entries",
                type: "character varying(180)",
                maxLength: 180,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "property_history_entries",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "pendency_types",
                newName: "Acronym");

            migrationBuilder.AlterColumn<string>(
                name: "Acronym",
                table: "pendency_types",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.RenameColumn(
                name: "ReadjustmentIndex",
                table: "lease_contracts",
                newName: "AdjustmentIndex");

            migrationBuilder.Sql("""
                UPDATE properties
                SET "OccupancyStatus" = CASE
                    WHEN "OccupancyStatus" = 'OCCUPIED' THEN 'LEASED'
                    WHEN "AssetState" = 'PREPARATION' THEN 'PREPARATION'
                    ELSE 'AVAILABLE'
                END;
                """);

            migrationBuilder.RenameColumn(
                name: "Observation",
                table: "properties",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "OccupancyStatus",
                table: "properties",
                newName: "Status");
        }
    }
}
