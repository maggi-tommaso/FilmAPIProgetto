using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FilmAPI.DTO;
using FilmAPI.Services;

namespace FilmAPI.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/register", async (RegisterRequestDTO dto, IAuthService service) =>
        {
            try
            {
                var result = await service.RegisterAsync(dto);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }
        }).AllowAnonymous();

        group.MapPost("/login", async (LoginRequestDTO dto, IAuthService service) =>
        {
            try
            {
                var result = await service.LoginAsync(dto);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        }).AllowAnonymous();

        group.MapPost("/refresh", async (RefreshTokenRequestDTO dto, IAuthService service) =>
        {
            try
            {
                var result = await service.RefreshAsync(dto.RefreshToken, dto.DeviceId);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        }).AllowAnonymous();

        group.MapPost("/logout", async (RefreshTokenRequestDTO dto, IAuthService service) =>
        {
            var result = await service.LogoutAsync(dto.RefreshToken, dto.DeviceId);
            return result ? Results.Ok() : Results.NotFound();
        }).RequireAuthorization("Authenticated");

        group.MapGet("/me", async (HttpContext context, IAuthService service) =>
        {
            var userIdClaim = context.User.FindFirst("sub")?.Value
                ?? context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var userInfo = await service.GetUserByIdAsync(userId);
            return userInfo is null ? Results.Unauthorized() : Results.Ok(userInfo);
        }).RequireAuthorization("Authenticated");

        group.MapPost("/change-password", async (ChangePasswordRequestDTO dto, HttpContext context, IAuthService service) =>
        {
            var userIdClaim = context.User.FindFirst("sub")?.Value
                ?? context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            try
            {
                await service.ChangePasswordAsync(userId, dto);
                return Results.Ok(new { message = "Password aggiornata con successo." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Unauthorized();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireAuthorization("Authenticated");

        group.MapPost("/forgot-password", async (ForgotPasswordRequestDTO dto, IAuthService service) =>
        {
            await service.RequestPasswordResetAsync(dto);
            return Results.Ok(new { message = "Se l'email e registrata, riceverai un link per reimpostare la password." });
        }).AllowAnonymous();

        group.MapPost("/reset-password", async (ResetPasswordRequestDTO dto, IAuthService service) =>
        {
            try
            {
                var result = await service.ResetPasswordAsync(dto, dto.DeviceId);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).AllowAnonymous();

        group.MapPost("/set-password", async (ResetPasswordRequestDTO dto, IAuthService service) =>
        {
            try
            {
                var result = await service.SetPasswordAsync(dto, dto.DeviceId);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).AllowAnonymous();

        group.MapPost("/verify-email", async (VerifyEmailRequestDTO dto, IAuthService service) =>
        {
            try
            {
                await service.VerifyEmailAsync(dto.Token);
                return Results.Ok(new { message = "Email verificata con successo." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).AllowAnonymous();

        group.MapGet("/security/me", async (HttpContext context, IAuthService service) =>
        {
            var userIdClaim = context.User.FindFirst("sub")?.Value
                ?? context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            var security = await service.GetAccountSecurityAsync(userId);
            return Results.Ok(security);
        }).RequireAuthorization("Authenticated");

        group.MapPost("/set-password/request", async (HttpContext context, IAuthService service) =>
        {
            var userIdClaim = context.User.FindFirst("sub")?.Value
                ?? context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Results.Unauthorized();

            try
            {
                await service.RequestSetPasswordAsync(userId);
                return Results.Ok(new { message = "Ti abbiamo inviato una email per impostare la password." });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireAuthorization("Authenticated");
    }
}
