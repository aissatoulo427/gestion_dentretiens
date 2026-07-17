using Gestion_dentretiens.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Gestion_dentretiens.Api;

/// <summary>
/// Utilisée uniquement par les outils EF Core (dotnet ef migrations / database update).
/// Construit le DbContext à partir de appsettings.json, sans démarrer l'application.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(configuration.GetConnectionString("AppDb"))
            .Options;

        return new AppDbContext(options);
    }
}
