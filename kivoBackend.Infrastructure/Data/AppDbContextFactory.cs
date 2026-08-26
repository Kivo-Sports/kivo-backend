using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace kivoBackend.Infrastructure.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // Ensure EF tooling can use the same connection used at runtime.
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                var envPath = Path.Combine(Directory.GetCurrentDirectory(), "kivoBackend.Presentation", ".env");
                if (File.Exists(envPath))
                {
                    foreach (var line in File.ReadLines(envPath))
                    {
                        if (line.StartsWith("DB_CONNECTION_STRING=", StringComparison.Ordinal))
                        {
                            connectionString = line.Substring("DB_CONNECTION_STRING=".Length).Trim();
                            break;
                        }
                    }
                }
            }

            connectionString ??= "Server=localhost,1433;Database=KivoDb;User Id=sa;Password=Kivo@Sports2026!;TrustServerCertificate=True;";

            optionsBuilder.UseSqlServer(connectionString);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}