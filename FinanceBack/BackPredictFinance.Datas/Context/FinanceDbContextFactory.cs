using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BackPredictFinance.Datas.Context
{
    /// <summary>
    /// Crée <see cref="FinanceDbContext"/> pour les outils EF Core en design-time,
    /// sans démarrer le runtime HTTP de l'API.
    /// </summary>
    public sealed class FinanceDbContextFactory : IDesignTimeDbContextFactory<FinanceDbContext>
    {
        /// <summary>
        /// Charge la configuration du projet API et construit un contexte utilisable
        /// par les commandes de migration et de mise à jour de base.
        /// </summary>
        public FinanceDbContext CreateDbContext(string[] args)
        {
            var configuration = BuildConfiguration();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing for design-time FinanceDbContext creation.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<FinanceDbContext>();
            optionsBuilder.UseSqlServer(connectionString, sql =>
                sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));

            return new FinanceDbContext(optionsBuilder.Options, new HttpContextAccessor());
        }

        private static IConfiguration BuildConfiguration()
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            var apiProjectPath = ResolveApiProjectPath();

            return new ConfigurationBuilder()
                .SetBasePath(apiProjectPath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

        private static string ResolveApiProjectPath()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var candidates = new[]
            {
                Path.GetFullPath(Path.Combine(currentDirectory, "FinanceAPI", "BackPredictFinance.API")),
                Path.GetFullPath(Path.Combine(currentDirectory, "..", "BackPredictFinance.API")),
                Path.GetFullPath(Path.Combine(currentDirectory, "..", "..", "BackPredictFinance.API"))
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(Path.Combine(candidate, "appsettings.json")))
                {
                    return candidate;
                }
            }

            throw new DirectoryNotFoundException("Unable to resolve BackPredictFinance.API configuration directory for design-time FinanceDbContext creation.");
        }
    }
}
