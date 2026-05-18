using FilmAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FilmAPI.Endpoints;

public static class TmdbEndpoints
{
    public static void MapTmdbEndpoints(this WebApplication app)
    {
        app.MapGet("/tmdb/search", async (string? title, [FromServices] ITmdbService service) =>
        {
            if (string.IsNullOrWhiteSpace(title))
                return Results.BadRequest(new { message = "Parametro 'title' obbligatorio." });

            var result = await service.SearchMovieAsync(title.Trim());
            if (result is null)
                return Results.NotFound(new { message = "Nessun film trovato su TMDB per il titolo specificato." });

            return Results.Ok(result);
        }).RequireAuthorization("PowerUserOrAdmin");
    }
}
