using AutoMapper;
using BackPredictFinance.API.Data;
using BackPredictFinance.API.Middleware;
using BackPredictFinance.API.ProgramSubFiles;
using BackPredictFinance.Common;
using BackPredictFinance.Common.Email;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.Jwt;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

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
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
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
        policy.RequireRole(UserRoleEnum.Admin.ToString()));
});

var corsOrigins = configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim().TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToList()
    ?? [];

var frontendBaseUrl = configuration["Frontend:BaseUrl"];
if (!string.IsNullOrWhiteSpace(frontendBaseUrl))
{
    corsOrigins.Add(frontendBaseUrl.Trim().TrimEnd('/'));
}

corsOrigins = [.. corsOrigins.Distinct(StringComparer.OrdinalIgnoreCase)];
if (corsOrigins.Count == 0)
{
    throw new InvalidOperationException("At least one allowed CORS origin must be configured.");
}

builder.Services.AddCors(o =>
{
    o.AddPolicy("default", p =>
        p.WithOrigins([.. corsOrigins])
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials());
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddLocalization();

ProgramServiceDeclarator.ServicesDeclarator(builder.Services);

var mapperConfiguration = new MapperConfiguration(cfg =>
{
    cfg.AddMaps(typeof(BackPredictFinance.ViewModels.UserViewModels.UserViewModelProfile).Assembly);
}, NullLoggerFactory.Instance);

builder.Services.AddSingleton<IMapper>(mapperConfiguration.CreateMapper());

builder.Services.Configure<EmailServiceConfiguration>(configuration.GetSection("EmailService"));
builder.Services.Configure<MarketDataOptions>(configuration.GetSection("MarketData"));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
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

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", null, null),
            []
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
app.UseCors("default");
app.UseGlobalExceptionHandler();
app.UseMiddleware<RateLimitingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RequestHandlerMiddleware>();

app.MapControllers();

if (!app.Environment.IsEnvironment("Testing"))
{
    await DatabaseUpdater.RunDatabaseUpdate(app);
}

app.Run();
