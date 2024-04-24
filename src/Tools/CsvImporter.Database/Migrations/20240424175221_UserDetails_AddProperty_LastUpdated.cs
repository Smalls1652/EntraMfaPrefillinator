using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EntraMfaPrefillinator.Tools.CsvImporter.Database.Migrations
{
    /// <inheritdoc />
    public partial class UserDetails_AddProperty_LastUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastUpdated",
                table: "UserDetails",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "UserDetails");
        }
    }
}
