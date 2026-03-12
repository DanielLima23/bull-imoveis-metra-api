using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imoveis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPartyOab : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Oab",
                table: "parties",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Oab",
                table: "parties");
        }
    }
}
