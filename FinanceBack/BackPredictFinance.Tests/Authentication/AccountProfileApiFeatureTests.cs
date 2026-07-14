using BackPredictFinance.ViewModels.UserViewModels;
using Moq;

using BackPredictFinance.Tests.Infrastructure;

namespace BackPredictFinance.Tests.Authentication;

public sealed class AccountProfileApiFeatureTests
{
    [Fact]
    public async Task Profile_ReturnsCurrentUserProfile()
    {
        var accountService = TestInfrastructure.CreateAccountServiceMock();
        var currentSessionService = TestInfrastructure.CreateCurrentUserSessionServiceMock();
        var userService = TestInfrastructure.CreateUserServiceMock();
        var expected = new CurrentUserProfileViewModel
        {
            Email = "user@example.com",
            FirstName = "Jean",
            LastName = "Dupont",
            PhoneNumber = "0102030405"
        };
        userService.Setup(x => x.GetCurrentUserProfile(It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = TestInfrastructure.CreateAccountController(accountService, currentSessionService, userService);

        var result = await controller.Profile(CancellationToken.None);

        var payload = TestInfrastructure.AssertOkObject<CurrentUserProfileViewModel>(result);
        Assert.Equal("user@example.com", payload.Email);
        userService.Verify(x => x.GetCurrentUserProfile(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProfileUpdate_ReturnsUpdatedProfile()
    {
        var accountService = TestInfrastructure.CreateAccountServiceMock();
        var currentSessionService = TestInfrastructure.CreateCurrentUserSessionServiceMock();
        var userService = TestInfrastructure.CreateUserServiceMock();
        var request = new UpdateCurrentUserProfileRequestViewModel
        {
            FirstName = "Jeanne",
            LastName = "Dupont",
            PhoneNumber = "0600000000"
        };
        var expected = new CurrentUserProfileViewModel
        {
            Email = "user@example.com",
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber
        };
        userService.Setup(x => x.UpdateCurrentUserProfile(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = TestInfrastructure.CreateAccountController(accountService, currentSessionService, userService);

        var result = await controller.Profile(request, CancellationToken.None);

        var payload = TestInfrastructure.AssertOkObject<CurrentUserProfileViewModel>(result);
        Assert.Equal("Jeanne", payload.FirstName);
        userService.Verify(x => x.UpdateCurrentUserProfile(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
