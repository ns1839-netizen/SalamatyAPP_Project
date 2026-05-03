using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salamaty.API.Migrations
{
    public partial class FinalDatabaseFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // امسحي الأوامر اللي هنا تماماً عشان هي موجودة فعلياً في الداتابيز
            // لا تضعي أي migrationBuilder.AddColumn هنا
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // امسحي الأوامر اللي هنا أيضاً
        }
    }
}