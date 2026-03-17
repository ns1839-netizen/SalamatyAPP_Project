using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salamaty.API.Migrations
{
    /// <inheritdoc />
    public partial class AddLatLong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "MedicalProviders",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "MedicalProviders",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "MedicalProviders");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "MedicalProviders");
        }
    }
}
