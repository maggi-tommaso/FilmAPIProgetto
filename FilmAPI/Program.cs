using DotNetEnv;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Endpoints;
using Microsoft.EntityFrameworkCore;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

var connectionString = BuildConnectionString(builder.Configuration);
var defaultCoverPath = BuildDefaultCoverPath(builder.Configuration);
var googleClientId = BuildGoogleClientId(builder.Configuration);
var smtpConfig = BuildSmtpConfig(builder.Configuration);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDbContext<FilmDbContext>(options =>
    options.UseMySql(connectionString, new MariaDbServerVersion(new Version(11, 4, 0))));

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(options =>
{
    options.Title = "FilmAPI";
    options.Version = "v1";
    options.Description = "REST API per gestione registi, film, cinema e proiezioni.";
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("SalaLuceWeb", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    return false;
                }

                return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
            })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();

    // MariaDB può non essere immediatamente pronta (soprattutto con Docker).
    var attempts = 0;
    while (true)
    {
        try
        {
            await db.Database.MigrateAsync();
            await DbSeeder.SeedIfEmptyAsync(db);
            break;
        }
        catch (Exception) when (attempts++ < 15)
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Min(10, 1 + attempts)));
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi(settings =>
    {
        settings.Path = "/swagger/v1/swagger.json";
    });

    app.UseSwaggerUi(settings =>
    {
        settings.Path = "/swagger";
        settings.DocumentPath = "/swagger/v1/swagger.json";
    });
}

app.UseCors("SalaLuceWeb");

app.MapRegistaEndpoints();
app.MapFilmEndpoints(defaultCoverPath);
app.MapCinemaEndpoints();
app.MapProiezioneEndpoints();
app.MapAuthEndpoints(googleClientId, smtpConfig, "http://localhost:5072");

app.Run();

static string BuildConnectionString(IConfiguration configuration)
{
    var host = Environment.GetEnvironmentVariable("DB_HOST")
        ?? configuration["Database:Host"]
        ?? "localhost";

    var port = Environment.GetEnvironmentVariable("DB_PORT")
        ?? configuration["Database:Port"]
        ?? "3306";

    var name = Environment.GetEnvironmentVariable("DB_NAME")
        ?? configuration["Database:Name"]
        ?? "filmapi_db";

    var user = Environment.GetEnvironmentVariable("DB_USER")
        ?? configuration["Database:User"]
        ?? "filmapi_user";

    var password = Environment.GetEnvironmentVariable("DB_PASSWORD")
        ?? configuration["Database:Password"]
        ?? "password123";

    if (string.IsNullOrWhiteSpace(host) ||
        string.IsNullOrWhiteSpace(port) ||
        string.IsNullOrWhiteSpace(name) ||
        string.IsNullOrWhiteSpace(user) ||
        string.IsNullOrWhiteSpace(password))
    {
        var fallbackConnection = configuration.GetConnectionString("MariaDb");
        if (!string.IsNullOrWhiteSpace(fallbackConnection))
        {
            return fallbackConnection;
        }
    }

    return $"Server={host};Port={port};Database={name};User={user};Password={password};";
}

static string BuildDefaultCoverPath(IConfiguration configuration)
{
    var fromEnv = Environment.GetEnvironmentVariable("DEFAULT_COVER_IMAGE_PATH");
    if (!string.IsNullOrWhiteSpace(fromEnv))
    {
        return fromEnv;
    }

    var fromConfig = configuration["Defaults:CoverImagePath"];
    if (!string.IsNullOrWhiteSpace(fromConfig))
    {
        return fromConfig;
    }

    return "/media/defaults/cover-default.jpg";
}

static string? BuildGoogleClientId(IConfiguration configuration)
{
    var fromEnv = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
    if (!string.IsNullOrWhiteSpace(fromEnv))
    {
        return fromEnv;
    }

    var fromConfig = configuration["GoogleAuth:ClientId"];
    if (!string.IsNullOrWhiteSpace(fromConfig))
    {
        return fromConfig;
    }

    return null;
}

static SmtpConfig BuildSmtpConfig(IConfiguration configuration)
{
    var host = Environment.GetEnvironmentVariable("SMTP_HOST")
        ?? configuration["Smtp:Host"] ?? string.Empty;
    var portStr = Environment.GetEnvironmentVariable("SMTP_PORT")
        ?? configuration["Smtp:Port"] ?? "587";
    var user = Environment.GetEnvironmentVariable("SMTP_USER")
        ?? configuration["Smtp:User"] ?? string.Empty;
    var password = Environment.GetEnvironmentVariable("SMTP_PASSWORD")
        ?? configuration["Smtp:Password"] ?? string.Empty;
    var from = Environment.GetEnvironmentVariable("SMTP_FROM")
        ?? configuration["Smtp:From"] ?? string.Empty;
    var useSslStr = Environment.GetEnvironmentVariable("SMTP_USE_SSL")
        ?? configuration["Smtp:UseSsl"] ?? "false";

    int.TryParse(portStr, out var port);
    bool.TryParse(useSslStr, out var useSsl);

    return new SmtpConfig(host, port, user, password, from, useSsl);
}
