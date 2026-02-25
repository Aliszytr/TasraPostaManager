// File: Data/AppDbContextFactory.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TasraPostaManager.Data
{
    // EF Tools (Add-Migration / Update-Database) bu sınıfı kullanarak
    // design-time'da DbContext oluşturur ve appsettings.json'dan connection string'i okur.
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            var cs = configuration.GetConnectionString("SqlConnection")
                     ?? throw new InvalidOperationException("SqlConnection bulunamadı.");

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(cs)
                .Options;

            return new AppDbContext(options);
        }
    }
}
