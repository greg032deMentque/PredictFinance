using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Models;
using BackPredictFinance.Services;
using BackPredictFinance.Services.UserServices;
using BackPredictFinance.ViewModels;
using BackPredictFinance.ViewModels.UserViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace BackPredictFinance.API.Data
{
    public static class DatabaseUpdater
    {
        /// <summary>
        /// Initializes the database with the roles and the admin account.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static async Task RunDatabaseUpdate(WebApplication app)
        {
            // create the database from the migrations
            using (var scope = app.Services.CreateScope())
            {
                var serviceProvider = scope.ServiceProvider;
                var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var logger = serviceProvider.GetRequiredService<ILogger<BaseService>>();
                var userService = serviceProvider.GetRequiredService<UserService>();
                var config = app.Services.GetRequiredService<IConfiguration>();
                var financeDbContext = serviceProvider.GetRequiredService<FinanceDbContext>();

                logger.LogInformation("Initializing Wagram data start...");

                logger.LogInformation("Database migrations (if needed) start....");
                await RunMigrationsAsync(financeDbContext, logger);
                logger.LogInformation("Database migrations done");

                await InitializeRoles(serviceProvider);

                logger.LogInformation("Ensure admin account is up to date start...");
                await UpdateAdminAccountAsync(config["adminEmail"], config["adminPwd"], userManager, logger, userService);
                logger.LogInformation("Ensure admin account is up to date done");

                logger.LogInformation("Initializing Wagram data done");
            }
        }

        private static async Task RunMigrationsAsync(FinanceDbContext context, ILogger<BaseService> logger)
        {
            var pendingMigrations = context.Database.GetPendingMigrations().Count();
            if (pendingMigrations > 0)
            {
                logger.LogInformation($"Trying adding {pendingMigrations} new migrations start...");
                await context.Database.MigrateAsync();
                logger.LogInformation($"Trying adding {pendingMigrations} new migrations done");
            }
            else
            {
                logger.LogInformation($"No migrations needed");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <param name="logService"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private static async Task UpdateAdminAccountAsync(string? email, string? password, UserManager<User> usermanager, ILogger<BaseService> logService, UserService userService)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentNullException("email or password", "Missing one of the arguments");
            }

            var user = await usermanager.FindByEmailAsync(email);

            if (user == null)
            {
                logService.LogInformation($"No admin account \"{email}\" found in database! I will create it");

                var userVm = new UserViewModel();
                userVm.Email = email;
                userVm.FirstName = "admin";
                userVm.LastName = "admin";
                userVm.Password = password;
                userVm.IsActive = true;

                await userService.CreateUser(userVm);

                logService.LogInformation($"Admin account has been created");
            }
        }

        private static async Task InitializeRoles(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

            // Liste des rôles définis dans l'enum
            var enumRoleNames = Enum.GetNames(typeof(UserRoleEnum)).ToHashSet();

            // Liste des rôles existants en base
            var existingRoles = roleManager.Roles.ToList();

            // 1. Créer les rôles manquants
            foreach (var roleName in enumRoleNames)
            {
                if (!existingRoles.Any(r => r.Name == roleName))
                {
                    var newRole = new IdentityRole(roleName)
                    {
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    };
                    await roleManager.CreateAsync(newRole);
                }
            }
        }
    }
}