using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.RateLimiting;
using FilmAPI.Data;
using FilmAPI.Endpoints;
using FilmAPI.Services;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var envCandidates = new[]
{
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".env")),
    Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "backend", ".env")),
    Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".env"))
};

var backendEnvPath = envCandidates.FirstOrDefault(File.Exists);
if (!string.IsNullOrWhiteSpace(backendEnvPath))
{
    Env.Load(backendEnvPath);
}
else
{
    Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(new FrontendRuntimeConfig(
    Environment.GetEnvironmentVariable("STRIPE_PUBLISHABLE_API_KEY")
    ?? Environment.GetEnvironmentVariable("STRIPE_PUBLISHABLE_KEY")
    ?? string.Empty));

var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "film-api-db";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "root";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "root";
var dbUseAutoDetect = (Environment.GetEnvironmentVariable("DB_USE_AUTODETECT") ?? "true")
    .Equals("true", StringComparison.OrdinalIgnoreCase);
var dbServerVersion = Environment.GetEnvironmentVariable("DB_SERVER_VERSION") ?? "10.11.0-mariadb";

var connectionString = $"Server={dbHost};Port={dbPort};Database={dbName};User Id={dbUser};Password={dbPassword};";
var serverVersion = dbUseAutoDetect
    ? ServerVersion.AutoDetect(connectionString)
    : ServerVersion.Parse(dbServerVersion);

builder.Services.AddDbContext<FilmDbContext>(
    dbContextOptions =>
    {
        dbContextOptions
            .UseMySql(connectionString, serverVersion)
            .LogTo(Console.WriteLine, LogLevel.Information);

        if (builder.Environment.IsDevelopment())
        {
            dbContextOptions.EnableSensitiveDataLogging().EnableDetailedErrors();
        }
    }
);

builder.Services.AddScoped<IRegistaService, RegistaService>();
builder.Services.AddScoped<IFilmService, FilmService>();
builder.Services.AddScoped<ICinemaService, CinemaService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfiloService, ProfiloService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddScoped<IProgrammazioneService, ProgrammazioneService>();
builder.Services.AddScoped<ISalaService, SalaService>();
builder.Services.AddScoped<IShowService, ShowService>();
builder.Services.AddScoped<ISeatHoldService, SeatHoldService>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.AddScoped<ICreditoService, CreditoService>();
builder.Services.AddScoped<IBigliettoService, BigliettoService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IValidazioneBigliettoService, ValidazioneBigliettoService>();
builder.Services.AddScoped<IStripePaymentGateway, StripePaymentGateway>();
builder.Services.AddScoped<IPagamentoService, PagamentoService>();
builder.Services.AddHostedService<RefreshTokenCleanupService>();
builder.Services.AddHostedService<ExpiredHoldCleanupService>();
builder.Services.AddScoped<IAccountTokenService, AccountTokenService>();
builder.Services.AddScoped<IAccountEmailService, AccountEmailService>();
builder.Services.AddScoped<IUserSecurityAuditService, UserSecurityAuditService>();
builder.Services.AddHttpClient<GoogleExternalAuthProvider>();
builder.Services.AddHttpClient<MicrosoftExternalAuthProvider>();
builder.Services.AddScoped<IExternalAuthService, ExternalAuthService>();
builder.Services.AddScoped<INotificheService, NotificheService>();

var tmdbBearerToken = Environment.GetEnvironmentVariable("TMDB_BEARER_TOKEN")
    ?? builder.Configuration["TMDB:BearerToken"];
if (!string.IsNullOrWhiteSpace(tmdbBearerToken))
{
    builder.Services.AddHttpClient<ITmdbService, TmdbService>(client =>
    {
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tmdbBearerToken);
        client.Timeout = TimeSpan.FromSeconds(15);
    });

    builder.Services.AddHttpClient("TmdbImport", client =>
    {
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tmdbBearerToken);
        client.Timeout = TimeSpan.FromSeconds(30);
    });
}

builder.Services.AddScoped<ITmdbImportService, TmdbImportService>();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowRedCurtainFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5001", "http://127.0.0.1:5001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("Authorization");
    });
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => (c.Type == "role" || c.Type == System.Security.Claims.ClaimTypes.Role) && c.Value == "Admin")));
    options.AddPolicy("PowerUserOrAdmin", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => (c.Type == "role" || c.Type == System.Security.Claims.ClaimTypes.Role) && (c.Value == "PowerUser" || c.Value == "Admin"))));
    options.AddPolicy("Authenticated", policy =>
        policy.RequireAuthenticatedUser());
});

builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "FilmAPI";
    config.Title = "FilmAPI v1";
    config.Version = "v1";
});

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException("JWT_SECRET environment variable is required but not set.");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "RedCurtainAPI";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "RedCurtainWeb";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        RoleClaimType = "role"
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
            if (identity != null)
            {
                var roleClaim = identity.FindFirst("role");
                if (roleClaim != null)
                {
                    identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, roleClaim.Value));
                }
            }
            return System.Threading.Tasks.Task.CompletedTask;
        }
    };
});

var rateLimitingDisabled = (Environment.GetEnvironmentVariable("DISABLE_RATE_LIMITING") ?? "false")
    .Equals("true", StringComparison.OrdinalIgnoreCase);

if (!rateLimitingDisabled)
{
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy("AuthRateLimit", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));
    });
}

var app = builder.Build();

app.UseCors("AllowRedCurtainFrontend");
app.UseAuthentication();
app.UseAuthorization();
if (!rateLimitingDisabled)
{
    app.UseRateLimiter();
}
app.UseStaticFiles();

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;

    headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline' https://cdn.tailwindcss.com https://cdnjs.cloudflare.com https://kit.fontawesome.com; style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://cdn.tailwindcss.com; img-src 'self' data: https:; font-src 'self' https://cdnjs.cloudflare.com https://kit.fontawesome.com; connect-src 'self' http://localhost:* https:; frame-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'";
    headers["X-Frame-Options"] = "DENY";
    headers["X-Content-Type-Options"] = "nosniff";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), interest-cohort=()";

    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "FilmAPI v1";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
        config.DocExpansion = "list";
    });
}

app.MapRegistiEndpoints();
app.MapFilmsEndpoints();
app.MapCinemasEndpoints();
app.MapMediaEndpoints();
app.MapCategorieEndpoints();
app.MapAuthEndpoints();
app.MapExternalAuthEndpoints();
app.MapProfiloEndpoints();
app.MapAdminUtentiEndpoints();
app.MapProgrammazioneEndpoints();
app.MapSaleEndpoints();
app.MapShowsEndpoints();
app.MapCheckoutEndpoints();
app.MapCreditoEndpoints();
app.MapPagamentoEndpoints();
app.MapValidazioneBigliettiEndpoints();
app.MapTmdbEndpoints();
app.MapTmdbImportEndpoints();
app.MapNotificheEndpoints();

app.MapGet("/config/frontend", (FrontendRuntimeConfig config) => Results.Ok(new
{
    stripePublishableKey = config.StripePublishableKey
})).AllowAnonymous();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
    var seeder = new DataSeeder(db);
    await seeder.SeedAsync();

    var logger = scope.ServiceProvider.GetRequiredService<ILogger<WeeklyShowPlanner>>();
    var planner = new WeeklyShowPlanner(db, logger);
    await planner.PlanCurrentWeekAsync();

    await DataSeeder.UpdateSalaImmaginiAsync(db);
}

app.Run();

public partial class Program;

public sealed record FrontendRuntimeConfig(string StripePublishableKey);
