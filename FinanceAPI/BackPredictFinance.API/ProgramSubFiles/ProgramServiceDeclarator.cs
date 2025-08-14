using BackPredictFinance.Services;
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
            // --- API & Intégrations externes ---
            services.AddScoped<UserAssetService>()
                    .AddScoped<UserRoleDataService>()
                    .AddScoped<EmailService>()
                    .AddScoped<UserService>();

        }

    }
}
