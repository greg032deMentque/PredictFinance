using BackPredictFinance.ViewModels.UserViewModels.AuthViewModels;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Reflection;

using BackPredictFinance.Tests.Infrastructure;

namespace BackPredictFinance.Tests.Authentication;

public sealed class AccountSecurityApiTests
{
    [Fact]
    public async Task ForgotPassword_Post_DelegatesToAccountService()
    {
        var accountService = TestInfrastructure.CreateAccountServiceMock();
        var request = new ForgotPasswordViewModel
        {
            Email = "user@example.com"
        };

        accountService
            .Setup(x => x.ForgotPassword(request.Email))
            .Returns(Task.CompletedTask);

        var controller = TestInfrastructure.CreateAccountController(accountService);

        var result = await controller.ForgotPassword(request);

        Assert.IsType<OkResult>(result);
        accountService.Verify(x => x.ForgotPassword(request.Email), Times.Once);
    }

    [Fact]
    public async Task ConfirmEmail_Post_DelegatesToAccountService_AndReturnsNoContent()
    {
        var accountService = TestInfrastructure.CreateAccountServiceMock();
        var request = new ConfirmEmailViewModel
        {
            Email = "user@example.com",
            Token = "token"
        };

        accountService
            .Setup(x => x.ConfirmEmailAsync(request))
            .Returns(Task.CompletedTask);

        var controller = TestInfrastructure.CreateAccountController(accountService);

        var result = await controller.ConfirmEmail(request);

        Assert.IsType<NoContentResult>(result);
        accountService.Verify(x => x.ConfirmEmailAsync(request), Times.Once);
    }

    [Fact]
    public async Task ResendConfirmationEmail_Post_DelegatesToAccountService_AndReturnsNoContent()
    {
        var accountService = TestInfrastructure.CreateAccountServiceMock();
        var request = new ResendConfirmationEmailViewModel
        {
            Email = "user@example.com"
        };

        accountService
            .Setup(x => x.ResendConfirmationEmailAsync(request.Email))
            .Returns(Task.CompletedTask);

        var controller = TestInfrastructure.CreateAccountController(accountService);

        var result = await controller.ResendConfirmationEmail(request);

        Assert.IsType<NoContentResult>(result);
        accountService.Verify(x => x.ResendConfirmationEmailAsync(request.Email), Times.Once);
    }

    [Fact]
    public void AccountController_ShouldNotExposeGetForgotPasswordRoute()
    {
        var forgotPasswordActions = typeof(BackPredictFinance.API.Controllers.AccountController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => string.Equals(method.Name, nameof(BackPredictFinance.API.Controllers.AccountController.ForgotPassword), StringComparison.Ordinal))
            .ToArray();

        Assert.Contains(forgotPasswordActions, method => method.GetCustomAttribute<HttpPostAttribute>() is not null);
        Assert.DoesNotContain(forgotPasswordActions, method => method.GetCustomAttribute<HttpGetAttribute>() is not null);
    }
}
