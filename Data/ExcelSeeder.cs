using ClosedXML.Excel;
using Salamaty.API.Models;

namespace SalamatyAPI.Data
{
    public static class ExcelSeeder
    {
        public static void SeedProductsFromExcel(ApplicationDbContext context, string filePath)
        {
            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1); // أول شيت
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // نتخطى الهيدر

            foreach (var row in rows)
            {
                var product = new Product
                {
                    Name = row.Cell(1).GetString() ?? string.Empty,
                    Price = row.Cell(2).GetValue<decimal>(),
                    SideEffect = row.Cell(3).GetString() ?? string.Empty,
                    Description = row.Cell(4).GetString() ?? string.Empty,
                    Uses = row.Cell(5).GetString() ?? string.Empty,
                    Alternatives = row.Cell(6).GetString() ?? string.Empty,
                    Category = row.Cell(7).GetString() ?? string.Empty,
                    ImageUrl = row.Cell(8).GetString() ?? string.Empty
                };

                if (!context.Products.Any(p => p.Name == product.Name))
                    context.Products.Add(product);
            }

            context.SaveChanges();
        }
    }
}
