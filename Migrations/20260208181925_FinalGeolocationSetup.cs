using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salamaty.API.Migrations
{
    /// <inheritdoc />
    public partial class FinalGeolocationSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Doctors",
                table: "MedicalProviders");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "MedicalProviders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Doctors",
                table: "MedicalProviders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "MedicalProviders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
