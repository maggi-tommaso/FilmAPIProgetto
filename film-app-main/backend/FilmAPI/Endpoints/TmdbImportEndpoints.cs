using FilmAPI.Services;

namespace FilmAPI.Endpoints;

public static class TmdbImportEndpoints
{
    public static void MapTmdbImportEndpoints(this WebApplication app)
    {
        app.MapPost("/admin/import-tmdb", async (
            int? movieCount,
            int? showDays,
            ITmdbImportService service) =>
        {
            var result = await service.ImportNowPlayingAsync(
                movieCount ?? 20,
                showDays ?? 15);

            return Results.Ok(result);
        }).RequireAuthorization("AdminOnly");

        app.MapPost("/admin/tmdb-enrich-posters", async (ITmdbImportService service) =>
        {
            var result = await service.EnrichPostersAsync();
            return Results.Ok(result);
        }).RequireAuthorization("AdminOnly");
    }
}
