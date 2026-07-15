using BackPredictFinance.Datas.Context;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BackPredictFinance.API.HealthChecks
{
    public sealed class DatabaseHealthCheck : IHealthCheck
    {
        private readonly FinanceDbContext _context;

        public DatabaseHealthCheck(FinanceDbContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("La base de données est joignable.")
                : HealthCheckResult.Unhealthy("La base de données est injoignable.");
        }
    }
}
