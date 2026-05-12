using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class FilmEndpoints
{
    public static IEndpointRouteBuilder MapFilmEndpoints(this IEndpointRouteBuilder app, string defaultCoverImagePath)
    {
        var group = app.MapGroup("/films").WithTags("Films");

        group.MapGet("/", async (FilmDbContext db) =>
        {
            var items = await db.Films
                .AsNoTracking()
                .Select(f => new FilmDTO(
                    f.Id,
                    f.Titolo,
                    f.DataProduzione,
                    f.RegistaId,
                    f.Durata,
                    f.CopertinaPath,
                    f.FilmatoPath))
                .ToListAsync();

            return Results.Ok(items);
        });

        group.MapGet("/{id:int}", async (int id, FilmDbContext db) =>
        {
            var film = await db.Films
                .AsNoTracking()
                .Where(f => f.Id == id)
                .Select(f => new FilmDTO(
                    f.Id,
                    f.Titolo,
                    f.DataProduzione,
                    f.RegistaId,
                    f.Durata,
                    f.CopertinaPath,
                    f.FilmatoPath))
                .FirstOrDefaultAsync();

            return film is null ? Results.NotFound() : Results.Ok(film);
        });

        group.MapPost("/", async (FilmDTO input, FilmDbContext db) =>
        {
            var validation = ValidateInput(input);
            if (validation is not null)
            {
                return Results.BadRequest(validation);
            }

            var registaExists = await db.Registi.AnyAsync(r => r.Id == input.RegistaId);
            if (!registaExists)
            {
                return Results.BadRequest($"RegistaId {input.RegistaId} non valido.");
            }

            var copertinaPath = string.IsNullOrWhiteSpace(input.CopertinaPath)
                ? defaultCoverImagePath
                : input.CopertinaPath.Trim();

            var filmatoPath = string.IsNullOrWhiteSpace(input.FilmatoPath)
                ? null
                : input.FilmatoPath.Trim();

            var entity = new Film
            {
                Titolo = input.Titolo.Trim(),
                DataProduzione = input.DataProduzione,
                RegistaId = input.RegistaId,
                Durata = input.Durata,
                CopertinaPath = copertinaPath,
                FilmatoPath = filmatoPath
            };

            db.Films.Add(entity);
            await db.SaveChangesAsync();

            var dto = new FilmDTO(
                entity.Id,
                entity.Titolo,
                entity.DataProduzione,
                entity.RegistaId,
                entity.Durata,
                entity.CopertinaPath,
                entity.FilmatoPath);

            return Results.Created($"/films/{entity.Id}", dto);
        });

        group.MapPut("/{id:int}", async (int id, FilmDTO input, FilmDbContext db) =>
        {
            var validation = ValidateInput(input);
            if (validation is not null)
            {
                return Results.BadRequest(validation);
            }

            var entity = await db.Films.FindAsync(id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            var registaExists = await db.Registi.AnyAsync(r => r.Id == input.RegistaId);
            if (!registaExists)
            {
                return Results.BadRequest($"RegistaId {input.RegistaId} non valido.");
            }

            entity.Titolo = input.Titolo.Trim();
            entity.DataProduzione = input.DataProduzione;
            entity.RegistaId = input.RegistaId;
            entity.Durata = input.Durata;
            entity.CopertinaPath = string.IsNullOrWhiteSpace(input.CopertinaPath)
                ? defaultCoverImagePath
                : input.CopertinaPath.Trim();
            entity.FilmatoPath = string.IsNullOrWhiteSpace(input.FilmatoPath)
                ? null
                : input.FilmatoPath.Trim();

            await db.SaveChangesAsync();

            return Results.Ok(new FilmDTO(
                entity.Id,
                entity.Titolo,
                entity.DataProduzione,
                entity.RegistaId,
                entity.Durata,
                entity.CopertinaPath,
                entity.FilmatoPath));
        });

        group.MapDelete("/{id:int}", async (int id, FilmDbContext db) =>
        {
            var entity = await db.Films.FindAsync(id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            db.Films.Remove(entity);

            try
            {
                await db.SaveChangesAsync();
                return Results.NoContent();
            }
            catch (DbUpdateException)
            {
                return Results.Conflict("Impossibile eliminare il film: esistono proiezioni collegate.");
            }
        });

        return app;
    }

    private static string? ValidateInput(FilmDTO input)
    {
        if (string.IsNullOrWhiteSpace(input.Titolo))
        {
            return "Titolo obbligatorio.";
        }

        if (input.RegistaId <= 0)
        {
            return "RegistaId deve essere maggiore di zero.";
        }

        if (input.Durata <= 0)
        {
            return "Durata deve essere maggiore di zero.";
        }

        return null;
    }
}
