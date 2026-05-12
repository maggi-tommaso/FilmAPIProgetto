using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services;

namespace FilmAPI.Endpoints;

public static class ExternalAuthEndpoints
{
    public static void MapExternalAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth/external");

        group.MapGet("/providers", (IExternalAuthService service) =>
        {
            return Results.Ok(service.GetProviders());
        }).AllowAnonymous();

        group.MapGet("/google/start", async (string? redirect, IExternalAuthService service) =>
        {
            var url = await service.StartAsync(ExternalLoginProvider.Google, redirect ?? "/index.html");
            return Results.Redirect(url);
        }).AllowAnonymous();

        group.MapGet("/google/callback", async (string state, string code, IExternalAuthService service) =>
        {
            try
            {
                var (_, redirectPath) = await service.CallbackAsync(ExternalLoginProvider.Google, state, code);
                var frontendUrl = (Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "http://localhost:5001") + redirectPath;
                return Results.Redirect(frontendUrl);
            }
            catch (Exception ex)
            {
                var errMsg = Uri.EscapeDataString(ex.Message);
                return Results.Redirect($"{Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "http://localhost:5001"}/login.html?error={errMsg}");
            }
        }).AllowAnonymous();

        group.MapGet("/microsoft/start", async (string? redirect, IExternalAuthService service) =>
        {
            var url = await service.StartAsync(ExternalLoginProvider.Microsoft, redirect ?? "/index.html");
            return Results.Redirect(url);
        }).AllowAnonymous();

        group.MapGet("/microsoft/callback", async (string state, string code, IExternalAuthService service) =>
        {
            try
            {
                var (_, redirectPath) = await service.CallbackAsync(ExternalLoginProvider.Microsoft, state, code);
                var frontendUrl = (Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "http://localhost:5001") + redirectPath;
                return Results.Redirect(frontendUrl);
            }
            catch (Exception ex)
            {
                var errMsg = Uri.EscapeDataString(ex.Message);
                return Results.Redirect($"{Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "http://localhost:5001"}/login.html?error={errMsg}");
            }
        }).AllowAnonymous();

        group.MapPost("/exchange", async (ExternalExchangeRequestDTO dto, IExternalAuthService service) =>
        {
            try
            {
                var result = await service.ExchangeAsync(dto.Code, dto.DeviceId);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Unauthorized();
            }
        }).AllowAnonymous();
    }
}
