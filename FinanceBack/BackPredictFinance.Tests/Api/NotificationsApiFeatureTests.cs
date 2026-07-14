using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.ViewModels.NotificationViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using BackPredictFinance.Tests.Infrastructure;

namespace BackPredictFinance.Tests.Api;

public sealed class NotificationsApiFeatureTests : IClassFixture<ApiIntegrationTestFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly ApiIntegrationTestFactory _factory;

    public NotificationsApiFeatureTests(ApiIntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Notifications_GetList_ReturnsUnauthorized_ForAnonymous()
    {
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/api/Notifications/GetList");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Notifications_GetList_ReturnsCurrentUserNotifications_FilteredAndNewestFirst()
    {
        await SeedNotificationsAsync();
        var client = _factory.CreateAuthenticatedClient(ApiIntegrationTestFactory.StandardUserId, UserRoleEnum.User);

        var response = await client.GetAsync("/api/Notifications/GetList?category=Analysis&status=Unread&take=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<NotificationItemViewModel>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(2, payload!.Count);
        Assert.Equal(["notif-analysis-2", "notif-analysis-1"], payload.Select(x => x.NotificationId).ToArray());
        Assert.All(payload, item => Assert.Equal(ApiIntegrationTestFactory.StandardUserId, item.UserId));
        Assert.All(payload, item => Assert.Equal(NotificationCategoryEnum.Analysis, item.Category));
        Assert.All(payload, item => Assert.Equal(NotificationStatusEnum.Unread, item.Status));
    }

    [Fact]
    public async Task Notifications_MarkAsRead_UpdatesCurrentUserNotification()
    {
        await SeedNotificationsAsync();
        var client = _factory.CreateAuthenticatedClient(ApiIntegrationTestFactory.StandardUserId, UserRoleEnum.User);

        var response = await client.PostAsJsonAsync("/api/Notifications/MarkAsRead", new MarkNotificationAsReadRequestViewModel
        {
            NotificationId = "notif-analysis-1"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<NotificationItemViewModel>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal("notif-analysis-1", payload!.NotificationId);
        Assert.Equal(NotificationStatusEnum.Read, payload.Status);
        Assert.NotNull(payload.ReadAtUtc);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        var persistedNotification = await dbContext.UserNotifications.FindAsync("notif-analysis-1");
        Assert.NotNull(persistedNotification);
        Assert.Equal(NotificationStatusEnum.Read, persistedNotification!.Status);
        Assert.NotNull(persistedNotification.ReadAtUtc);
    }

    private async Task SeedNotificationsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

        // The factory is a class fixture, so every test in this class shares one in-memory
        // database, and MarkAsRead flips notif-analysis-1 to Read. Seeding only when the rows
        // are absent would leave that mutation in place and make GetList depend on execution
        // order. Rebuild the seeded state on every call instead.
        var seeded = await dbContext.UserNotifications.ToListAsync();
        if (seeded.Count > 0)
        {
            dbContext.UserNotifications.RemoveRange(seeded);
            await dbContext.SaveChangesAsync();
        }

        await dbContext.UserNotifications.AddRangeAsync(
            new UserNotification
            {
                NotificationId = "notif-analysis-1",
                UserId = ApiIntegrationTestFactory.StandardUserId,
                Category = NotificationCategoryEnum.Analysis,
                Status = NotificationStatusEnum.Unread,
                Title = "Nouvelle analyse disponible",
                Summary = "Une analyse gouvernee est disponible pour AIRP.",
                TargetScreen = NotificationTargetScreenEnum.AnalysisResult,
                TargetEntityId = "analysis-1",
                CreatedAtUtc = new DateTime(2026, 4, 11, 8, 0, 0, DateTimeKind.Utc)
            },
            new UserNotification
            {
                NotificationId = "notif-analysis-2",
                UserId = ApiIntegrationTestFactory.StandardUserId,
                Category = NotificationCategoryEnum.Analysis,
                Status = NotificationStatusEnum.Unread,
                Title = "Point de vigilance sur une analyse",
                Summary = "Un scenario compatible reste a surveiller.",
                TargetScreen = NotificationTargetScreenEnum.InstrumentDetail,
                TargetEntityId = "AIRP",
                CreatedAtUtc = new DateTime(2026, 4, 12, 8, 0, 0, DateTimeKind.Utc)
            },
            new UserNotification
            {
                NotificationId = "notif-account-1",
                UserId = ApiIntegrationTestFactory.StandardUserId,
                Category = NotificationCategoryEnum.Account,
                Status = NotificationStatusEnum.Read,
                Title = "Action compte deja lue",
                Summary = "Votre profil a ete revu.",
                TargetScreen = NotificationTargetScreenEnum.Account,
                TargetEntityId = null,
                ReadAtUtc = new DateTime(2026, 4, 12, 9, 0, 0, DateTimeKind.Utc),
                CreatedAtUtc = new DateTime(2026, 4, 12, 7, 0, 0, DateTimeKind.Utc)
            },
            new UserNotification
            {
                NotificationId = "notif-other-user",
                UserId = ApiIntegrationTestFactory.TargetUserId,
                Category = NotificationCategoryEnum.Analysis,
                Status = NotificationStatusEnum.Unread,
                Title = "Autre user",
                Summary = "Ne doit pas remonter dans la liste.",
                TargetScreen = NotificationTargetScreenEnum.AnalysisResult,
                TargetEntityId = "analysis-other",
                CreatedAtUtc = new DateTime(2026, 4, 12, 10, 0, 0, DateTimeKind.Utc)
            });

        await dbContext.SaveChangesAsync();
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
