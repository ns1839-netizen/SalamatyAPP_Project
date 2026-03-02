using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salamaty.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialNewStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. إعادة إنشاء جدول الـ Roles (لأنه اتمسح من الـ Adminer)
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            // 2. إعادة إنشاء جدول ربط المستخدمين بالأدوار (UserRoles)
            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 3. إنشاء جداول شركات التأمين
            migrationBuilder.CreateTable(
                name: "InsuranceProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceProviders", x => x.Id);
                });

            // 4. إنشاء جدول المنتجات (الأدوية)
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SideEffects = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Pharmacies = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            // 5. إنشاء جداول شبكة خدمات التأمين (صيدليات، معامل، الخ)
            migrationBuilder.CreateTable(
                name: "InsuranceNetworkServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InsuranceProviderId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    OpenFrom = table.Column<TimeSpan>(type: "time", nullable: false),
                    OpenTo = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceNetworkServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsuranceNetworkServices_InsuranceProviders_InsuranceProviderId",
                        column: x => x.InsuranceProviderId,
                        principalTable: "InsuranceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 6. إنشاء جدول ملفات تأمين المستخدمين (الربط الأساسي مع Identity)
            migrationBuilder.CreateTable(
                name: "InsuranceProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InsuranceProviderId = table.Column<int>(type: "int", nullable: false),
                    CardHolderId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FrontImagePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BackImagePath = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsuranceProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InsuranceProfiles_InsuranceProviders_InsuranceProviderId",
                        column: x => x.InsuranceProviderId,
                        principalTable: "InsuranceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 7. إنشاء جدول المفضلات
            migrationBuilder.CreateTable(
                name: "Favorites",
                columns: table => new
                {
                    FavoriteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favorites", x => x.FavoriteId);
                    table.ForeignKey(
                        name: "FK_Favorites_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Favorites_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 8. إنشاء جدول بدائل المنتجات
            migrationBuilder.CreateTable(
                name: "ProductAlternatives",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    AlternativeProductId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAlternatives", x => new { x.ProductId, x.AlternativeProductId });
                    table.ForeignKey(
                        name: "FK_ProductAlternatives_Products_AlternativeProductId",
                        column: x => x.AlternativeProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductAlternatives_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // إنشاء الـ Indexes لتسريع البحث والربط
            migrationBuilder.CreateIndex(name: "IX_Favorites_ProductId", table: "Favorites", column: "ProductId");
            migrationBuilder.CreateIndex(name: "IX_Favorites_UserId", table: "Favorites", column: "UserId");
            migrationBuilder.CreateIndex(name: "IX_InsuranceNetworkServices_InsuranceProviderId", table: "InsuranceNetworkServices", column: "InsuranceProviderId");
            migrationBuilder.CreateIndex(name: "IX_InsuranceProfiles_InsuranceProviderId", table: "InsuranceProfiles", column: "InsuranceProviderId");
            migrationBuilder.CreateIndex(name: "IX_InsuranceProfiles_UserId", table: "InsuranceProfiles", column: "UserId");
            migrationBuilder.CreateIndex(name: "IX_ProductAlternatives_AlternativeProductId", table: "ProductAlternatives", column: "AlternativeProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Favorites");
            migrationBuilder.DropTable(name: "InsuranceNetworkServices");
            migrationBuilder.DropTable(name: "InsuranceProfiles");
            migrationBuilder.DropTable(name: "ProductAlternatives");
            migrationBuilder.DropTable(name: "InsuranceProviders");
            migrationBuilder.DropTable(name: "Products");
            migrationBuilder.DropTable(name: "AspNetUserRoles");
            migrationBuilder.DropTable(name: "AspNetRoles");
        }
    }
}