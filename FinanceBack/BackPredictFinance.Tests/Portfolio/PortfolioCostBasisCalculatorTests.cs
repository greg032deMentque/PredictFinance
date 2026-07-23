using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.ClientFinanceServices.PortfolioCostBasis;

namespace BackPredictFinance.Tests.Portfolio;

public sealed class PortfolioCostBasisCalculatorTests
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
    public void Compute_NoTransactions_ReturnsEmptyConsistentResult()
    {
        var result = PortfolioCostBasisCalculator.Compute([]);

        Assert.Equal(0m, result.QuantityHeld);
        Assert.Null(result.AverageUnitCost);
        Assert.Equal(0m, result.InvestedAmount);
        Assert.Empty(result.RealizedSales);
        Assert.True(result.IsHistoryConsistent);
    }

    [Fact]
    public void Compute_SegmentsQuantityPerTransactionSet()
    {
        // Même actif, deux jeux de transactions (déjà filtrés par portfolioId côté appelant) :
        // chacun produit sa propre quantité, jamais l'agrégat des deux.
        var portfolioA = new[] { Tx(TransactionTypeEnum.Buy, 10m, 100m, 0) };
        var portfolioB = new[] { Tx(TransactionTypeEnum.Buy, 4m, 100m, 0) };

        var resultA = PortfolioCostBasisCalculator.Compute(portfolioA);
        var resultB = PortfolioCostBasisCalculator.Compute(portfolioB);

        Assert.Equal(10m, resultA.QuantityHeld);
        Assert.Equal(4m, resultB.QuantityHeld);
    }

    [Fact]
    public void Compute_AppliesWeightedAverageCostAcrossBuys_OnSell()
    {
        // PMP (méthode fiscale française, BOI-RPPM-PVBMI-20-10-20) : achat 10@100 puis 10@120
        // -> PMP = (10*100+10*120)/20 = 110. Vente 5@130 : reste 15 titres à 110, plus-value
        // réalisée = 5*(130-110) = 100.
        var transactions = new[]
        {
            Tx(TransactionTypeEnum.Buy, 10m, 100m, 0),
            Tx(TransactionTypeEnum.Buy, 10m, 120m, 1),
            Tx(TransactionTypeEnum.Sell, 5m, 130m, 2)
        };

        var result = PortfolioCostBasisCalculator.Compute(transactions);

        Assert.Equal(15m, result.QuantityHeld);
        Assert.Equal(110m, result.AverageUnitCost);
        Assert.Equal(1650m, result.InvestedAmount);
        Assert.True(result.IsHistoryConsistent);
        var sale = Assert.Single(result.RealizedSales);
        Assert.Equal(5m, sale.Quantity);
        Assert.Equal(110m, sale.AvgCostAtSale);
        Assert.Equal(100m, sale.RealizedPnl);
    }

    [Fact]
    public void Compute_IncludesAcquisitionFeesInCost_AndDeductsDisposalFeesFromProceeds()
    {
        // Frais d'acquisition inclus au coût (BOI-RPPM-PVBMI-20-10-20-10) : achat 10@100 frais 10
        // -> coût unitaire = 100 + 10/10 = 101. Vente 5@120 frais 5 -> prix net = 120 - 5/5 = 119.
        // Plus-value = 5*(119-101) = 90.
        var transactions = new[]
        {
            Tx(TransactionTypeEnum.Buy, 10m, 100m, 0, fees: 10m),
            Tx(TransactionTypeEnum.Sell, 5m, 120m, 1, fees: 5m)
        };

        var result = PortfolioCostBasisCalculator.Compute(transactions);

        Assert.Equal(101m, result.AverageUnitCost);
        var sale = Assert.Single(result.RealizedSales);
        Assert.Equal(119m, sale.NetUnitSellPrice);
        Assert.Equal(90m, sale.RealizedPnl);
    }

    [Fact]
    public void Compute_ResetsAverageCost_WhenPositionFullyClosedThenReopened()
    {
        var transactions = new[]
        {
            Tx(TransactionTypeEnum.Buy, 10m, 100m, 0),
            Tx(TransactionTypeEnum.Sell, 10m, 110m, 1),
            Tx(TransactionTypeEnum.Buy, 5m, 200m, 2)
        };

        var result = PortfolioCostBasisCalculator.Compute(transactions);

        Assert.Equal(5m, result.QuantityHeld);
        Assert.Equal(200m, result.AverageUnitCost);
    }

    [Fact]
    public void Compute_CapsSellAndFlagsInconsistency_WhenSellExceedsTrackedStock()
    {
        // Historique incohérent (vente > stock disponible) : plafonnement sans exception,
        // historique marqué non fiable (CORR-02 — dégradation gracieuse, jamais de throw).
        var transactions = new[]
        {
            Tx(TransactionTypeEnum.Buy, 5m, 100m, 0),
            Tx(TransactionTypeEnum.Sell, 8m, 110m, 1)
        };

        var result = PortfolioCostBasisCalculator.Compute(transactions);

        Assert.Equal(0m, result.QuantityHeld);
        Assert.Null(result.AverageUnitCost);
        Assert.False(result.IsHistoryConsistent);
        var sale = Assert.Single(result.RealizedSales);
        Assert.Equal(5m, sale.Quantity);
    }
}
