using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salamaty.API.Migrations
{
    public partial class FixSync : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 🔥 SAFE DROP (بدل DropForeignKey المباشر)
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * FROM sys.foreign_keys 
                    WHERE name = 'FK_InsuranceNetworkServices_InsuranceProviders_InsuranceProviderId'
                )
                ALTER TABLE InsuranceNetworkServices 
                DROP CONSTRAINT FK_InsuranceNetworkServices_InsuranceProviders_InsuranceProviderId
            ");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "InsuranceNetworkServices",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "InsuranceNetworkServices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "OpenTo",
                table: "InsuranceNetworkServices",
                type: "time",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "OpenFrom",
                table: "InsuranceNetworkServices",
                type: "time",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "InsuranceNetworkServices",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "InsuranceNetworkServices",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<int>(
                name: "InsuranceProviderId",
                table: "InsuranceNetworkServices",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");







            //migrationBuilder.CreateTable(
            //    name: "MedicalProducts",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //        Composition = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        Uses = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        SideEffects = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        Manufacturer = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        ExcellentReviewPercent = table.Column<int>(type: "int", nullable: false),
            //        AverageReviewPercent = table.Column<int>(type: "int", nullable: false),
            //        Price = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_MedicalProducts", x => x.Id);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "Prescriptions",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        ScanDate = table.Column<DateTime>(type: "datetime2", nullable: false),
            //        DetectedMedicines = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        UserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_Prescriptions", x => x.Id);
            //    });

            //migrationBuilder.CreateIndex(
            //    name: "IX_Prescriptions_UserId",
            //    table: "Prescriptions",
            //    column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MedicalProducts");
            migrationBuilder.DropTable(name: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "InsuranceNetworkServices");

            migrationBuilder.DropColumn(
                name: "Governorate",
                table: "InsuranceNetworkServices");

            migrationBuilder.DropColumn(
                name: "InsuranceProviderName",
                table: "InsuranceNetworkServices");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "InsuranceNetworkServices",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "InsuranceNetworkServices",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "OpenTo",
                table: "InsuranceNetworkServices",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0),
                oldClrType: typeof(TimeSpan),
                oldType: "time",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "OpenFrom",
                table: "InsuranceNetworkServices",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0),
                oldClrType: typeof(TimeSpan),
                oldType: "time",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "InsuranceNetworkServices",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "InsuranceNetworkServices",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "InsuranceProviderId",
                table: "InsuranceNetworkServices",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}