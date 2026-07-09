using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PrestexaAPI.Data;
using PrestexaAPI.Services;
using PrestexaAPI.Services.Mismo;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ITrustedDeviceService, TrustedDeviceService>();

builder.Services.AddScoped<IAuthTokenService, AuthTokenService>();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 15728640;
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 15728640;
});

if (builder.Environment.IsProduction())
{
    var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
    var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
    var clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");

    if (string.IsNullOrWhiteSpace(tenantId) ||
        string.IsNullOrWhiteSpace(clientId) ||
        string.IsNullOrWhiteSpace(clientSecret))
    {
        throw new Exception("Azure Key Vault credentials are missing.");
    }

    builder.Configuration.AddAzureKeyVault(
        new Uri("https://prestexa-kv.vault.azure.net/"),
        new ClientSecretCredential(
            tenantId,
            clientId,
            clientSecret,
            new ClientSecretCredentialOptions
            {
                AdditionallyAllowedTenants = { "*" }
            }
        )
    );
}

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IEmailSender, SendGridEmailSender>();
builder.Services.AddScoped<IMismoParserService, MismoParserService>();
builder.Services.AddScoped<IMismoImportService, MismoImportService>();
builder.Services.AddScoped<ManualMortgageApplicationService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new Exception("Database connection string missing.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
);

var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new Exception("JWT Key missing.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        RoleClaimType = ClaimTypes.Role,
        ClockSkew = TimeSpan.Zero,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        )
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("PrestexaFrontend", policy =>
    {
        policy
            .WithOrigins(
                "https://admin.prestexa.com",
                "https://auth.prestexa.com",
                "https://los.prestexa.com",
                "https://*.prestexa.com",
                "http://localhost:3000",
                "http://127.0.0.1:3000"
            )
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.OrderActionsBy(apiDesc =>
    {
        var controller = apiDesc.ActionDescriptor.RouteValues["controller"] ?? "";
        var path = apiDesc.RelativePath ?? "";
        var method = apiDesc.HttpMethod?.ToUpperInvariant() ?? "";

        var isCollectionRoute = !path.Contains("{");
        var isSingleRoute = path.Contains("{");

        var order = method switch
        {
            "POST" => 1,
            "GET" when isCollectionRoute => 2,
            "GET" when isSingleRoute => 3,
            "PUT" => 4,
            "PATCH" => 5,
            "DELETE" => 6,
            _ => 99
        };

        return $"{controller}_{order:D2}_{path}";
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database migration failed: {ex.Message}");
        throw;
    }
}

app.UseSwagger();
app.UseSwaggerUI();

// app.UseHttpsRedirection();

app.UseCors("PrestexaFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();