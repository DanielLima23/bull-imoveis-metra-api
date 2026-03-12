using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imoveis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyLegacyStructureAndParties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Bedrooms",
                table: "properties",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeedNumber",
                table: "properties",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasElevator",
                table: "properties",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasGarage",
                table: "properties",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationCertificate",
                table: "properties",
                type: "character varying(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "properties",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VacancyReason",
                table: "properties",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "VacatedAt",
                table: "properties",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Acronym",
                table: "pendency_types",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "pendency_types",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "pendency_types",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdjustmentIndex",
                table: "lease_contracts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuaranteeDetails",
                table: "lease_contracts",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuaranteeType",
                table: "lease_contracts",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentDay",
                table: "lease_contracts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentLocation",
                table: "lease_contracts",
                type: "character varying(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.Sql("""
                WITH prepared AS (
                    SELECT
                        "Id",
                        COALESCE(
                            NULLIF(UPPER(LEFT(REGEXP_REPLACE("Name", '[^A-Za-z0-9]+', '', 'g'), 20)), ''),
                            'PEND'
                        ) AS base_acronym
                    FROM pendency_types
                ),
                ranked AS (
                    SELECT
                        "Id",
                        base_acronym,
                        ROW_NUMBER() OVER (PARTITION BY base_acronym ORDER BY "Id") AS acronym_order
                    FROM prepared
                )
                UPDATE pendency_types AS pt
                SET "Acronym" = CASE
                    WHEN r.acronym_order = 1 THEN r.base_acronym
                    ELSE LEFT(r.base_acronym, 11) || '_' || SUBSTRING(REPLACE(CAST(pt."Id" AS text), '-', '') FROM 1 FOR 8)
                END
                FROM ranked AS r
                WHERE pt."Id" = r."Id";
                """);

            migrationBuilder.CreateTable(
                name: "parties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Email = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "property_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_property_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_property_documents_properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "property_history_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_property_history_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_property_history_entries_properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "property_party_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_property_party_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_property_party_links_parties_PartyId",
                        column: x => x.PartyId,
                        principalTable: "parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_property_party_links_properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pendency_types_Acronym",
                table: "pendency_types",
                column: "Acronym",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_parties_Name",
                table: "parties",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_property_documents_PropertyId",
                table: "property_documents",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_property_history_entries_PropertyId_OccurredAtUtc",
                table: "property_history_entries",
                columns: new[] { "PropertyId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_property_party_links_PartyId",
                table: "property_party_links",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_property_party_links_PropertyId_PartyId_Role",
                table: "property_party_links",
                columns: new[] { "PropertyId", "PartyId", "Role" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "property_documents");

            migrationBuilder.DropTable(
                name: "property_history_entries");

            migrationBuilder.DropTable(
                name: "property_party_links");

            migrationBuilder.DropTable(
                name: "parties");

            migrationBuilder.DropIndex(
                name: "IX_pendency_types_Acronym",
                table: "pendency_types");

            migrationBuilder.DropColumn(
                name: "Bedrooms",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "DeedNumber",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "HasElevator",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "HasGarage",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "RegistrationCertificate",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "VacancyReason",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "VacatedAt",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "Acronym",
                table: "pendency_types");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "pendency_types");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "pendency_types");

            migrationBuilder.DropColumn(
                name: "AdjustmentIndex",
                table: "lease_contracts");

            migrationBuilder.DropColumn(
                name: "GuaranteeDetails",
                table: "lease_contracts");

            migrationBuilder.DropColumn(
                name: "GuaranteeType",
                table: "lease_contracts");

            migrationBuilder.DropColumn(
                name: "PaymentDay",
                table: "lease_contracts");

            migrationBuilder.DropColumn(
                name: "PaymentLocation",
                table: "lease_contracts");
        }
    }
}
