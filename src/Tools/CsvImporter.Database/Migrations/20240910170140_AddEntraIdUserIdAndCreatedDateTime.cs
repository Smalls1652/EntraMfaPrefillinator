using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EntraMfaPrefillinator.Tools.CsvImporter.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEntraIdUserIdAndCreatedDateTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EntraUserCreatedDateTime",
                table: "UserDetails",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "EntraUserId",
                table: "UserDetails",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntraUserCreatedDateTime",
                table: "UserDetails");

            migrationBuilder.DropColumn(
                name: "EntraUserId",
                table: "UserDetails");
        }
    }
}
