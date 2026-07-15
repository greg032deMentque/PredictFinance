using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.ClientFinanceServices;

namespace BackPredictFinance.Tests.Portfolio;

public sealed class PortfolioAggregationFilterTests
{
    private static AssetTransaction TxIn(PortfolioStatusEnum portfolioStatus, string portfolioId)
        => new()
        {
            PortfolioId = portfolioId,
            Portfolio = new Datas.Entities.Portfolio { Id = portfolioId, Status = portfolioStatus },
            TransactionType = TransactionTypeEnum.Buy,
            Quantity = 1m,
            UnitPrice = 100m
        };

    [Fact]
    public void ExcludeArchivedPortfolios_KeepsActive_DropsArchived()
    {
        // Un agrégat global doit ignorer les transactions des portefeuilles archivés
        // mais conserver celles des portefeuilles actifs.
        var transactions = new[]
        {
            TxIn(PortfolioStatusEnum.Active, "active-1"),
            TxIn(PortfolioStatusEnum.Archived, "archived-1"),
            TxIn(PortfolioStatusEnum.Active, "active-2")
        }.AsQueryable();

        var result = transactions.ExcludeArchivedPortfolios().ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, x => Assert.Equal(PortfolioStatusEnum.Active, x.Portfolio.Status));
        Assert.DoesNotContain(result, x => x.PortfolioId == "archived-1");
    }
}
