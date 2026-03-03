using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services;
using BackPredictFinance.Services.UserServices;
using BackPredictFinance.ViewModels.UserViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.API.Data
{
    public static class DatabaseUpdater
    {
        public static async Task RunDatabaseUpdate(WebApplication app)
        {
            await using var scope = app.Services.CreateAsyncScope();

            var serviceProvider = scope.ServiceProvider;
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = serviceProvider.GetRequiredService<ILogger<BaseService>>();
            var userService = serviceProvider.GetRequiredService<UserService>();
            var config = app.Services.GetRequiredService<IConfiguration>();
            var financeDbContext = serviceProvider.GetRequiredService<FinanceDbContext>();

            logger.LogInformation("Initializing finance data start...");

            logger.LogInformation("Database migrations (if needed) start...");
            await RunMigrationsAsync(financeDbContext, logger);
            await EnsureRefreshTokenStorageAsync(financeDbContext, logger);
            logger.LogInformation("Database migration phase done");

            await InitializeRoles(roleManager);
            await EnsureAdminAccountAsync(config["adminEmail"], config["adminPwd"], userManager, logger, userService);
            await EnsureSimpleUserAccountAsync(config["userEmail"], config["userPwd"], userManager, logger);

            logger.LogInformation("Initializing finance data done");
        }

        private static async Task RunMigrationsAsync(FinanceDbContext context, ILogger<BaseService> logger)
        {
            try
            {
                var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).Count();
                if (pendingMigrations > 0)
                {
                    logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations);
                    await context.Database.MigrateAsync();
                    logger.LogInformation("Migrations applied");
                }
                else
                {
                    logger.LogInformation("No migration needed");
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("pending changes", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Pending model changes detected. Falling back to EnsureCreated().");
                await context.Database.EnsureCreatedAsync();
            }
        }

        private static async Task EnsureRefreshTokenStorageAsync(FinanceDbContext context, ILogger<BaseService> logger)
        {
            const string script = """
                IF OBJECT_ID(N'[dbo].[RefreshTokens]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[RefreshTokens](
                        [Id] [bigint] IDENTITY(1,1) NOT NULL,
                        [UserId] [nvarchar](450) NOT NULL,
                        [TokenHash] [nvarchar](512) NOT NULL,
                        [ReplacedByTokenHash] [nvarchar](512) NULL,
                        [ExpiresAtUtc] [datetime2](7) NOT NULL,
                        [CreatedAtUtc] [datetime2](7) NOT NULL,
                        [RevokedAtUtc] [datetime2](7) NULL,
                        [DeviceId] [nvarchar](200) NULL,
                        [FingerprintHash] [nvarchar](512) NULL,
                        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY CLUSTERED ([Id] ASC),
                        CONSTRAINT [FK_RefreshTokens_AspNetUsers_UserId]
                            FOREIGN KEY([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
                    );
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RefreshTokens_TokenHash' AND object_id = OBJECT_ID(N'[dbo].[RefreshTokens]'))
                BEGIN
                    CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [dbo].[RefreshTokens]([TokenHash]);
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RefreshTokens_UserId' AND object_id = OBJECT_ID(N'[dbo].[RefreshTokens]'))
                BEGIN
                    CREATE INDEX [IX_RefreshTokens_UserId] ON [dbo].[RefreshTokens]([UserId]);
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RefreshTokens_ExpiresAtUtc' AND object_id = OBJECT_ID(N'[dbo].[RefreshTokens]'))
                BEGIN
                    CREATE INDEX [IX_RefreshTokens_ExpiresAtUtc] ON [dbo].[RefreshTokens]([ExpiresAtUtc]);
                END;
                """;

            try
            {
                await context.Database.ExecuteSqlRawAsync(script);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "RefreshTokens storage ensure failed; API auth refresh may not work.");
                throw;
            }
        }

        private static async Task EnsureAdminAccountAsync(
            string? email,
            string? password,
            UserManager<User> userManager,
            ILogger<BaseService> logger,
            UserService userService)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentNullException("email/password", "Missing admin credentials in configuration.");
            }

            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser is not null)
            {
                await EnsureSuperAdminRoleAsync(existingUser, userManager, logger);
                return;
            }

            logger.LogInformation("No admin account '{Email}' found. Creating one.", email);

            var userVm = new UserViewModel
            {
                Email = email,
                FirstName = "admin",
                LastName = "admin",
                Password = password,
                IsActive = true
            };

            await userService.Register(userVm);
            var createdUser = await userManager.FindByEmailAsync(email);
            if (createdUser is not null)
            {
                await EnsureSuperAdminRoleAsync(createdUser, userManager, logger);
            }
            logger.LogInformation("Admin account created");
        }

        private static async Task EnsureSuperAdminRoleAsync(
            User user,
            UserManager<User> userManager,
            ILogger<BaseService> logger)
        {
            var superAdminRole = UserRoleEnum.SuperAdmin.ToString();
            if (await userManager.IsInRoleAsync(user, superAdminRole))
            {
                return;
            }

            var currentRoles = await userManager.GetRolesAsync(user);
            if (currentRoles.Count > 0)
            {
                await userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            await userManager.AddToRoleAsync(user, superAdminRole);
            logger.LogInformation("User {Email} set as {Role}", user.Email, superAdminRole);
        }

        private static async Task EnsureSimpleUserAccountAsync(
            string? email,
            string? password,
            UserManager<User> userManager,
            ILogger<BaseService> logger)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentNullException("email/password", "Missing simple user credentials in configuration.");
            }

            var userRole = UserRoleEnum.User.ToString();
            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser is null)
            {
                var newUser = new User
                {
                    UserName = email,
                    Email = email,
                    FirstName = "user",
                    LastName = "user",
                    IsActive = true,
                    RefreshToken = string.Empty
                };

                var createResult = await userManager.CreateAsync(newUser, password);
                if (!createResult.Succeeded)
                {
                    var reason = string.Join(" | ", createResult.Errors.Select(x => x.Description));
                    throw new InvalidOperationException($"Simple user creation failed: {reason}");
                }

                await userManager.AddToRoleAsync(newUser, userRole);
                logger.LogInformation("Simple user account created for {Email}", email);
                return;
            }

            var currentRoles = await userManager.GetRolesAsync(existingUser);
            if (currentRoles.Count > 0)
            {
                await userManager.RemoveFromRolesAsync(existingUser, currentRoles);
            }

            await userManager.AddToRoleAsync(existingUser, userRole);
            logger.LogInformation("User {Email} set as {Role}", existingUser.Email, userRole);
        }

        private static async Task InitializeRoles(RoleManager<IdentityRole> roleManager)
        {
            var roleNames = Enum.GetNames(typeof(UserRoleEnum));

            foreach (var roleName in roleNames)
            {
                if (await roleManager.RoleExistsAsync(roleName))
                {
                    continue;
                }

                var newRole = new IdentityRole(roleName)
                {
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };

                await roleManager.CreateAsync(newRole);
            }
        }
    }
}
