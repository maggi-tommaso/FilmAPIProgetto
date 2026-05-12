using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class CinemaEndpoints
{
    public static IEndpointRouteBuilder MapCinemaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/cinemas").WithTags("Cinemas");

        group.MapGet("/", async (FilmDbContext db) =>
        {
            var items = await db.Cinemas
                .AsNoTracking()
                .Select(c => new CinemaDTO(c.Id, c.Nome, c.Indirizzo, c.Citta))
                .ToListAsync();

            return Results.Ok(items);
        });

        group.MapGet("/{id:int}", async (int id, FilmDbContext db) =>
        {
            var cinema = await db.Cinemas
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new CinemaDTO(c.Id, c.Nome, c.Indirizzo, c.Citta))
                .FirstOrDefaultAsync();

            return cinema is null ? Results.NotFound() : Results.Ok(cinema);
        });

        group.MapPost("/", async (CinemaDTO input, FilmDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(input.Nome) ||
                string.IsNullOrWhiteSpace(input.Indirizzo) ||
                string.IsNullOrWhiteSpace(input.Citta))
            {
                return Results.BadRequest("Nome, Indirizzo e Citta sono obbligatori.");
            }

            var entity = new Cinema
            {
                Nome = input.Nome.Trim(),
                Indirizzo = input.Indirizzo.Trim(),
                Citta = input.Citta.Trim()
            };

            db.Cinemas.Add(entity);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/cinemas/{entity.Id}",
                new CinemaDTO(entity.Id, entity.Nome, entity.Indirizzo, entity.Citta));
        });

        group.MapPut("/{id:int}", async (int id, CinemaDTO input, FilmDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(input.Nome) ||
                string.IsNullOrWhiteSpace(input.Indirizzo) ||
                string.IsNullOrWhiteSpace(input.Citta))
            {
                return Results.BadRequest("Nome, Indirizzo e Citta sono obbligatori.");
            }

            var entity = await db.Cinemas.FindAsync(id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            entity.Nome = input.Nome.Trim();
            entity.Indirizzo = input.Indirizzo.Trim();
            entity.Citta = input.Citta.Trim();

            await db.SaveChangesAsync();
            return Results.Ok(new CinemaDTO(entity.Id, entity.Nome, entity.Indirizzo, entity.Citta));
        });

        group.MapDelete("/{id:int}", async (int id, FilmDbContext db) =>
        {
            var entity = await db.Cinemas.FindAsync(id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            db.Cinemas.Remove(entity);

            try
            {
                await db.SaveChangesAsync();
                return Results.NoContent();
            }
            catch (DbUpdateException)
            {
                return Results.Conflict("Impossibile eliminare il cinema: esistono proiezioni collegate.");
            }
        });

        return app;
    }
}
