using BackPredictFinance.ViewModels.ClientFinanceViewModels.Instruments;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Snapshots;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Contact;
using Microsoft.AspNetCore.Mvc;
using Moq;

using BackPredictFinance.Tests.Infrastructure;

namespace BackPredictFinance.Tests.Api;

public sealed class ClientFinanceMilestone3ApiFeatureTests
{
    [Fact]
    public async Task GetInstrumentDetail_ReturnsNotFoundWhenInstrumentIsMissing()
    {
        var instrumentDetailService = TestInfrastructure.CreateInstrumentDetailServiceMock();
        instrumentDetailService.Setup(x => x.GetInstrumentDetailAsync("AIR.PA", It.IsAny<CancellationToken>())).ReturnsAsync((InstrumentDetailViewModel?)null);
        var controller = TestInfrastructure.CreateClientFinanceMarketController(instrumentDetailServiceMock: instrumentDetailService);

        var result = await controller.GetInstrumentDetail("AIR.PA", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        instrumentDetailService.Verify(x => x.GetInstrumentDetailAsync("AIR.PA", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Contact_ForwardsRequestToService_AndReturnsNoContent()
    {
        var contactService = TestInfrastructure.CreateContactServiceMock();
        var request = new ContactSupportRequestViewModel
        {
            Subject = "Probleme watchlist",
            Message = "Je n arrive pas a ajouter une valeur."
        };
        contactService.Setup(x => x.SendSupportMessageAsync(request, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var controller = TestInfrastructure.CreateClientFinanceContactController(contactServiceMock: contactService);

        var result = await controller.Contact(request, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        contactService.Verify(x => x.SendSupportMessageAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompareSnapshots_ReturnsNotFoundWhenSnapshotPairIsMissing()
    {
        var snapshotComparisonService = TestInfrastructure.CreateSnapshotComparisonServiceMock();
        var request = new SnapshotComparisonRequestViewModel
        {
            LeftSnapshotId = "snapshot-left",
            RightSnapshotId = "snapshot-right"
        };
        snapshotComparisonService.Setup(x => x.CompareAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync((SnapshotComparisonViewModel?)null);
        var controller = TestInfrastructure.CreateClientFinanceAnalysisController(snapshotComparisonServiceMock: snapshotComparisonService);

        var result = await controller.CompareSnapshots(request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        snapshotComparisonService.Verify(x => x.CompareAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
