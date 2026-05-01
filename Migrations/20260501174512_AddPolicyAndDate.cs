using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salamaty.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyAndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PolicyNumber",
                table: "InsuranceProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidUntil",
                table: "InsuranceProfiles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PolicyNumber",
                table: "InsuranceProfiles");

            migrationBuilder.DropColumn(
                name: "ValidUntil",
                table: "InsuranceProfiles");
        }
    }
}
