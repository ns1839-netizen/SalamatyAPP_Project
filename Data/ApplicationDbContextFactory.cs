
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SalamatyAPI.Data;
using System.IO;


public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // 1. Get the current directory of your project
        string basePath = Directory.GetCurrentDirectory();

        // 2. Build the configuration object to read appsettings.json
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json")
            .Build();

        // 3. Create a new DbContextOptionsBuilder
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // 4. Get the connection string from your appsettings.json
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // 5. Configure the optionsBuilder to use SQL Server with that connection string
        optionsBuilder.UseSqlServer(connectionString);

        // 6. Create and return a new instance of your ApplicationDbContext
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
