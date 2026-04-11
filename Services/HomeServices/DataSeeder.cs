using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Salamaty.API.Models.HomeModels;

public static class DataSeeder
{
    public static List<Banner> LoadBannersFromCsv()
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "DataResources", "BannerData.xlsx - Sheet1.csv");

        if (!File.Exists(filePath)) return new List<Banner>();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            // دي عشان يقرأ id كـ Id و title كـ Title
            PrepareHeaderForMatch = args => args.Header.ToLower().Trim(),
            HeaderValidated = null,
            MissingFieldFound = null
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);
        return csv.GetRecords<Banner>().ToList();
    }
}