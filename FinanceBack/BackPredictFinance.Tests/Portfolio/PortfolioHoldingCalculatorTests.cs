using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.ClientFinanceServices;

namespace BackPredictFinance.Tests.Portfolio;

public sealed class PortfolioHoldingCalculatorTests
{
    private static AssetTransaction Tx(TransactionTypeEnum type, decimal quantity, decimal unitPrice, int dayOffset, decimal fees = 0m)
        => new()
        {
            TransactionType = type,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Fees = fees,
            TimestampUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(dayOffset)
        };

    [Fact]
    public void Compute_SegmentsQuantityPerTransactionSet()
    {
        // Même actif, deux portefeuilles : chaque jeu de transactions (déjà filtré par portfolioId
        // côté appelant) doit produire sa propre quantité, jamais l'agrégat des deux.
        var portfolioA = new[] { Tx(TransactionTypeEnum.Buy, 10m, 100m, 0) };
        var portfolioB = new[] { Tx(TransactionTypeEnum.Buy, 4m, 100m, 0) };

        var holdingA = PortfolioHoldingCalculator.Compute(portfolioA);
        var holdingB = PortfolioHoldingCalculator.Compute(portfolioB);

        Assert.Equal(10m, holdingA.Quantity);
        Assert.Equal(4m, holdingB.Quantity);
    }

    [Fact]
    public void Compute_AppliesFifoOnSellAcrossLots()
    {
        // Achat 10@100 puis 10@120, vente 5 -> FIFO consomme le lot le plus ancien.
        // Reste 5@100 + 10@120 = 15 titres ; investi = 5*100 + 10*120 = 1700 ; PRU = 1700/15.
        var transactions = new[]
        {
            Tx(TransactionTypeEnum.Buy, 10m, 100m, 0),
            Tx(TransactionTypeEnum.Buy, 10m, 120m, 1),
            Tx(TransactionTypeEnum.Sell, 5m, 130m, 2)
        };

        var holding = PortfolioHoldingCalculator.Compute(transactions);

        Assert.Equal(15m, holding.Quantity);
        Assert.Equal(1700m, holding.InvestedAmount);
        Assert.Equal(decimal.Round(1700m / 15m, 4), holding.AverageBuyPrice);
    }

    [Fact]
    public void Compute_FallsBackWithoutThrowing_WhenSellExceedsStock()
    {
        // Historique incohérent (vente > stock disponible) : le FIFO ne peut pas s'appliquer,
        // le repli net simple doit produire un résultat sans jeter.
        var transactions = new[]
        {
            Tx(TransactionTypeEnum.Buy, 5m, 100m, 0),
            Tx(TransactionTypeEnum.Sell, 8m, 110m, 1)
        };

        var holding = PortfolioHoldingCalculator.Compute(transactions);

        Assert.Equal(0m, holding.Quantity);
        Assert.True(holding.InvestedAmount >= 0m);
    }
}
