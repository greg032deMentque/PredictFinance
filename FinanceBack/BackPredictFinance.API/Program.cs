using AutoMapper;
using BackPredictFinance.API.Data;
using BackPredictFinance.API.HealthChecks;
using BackPredictFinance.API.Middleware;
using BackPredictFinance.API.ProgramSubFiles;
using BackPredictFinance.Common;
using BackPredictFinance.Common.Auth;
using BackPredictFinance.Common.Email;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.Jwt;
using BackPredictFinance.Common.Security;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;
using System.Net;
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

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

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
    .AddErrorDescriber<LocalizedIdentityErrorDescriber>()
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
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?.GetUserId();
                var tokenStamp = context.Principal?.FindFirst(JwtClaimTypes.SecurityStamp)?.Value;
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(tokenStamp))
                {
                    context.Fail("Invalid token claims.");
                    return;
                }

                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<User>>();
                var user = await userManager.FindByIdAsync(userId);
                if (user is null || !user.IsActive)
                {
                    context.Fail("User is not available.");
                    return;
                }

                var currentStamp = await userManager.GetSecurityStampAsync(user);
                if (!string.Equals(tokenStamp, currentStamp, StringComparison.Ordinal))
                {
                    context.Fail("Token security stamp mismatch.");
                }
            }
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
         .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
         .WithHeaders("Authorization", "Content-Type", "X-XSRF-TOKEN", "X-Requested-With")
         .AllowCredentials());
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddLocalization();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    var knownProxies = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
    foreach (var knownProxy in knownProxies)
    {
        if (IPAddress.TryParse(knownProxy.Trim(), out var proxyAddress))
        {
            options.KnownProxies.Add(proxyAddress);
        }
    }

    var knownNetworks = configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];
    foreach (var knownNetwork in knownNetworks)
    {
        if (System.Net.IPNetwork.TryParse(knownNetwork.Trim(), out var parsedNetwork))
        {
            options.KnownIPNetworks.Add(parsedNetwork);
        }
    }
});

ProgramServiceDeclarator.ServicesDeclarator(builder.Services, builder.Configuration);
ProgramServiceDeclarator.ConfigureOptions(builder.Services, builder.Configuration);

var mapperConfiguration = new MapperConfiguration(cfg =>
{
    cfg.AddMaps(typeof(BackPredictFinance.ViewModels.UserViewModels.UserViewModelProfile).Assembly);
}, NullLoggerFactory.Instance);

// Fait echouer le demarrage si l'un des 21 profils AutoMapper est mal configure (champ non mappe,
// mapping ambigu...) plutot que de laisser une valeur null silencieuse apparaitre en production.
mapperConfiguration.AssertConfigurationIsValid();

builder.Services.AddSingleton<IMapper>(mapperConfiguration.CreateMapper());

builder.Services.Configure<EmailServiceConfiguration>(configuration.GetSection("EmailService"));

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<BackPredictFinance.API.SeedData.ConceptsSeedService>();
    builder.Services.AddHostedService<BackPredictFinance.API.SeedData.GlossaryTermsSeedService>();
    builder.Services.AddHostedService<BackPredictFinance.API.SeedData.EducationArticlesSeedService>();
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Aligne la forme des erreurs 400 de validation de modèle sur celle produite par ExceptionMiddleware
// ({ message, errors: string[] }, voir Middleware/ExceptionMiddleware.cs) : sans cette configuration,
// le ValidationProblemDetails natif (dictionnaire { "Champ": ["message"] }) n'est pas reconnu par
// l'intercepteur d'erreurs du front, qui attend un tableau de strings.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
        var errors = context.ModelState.Values
            .SelectMany(x => x.Errors)
            .Select(x => x.ErrorMessage)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        var problem = new
        {
            type = "about:blank",
            title = "Requête invalide",
            status = StatusCodes.Status400BadRequest,
            message = "Requête invalide.",
            errors,
            detail = (string?)null,
            traceId,
            instance = context.HttpContext.Request.Path.Value,
            method = context.HttpContext.Request.Method,
        };

        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" }
        };
    };
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

var configuredKnownProxies = app.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
var configuredKnownNetworks = app.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];
if (configuredKnownProxies.Length == 0 && configuredKnownNetworks.Length == 0)
{
    throw new InvalidOperationException(
        "ForwardedHeaders:KnownProxies (or ForwardedHeaders:KnownNetworks) must be configured when X-Forwarded-For handling is enabled.");
}

app.UseForwardedHeaders();

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
app.UseSecurityHeaders();
app.UseCors("default");
app.UseGlobalExceptionHandler();
app.UseMiddleware<RateLimitingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RequestHandlerMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

if (!app.Environment.IsEnvironment("Testing"))
{
    await DatabaseUpdater.RunDatabaseUpdate(app);
}

app.Run();
