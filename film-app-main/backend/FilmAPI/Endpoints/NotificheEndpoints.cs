using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FilmAPI.Services;

namespace FilmAPI.Endpoints;

public static class NotificheEndpoints
{
    public static void MapNotificheEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/profilo/notifiche");

        group.MapGet("", async (HttpContext context, INotificheService service) =>
        {
            var userId = GetUserIdFromContext(context);
            if (userId == null) return Results.Unauthorized();

            var notifiche = await service.GetNotificheAsync(userId.Value);
            return Results.Ok(notifiche);
        }).RequireAuthorization("Authenticated");

        group.MapPost("/valuta", async (HttpContext context, ValutazioneCreateDTO dto, INotificheService service) =>
        {
            var userId = GetUserIdFromContext(context);
            if (userId == null) return Results.Unauthorized();

            try
            {
                var result = await service.ValutaFilmAsync(userId.Value, dto.FilmId, dto.Rating);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).RequireAuthorization("Authenticated");
    }

    private static int? GetUserIdFromContext(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;

        return userId;
    }
}
