using AutoMapper;
using BackPredictFinance.API.Data;
using BackPredictFinance.API.Middleware;
using BackPredictFinance.Common;
using BackPredictFinance.Common.Email;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.Jwt;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services;
using BackPredictFinance.Services.AuthServices;
using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.Services.PythonServices;
using BackPredictFinance.Services.TwelveDataServices;
using BackPredictFinance.Services.UserServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Host.UseSerilog((_, loggerConfiguration) =>
{
    loggerConfiguration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("Logs/finance_log_.txt", rollingInterval: RollingInterval.Day)
        .WriteTo.File("Logs/finance_error_.txt", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Error);
});

var connectionString = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");
}

builder.Services.AddDbContext<FinanceDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

builder.Services
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

builder.Services.Configure<DataProtectionTokenProviderOptions>(opts =>
    opts.TokenLifespan = TimeSpan.FromHours(10));

builder.Services.Configure<JWTToken>(configuration.GetRequiredSection(nameof(JWTToken)));
var jwtOptions = configuration.GetRequiredSection(nameof(JWTToken)).Get<JWTToken>()
    ?? throw new InvalidOperationException("JWTToken configuration is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew = TimeSpan.FromSeconds(20),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Bearer", policy =>
    {
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole(UserRoleEnum.Admin.ToString(), UserRoleEnum.SuperAdmin.ToString()));

    options.AddPolicy("RequireSuperAdminRole", policy =>
        policy.RequireRole(UserRoleEnum.SuperAdmin.ToString()));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontPolicy", cors =>
    {
        cors.WithOrigins("http://localhost:4200", "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddLocalization();

var mapperExpression = new MapperConfigurationExpression();
var mapperConfiguration = new MapperConfiguration(mapperExpression, NullLoggerFactory.Instance);
builder.Services.AddSingleton<IMapper>(new Mapper(mapperConfiguration));

builder.Services.Configure<EmailServiceConfiguration>(configuration.GetSection("EmailService"));
builder.Services.Configure<PythonCliOptions>(configuration.GetSection("PythonCli"));
builder.Services.Configure<TwelveDataOptions>(configuration.GetSection("TwelveData"));

builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IPathService, PathService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IJwtGeneratorService, JwtGeneratorService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IUserAssetService, UserAssetService>();
builder.Services.AddScoped<IUserRoleDataService, UserRoleDataService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IPythonApiService, PythonApiService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<AnalyticService>();
builder.Services.AddScoped<ITickerService, TickerService>();
builder.Services.AddScoped<IClientFinanceService, ClientFinanceService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName);
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("FrontPolicy");
app.UseGlobalExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await DatabaseUpdater.RunDatabaseUpdate(app);

app.Run();

