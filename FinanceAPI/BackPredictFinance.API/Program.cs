using BackPredictFinance.Common;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.Jwt;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Models;
using BackPredictFinance.Services.PythonServices;
using BackPredictFinance.Services.TwelveDataServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using SixLabors.ImageSharp;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// ---------- Logging Configuration ----------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ---------- Database & Identity ----------
// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<FinanceDbContext>(options =>
options.UseSqlServer(connectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)), ServiceLifetime.Transient);

// Configure Identity with Roles and Token Providers
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<FinanceDbContext>()
.AddDefaultTokenProviders();

// ---------- Authentication (JWT) ----------
builder.Services.Configure<DataProtectionTokenProviderOptions>(opts => opts.TokenLifespan = TimeSpan.FromHours(10));

var jWTToken = configuration.GetRequiredSection(nameof(JWTToken));
builder.Services.Configure<JWTToken>(jWTToken);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Bearer", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c =>
                c.Type == ClaimTypes.Role &&
                (c.Value == UserRoleEnum.User.ToString() ||
                 c.Value == UserRoleEnum.Admin.ToString())
            )
        )
        .AddAuthenticationSchemes(nameof(JWTToken))
    );
});
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(nameof(JWTToken), x =>
{
    x.RequireHttpsMetadata = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateAudience = true,
        ValidateIssuer = true,
        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
        RequireExpirationTime = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jWTToken.Get<JWTToken>().Issuer,
        ValidAudience = jWTToken.Get<JWTToken>().Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jWTToken.Get<JWTToken>().Secret)),
        ClockSkew = TimeSpan.FromSeconds(20),
        NameClaimType = JwtRegisteredClaimNames.Sub,
        RoleClaimType = "role" // ou ClaimTypes.Role
    };
});



// ---------- Authorization Policies ----------
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("admin"));
    options.AddPolicy("RequireSuperAdminRole", policy => policy.RequireRole("superadmin"));
});

// ---------- Log ----------


var logTemplate = "[{Level:u4}] [{UserName}] [{Timestamp:HH:mm:ss}] {Message:lj}{NewLine}{Exception}";
builder.Host.UseSerilog((ctx, lc) => lc
        .WriteTo.Logger(lc => lc
        .WriteTo.Console(outputTemplate: logTemplate))

        .WriteTo.Logger(lc => lc
        .MinimumLevel.Is(LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                    .Filter.ByIncludingOnly(logEvent => logEvent.Level == LogEventLevel.Error)
                    .WriteTo.File("Logs/fianance_error_.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: logTemplate)).Enrich.FromLogContext()

        .WriteTo.Logger(lc => lc
        .MinimumLevel.Is(LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                    .WriteTo.File("Logs/finance_log_.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: logTemplate)).Enrich.FromLogContext()
    );

// ---------- Controllers & Swagger ----------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// customize the schema ID generation so that each type gets a unique identifier
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName);
});

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 1safsfsdfdfd\"",
    });
    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement {
                {
                    new OpenApiSecurityScheme {
                        Reference = new OpenApiReference {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] {}
                }
            });
});

// ---------- PythonApi ----------

builder.Services
   .AddHttpClient<IPythonApiService, PythonApiService>(client =>
   {
       client.BaseAddress = new Uri(builder.Configuration["PythonApi:BaseUrl"]!);
       client.Timeout = TimeSpan.FromSeconds(10);
   });



var app = builder.Build();


// ------------ TwelData ---------- 
builder.Services.Configure<TwelveDataOptions>(
    builder.Configuration.GetSection("TwelveData"));

builder.Services
    .AddHttpClient<ITickerService, TickerService>()  // HttpClient injecté
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));   // rotation de handler. evite SocketException


// ---------- Middleware Pipeline ----------
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
