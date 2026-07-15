using BackPredictFinance.Common;

namespace BackPredictFinance.API.ProgramSubFiles
{
    public class ProgramServiceDeclarator
    {
        public static void ServicesDeclarator(IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddAuthAndUserServices()
                .AddMarketDataProviders(configuration)
                .AddPatternServices()
                .AddAnalysisServices()
                .AddClientFinanceServices()
                .AddAdminServices()
                .AddNotificationServices()
                .AddBackgroundJobs();
        }

        public static void ConfigureOptions(IServiceCollection services, IConfiguration configuration)
        {
            var env = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";

            services.AddOptions<FrontendOptions>()
                .Bind(configuration.GetSection("Frontend"))
                .Validate(opts =>
                {
                    if (string.IsNullOrWhiteSpace(opts.BaseUrl))
                        return false;

                    if (env == "Development" || env == "Testing")
                        return true;

                    return !opts.BaseUrl.Contains("localhost") && !opts.BaseUrl.Contains(".local");
                },
                "Frontend:BaseUrl must be a valid public URL (no localhost or .local) in non-Development environments")
                .ValidateOnStart();

            services.AddOptions<MarketDataOptions>()
                .Bind(configuration.GetSection("MarketData"))
                .ValidateOnStart();
        }
    }
}
