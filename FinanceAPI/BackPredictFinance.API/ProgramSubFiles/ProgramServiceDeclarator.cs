using BackPredictFinance.Services;
using BackPredictFinance.Services.AuthServices;
using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.Services.PythonServices;
using BackPredictFinance.Services.TwelveDataServices;
using BackPredictFinance.Services.UserServices;

namespace BackPredictFinance.API.ProgramSubFiles
{
    /*

 // 📁 Chemin racine de tes services
string rootPath = @"C:\Users\gregd\Documents\Projects\Wagram-ONE\BackWagramOne\Wagram.ONE.Services"; // ⚠️ modifie ce chemin

var serviceFiles = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories)
.Where(f => !Path.GetFileName(f).StartsWith("I") && !f.Contains("Interfaces")) // exclut les interfaces
.Select(Path.GetFileNameWithoutExtension)
.Distinct()
.OrderBy(f => f)
.ToList();

Console.WriteLine("// --- Services auto-générés ---");
foreach (var service in serviceFiles)
{
Console.WriteLine($"services.AddScoped<{service}>();");
}


 */

    public class ProgramServiceDeclarator
    {
        public static void ServicesDeclarator(IServiceCollection services)
        {

            services.AddScoped<ILogService, LogService>();
            services.AddScoped<IPathService, PathService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IJwtGeneratorService, JwtGeneratorService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IUserAssetService, UserAssetService>();
            services.AddScoped<IUserRoleDataService, UserRoleDataService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IPatternCatalogService, PatternCatalogService>();
            services.AddScoped<IPythonApiService, PythonApiService>();
            services.AddScoped<IIAStatusService, IAStatusService>();
            services.AddScoped<IAssetService, AssetService>();
            services.AddScoped<AnalyticService>();
            services.AddScoped<ITickerService, TickerService>();
            services.AddScoped<ITradingRecommendationService, TradingRecommendationService>();
            services.AddScoped<IClientFinanceService, ClientFinanceService>();

        }

    }
}
