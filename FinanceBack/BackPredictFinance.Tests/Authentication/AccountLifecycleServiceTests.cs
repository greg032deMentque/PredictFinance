using AutoMapper;
using BackPredictFinance.Common;
using BackPredictFinance.Common.Auth;
using BackPredictFinance.Common.Jwt;
using BackPredictFinance.Common.Localization;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services;
using BackPredictFinance.Services.AuthServices;
using BackPredictFinance.Services.UserServices;
using BackPredictFinance.ViewModels.UserViewModels;
using BackPredictFinance.ViewModels.UserViewModels.AuthViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;

namespace BackPredictFinance.Tests.Authentication;

public sealed class AccountLifecycleServiceTests
{
    [Fact]
    public async Task RegisterPublic_CreatesActiveUnconfirmedUser_AndBlocksLoginUntilConfirmation()
    {
        await using var testContext = await AccountServiceTestContext.CreateAsync(
            jwtSetup: mock =>
            {
                mock.Setup(x => x.GenerateJwtToken(It.IsAny<User>())).ReturnsAsync("access-token");
                mock.Setup(x => x.GenerateUserRefreshToken(It.IsAny<User>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new RefreshTokenResult("refresh-token", DateTime.UtcNow.AddDays(7)));
            },
            emailSetup: mock =>
            {
                mock.Setup(x => x.SendEmail(
                        "user@example.com",
                        It.Is<string>(subject => subject.Contains("Confirmez", StringComparison.Ordinal)),
                        It.Is<string>(body => body.Contains("/register?email=user%40example.com&token=", StringComparison.Ordinal)),
                        true,
                        null))
                    .Returns(Task.CompletedTask);
            });

        var userService = testContext.Services.GetRequiredService<IUserService>();
        var accountService = testContext.Services.GetRequiredService<IAccountService>();
        var userManager = testContext.Services.GetRequiredService<UserManager<User>>();

        var response = await userService.RegisterPublic(new PublicSignupRequestViewModel
        {
            Email = "user@example.com",
            Password = "Password1",
            ConfirmPassword = "Password1"
        });

        var createdUser = await userManager.FindByEmailAsync("user@example.com");
        Assert.NotNull(createdUser);
        Assert.True(createdUser.IsActive);
        Assert.False(createdUser.EmailConfirmed);
        Assert.True(await userManager.IsInRoleAsync(createdUser, UserRoleEnum.User.ToString()));
        Assert.Equal("user@example.com", response.Email);
        Assert.False(response.CanLogin);
        Assert.True(response.RequiresEmailConfirmation);

        var exception = await Assert.ThrowsAsync<CustomException>(() => accountService.Login(new LoginViewModel
        {
            Email = "user@example.com",
            Password = "Password1"
        })!);

        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        Assert.Equal("Confirmez votre adresse email avant de vous connecter.", exception.FrontMessage);
        testContext.EmailServiceMock.VerifyAll();
    }

    [Fact]
    public async Task RegisterPublic_ReturnsNonDisclosingResponseWhenEmailAlreadyExists()
    {
        await using var testContext = await AccountServiceTestContext.CreateAsync();

        var userService = testContext.Services.GetRequiredService<IUserService>();
        var userManager = testContext.Services.GetRequiredService<UserManager<User>>();

        var existingUser = new User
        {
            UserName = "user@example.com",
            Email = "user@example.com",
            IsActive = true,
            EmailConfirmed = true,
            RefreshToken = string.Empty
        };

        var createResult = await userManager.CreateAsync(existingUser, "Password1");
        Assert.True(createResult.Succeeded);
        await userManager.AddToRoleAsync(existingUser, UserRoleEnum.User.ToString());

        var response = await userService.RegisterPublic(new PublicSignupRequestViewModel
        {
            Email = "user@example.com",
            Password = "Password1",
            ConfirmPassword = "Password1"
        });

        Assert.Equal("user@example.com", response.Email);
        Assert.False(response.CanLogin);
        Assert.True(response.RequiresEmailConfirmation);

        var userCount = await userManager.Users.CountAsync(x => x.Email == "user@example.com");
        Assert.Equal(1, userCount);
    }

    [Fact]
    public async Task ConfirmEmail_ConfirmsUser_AndAllowsLogin()
    {
        await using var testContext = await AccountServiceTestContext.CreateAsync(
            jwtSetup: mock =>
            {
                mock.Setup(x => x.GenerateJwtToken(It.IsAny<User>())).ReturnsAsync("access-token");
                mock.Setup(x => x.GenerateUserRefreshToken(It.IsAny<User>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new RefreshTokenResult("refresh-token", DateTime.UtcNow.AddDays(7)));
            },
            emailSetup: mock =>
            {
                mock.Setup(x => x.SendEmail(
                        "user@example.com",
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        true,
                        null))
                    .Returns(Task.CompletedTask);
            });

        var userService = testContext.Services.GetRequiredService<IUserService>();
        var accountService = testContext.Services.GetRequiredService<IAccountService>();
        var userManager = testContext.Services.GetRequiredService<UserManager<User>>();

        await userService.RegisterPublic(new PublicSignupRequestViewModel
        {
            Email = "user@example.com",
            Password = "Password1",
            ConfirmPassword = "Password1"
        });

        var createdUser = await userManager.FindByEmailAsync("user@example.com");
        Assert.NotNull(createdUser);
        var token = await userManager.GenerateEmailConfirmationTokenAsync(createdUser!);

        await accountService.ConfirmEmailAsync(new ConfirmEmailViewModel
        {
            Email = "user@example.com",
            Token = token
        });

        var loginResult = await accountService.Login(new LoginViewModel
        {
            Email = "user@example.com",
            Password = "Password1"
        });

        Assert.NotNull(loginResult);
        Assert.Equal("refresh-token", loginResult!.RefreshToken);
        Assert.True((await userManager.FindByEmailAsync("user@example.com"))!.EmailConfirmed);
    }

    [Fact]
    public async Task Login_ThrowsForbiddenForInactiveUser()
    {
        await using var testContext = await AccountServiceTestContext.CreateAsync(jwtSetup: mock =>
        {
            mock.Setup(x => x.GenerateJwtToken(It.IsAny<User>())).ReturnsAsync("access-token");
            mock.Setup(x => x.GenerateUserRefreshToken(It.IsAny<User>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RefreshTokenResult("refresh-token", DateTime.UtcNow.AddDays(7)));
        });

        var accountService = testContext.Services.GetRequiredService<IAccountService>();
        var userManager = testContext.Services.GetRequiredService<UserManager<User>>();

        var user = new User
        {
            UserName = "inactive@example.com",
            Email = "inactive@example.com",
            IsActive = false,
            EmailConfirmed = true,
            RefreshToken = string.Empty
        };

        var createResult = await userManager.CreateAsync(user, "Password1");
        Assert.True(createResult.Succeeded);
        await userManager.AddToRoleAsync(user, UserRoleEnum.User.ToString());

        var exception = await Assert.ThrowsAsync<CustomException>(() => accountService.Login(new LoginViewModel
        {
            Email = "inactive@example.com",
            Password = "Password1"
        })!);

        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        Assert.Equal("Votre accès est indisponible.", exception.FrontMessage);
    }

    [Fact]
    public async Task ForgotPassword_SendsResetEmailForExistingUnconfirmedUser()
    {
        await using var testContext = await AccountServiceTestContext.CreateAsync(emailSetup: mock =>
        {
            mock.Setup(x => x.SendEmailPasswordReset(
                    "user@example.com",
                    It.Is<string>(link => link.Contains("reset-password", StringComparison.Ordinal)
                        && link.Contains("email=user%40example.com", StringComparison.Ordinal))))
                .Returns(Task.CompletedTask);
        });

        var accountService = testContext.Services.GetRequiredService<IAccountService>();
        var userManager = testContext.Services.GetRequiredService<UserManager<User>>();

        var user = new User
        {
            UserName = "user@example.com",
            Email = "user@example.com",
            IsActive = true,
            EmailConfirmed = false,
            RefreshToken = string.Empty
        };

        var createResult = await userManager.CreateAsync(user, "Password1");
        Assert.True(createResult.Succeeded);
        await userManager.AddToRoleAsync(user, UserRoleEnum.User.ToString());

        await accountService.ForgotPassword("user@example.com");

        testContext.EmailServiceMock.VerifyAll();
    }

    [Fact]
    public async Task Login_AfterFiveFailedAttempts_LocksTemporarily_AndUnlocksAfterWindow()
    {
        await using var testContext = await AccountServiceTestContext.CreateAsync(jwtSetup: mock =>
        {
            mock.Setup(x => x.GenerateJwtToken(It.IsAny<User>())).ReturnsAsync("access-token");
            mock.Setup(x => x.GenerateUserRefreshToken(It.IsAny<User>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RefreshTokenResult("refresh-token", DateTime.UtcNow.AddDays(7)));
        });

        var accountService = testContext.Services.GetRequiredService<IAccountService>();
        var userManager = testContext.Services.GetRequiredService<UserManager<User>>();

        var user = new User
        {
            UserName = "lockout@example.com",
            Email = "lockout@example.com",
            IsActive = true,
            EmailConfirmed = true,
            RefreshToken = string.Empty
        };

        var createResult = await userManager.CreateAsync(user, "Password1");
        Assert.True(createResult.Succeeded);
        await userManager.AddToRoleAsync(user, UserRoleEnum.User.ToString());

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var result = await accountService.Login(new LoginViewModel
            {
                Email = "lockout@example.com",
                Password = "WrongPassword1"
            });

            Assert.Null(result);
        }

        var lockedOutUser = await userManager.FindByEmailAsync("lockout@example.com");
        Assert.NotNull(lockedOutUser);
        Assert.True(await userManager.IsLockedOutAsync(lockedOutUser!));

        var lockoutEnd = await userManager.GetLockoutEndDateAsync(lockedOutUser!);
        Assert.NotNull(lockoutEnd);
        Assert.NotEqual(DateTimeOffset.MaxValue, lockoutEnd!.Value);
        Assert.True(lockoutEnd.Value < DateTimeOffset.UtcNow.AddHours(1));

        var resultWhileLocked = await accountService.Login(new LoginViewModel
        {
            Email = "lockout@example.com",
            Password = "Password1"
        });

        Assert.Null(resultWhileLocked);

        await userManager.SetLockoutEndDateAsync(lockedOutUser!, DateTimeOffset.UtcNow.AddMinutes(-1));

        var resultAfterWindow = await accountService.Login(new LoginViewModel
        {
            Email = "lockout@example.com",
            Password = "Password1"
        });

        Assert.NotNull(resultAfterWindow);
        Assert.Equal("refresh-token", resultAfterWindow!.RefreshToken);
    }

    [Fact]
    public async Task ResendConfirmationEmail_ReturnsSilently_ForUnknownOrConfirmedAccounts()
    {
        await using var testContext = await AccountServiceTestContext.CreateAsync();

        var accountService = testContext.Services.GetRequiredService<IAccountService>();
        var userManager = testContext.Services.GetRequiredService<UserManager<User>>();

        await accountService.ResendConfirmationEmailAsync("unknown@example.com");

        var confirmedUser = new User
        {
            UserName = "confirmed@example.com",
            Email = "confirmed@example.com",
            IsActive = true,
            EmailConfirmed = true,
            RefreshToken = string.Empty
        };

        var createResult = await userManager.CreateAsync(confirmedUser, "Password1");
        Assert.True(createResult.Succeeded);
        await userManager.AddToRoleAsync(confirmedUser, UserRoleEnum.User.ToString());

        await accountService.ResendConfirmationEmailAsync("confirmed@example.com");
    }

    private sealed class AccountServiceTestContext : IAsyncDisposable
    {
        private AccountServiceTestContext(ServiceProvider rootProvider, AsyncServiceScope scope, Mock<IEmailService> emailServiceMock)
        {
            RootProvider = rootProvider;
            Scope = scope;
            EmailServiceMock = emailServiceMock;
        }

        public ServiceProvider RootProvider { get; }
        public AsyncServiceScope Scope { get; }
        public Mock<IEmailService> EmailServiceMock { get; }
        public IServiceProvider Services => Scope.ServiceProvider;

        public static async Task<AccountServiceTestContext> CreateAsync(
            Action<Mock<IJwtGeneratorService>>? jwtSetup = null,
            Action<Mock<IEmailService>>? emailSetup = null)
        {
            var services = new ServiceCollection();
            var emailServiceMock = new Mock<IEmailService>(MockBehavior.Strict);
            var jwtGeneratorMock = new Mock<IJwtGeneratorService>(MockBehavior.Strict);

            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:BaseUrl"] = "http://localhost:4200"
                })
                .Build());
            services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext()
            });
            services.AddSingleton<IMapper>(new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(typeof(UserViewModel).Assembly);
            }, NullLoggerFactory.Instance).CreateMapper());
            services.AddSingleton(Mock.Of<IStringLocalizer<SharedResources>>());
            services.AddScoped<ILogService>(_ => Mock.Of<ILogService>());
            services.AddSingleton(emailServiceMock.Object);
            services.AddSingleton(jwtGeneratorMock.Object);
            services.AddSingleton(Mock.Of<IUserRoleDataService>());

            services.AddDbContext<FinanceDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));

            services
                .AddIdentity<User, IdentityRole>(options =>
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequiredLength = 6;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.AllowedForNewUsers = true;
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<FinanceDbContext>()
                .AddDefaultTokenProviders();

            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IUserService, UserService>();

            jwtSetup?.Invoke(jwtGeneratorMock);
            emailSetup?.Invoke(emailServiceMock);

            var provider = services.BuildServiceProvider();
            var scope = provider.CreateAsyncScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            foreach (var roleName in Enum.GetNames(typeof(UserRoleEnum)))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            return new AccountServiceTestContext(provider, scope, emailServiceMock);
        }

        public async ValueTask DisposeAsync()
        {
            await Scope.DisposeAsync();
            await RootProvider.DisposeAsync();
        }
    }
}
