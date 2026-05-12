using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class ProiezioneEndpoints
{
    public static IEndpointRouteBuilder MapProiezioneEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/proiezioni").WithTags("Proiezioni");

        group.MapGet("/", async (FilmDbContext db) =>
        {
            var items = await db.Proiezioni
                .AsNoTracking()
                .OrderBy(p => p.Data)
                .ThenBy(p => p.Ora)
                .Select(p => new ProiezioneDTO(p.Id, p.CinemaId, p.FilmId, p.Data, p.Ora))
                .ToListAsync();

            return Results.Ok(items);
        });

        group.MapGet("/{id:int}", async (int id, FilmDbContext db) =>
        {
            var item = await db.Proiezioni
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new DatiProiezioneDTO(
                    p.Id,
                    p.Cinema!.Nome,
                    p.Film!.Titolo,
                    p.Data,
                    p.Ora,
                    p.Film.Durata))
                .FirstOrDefaultAsync();

            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapPost("/", async (ProiezioneDTO input, FilmDbContext db) =>
        {
            if (input.CinemaId <= 0 || input.FilmId <= 0)
            {
                return Results.BadRequest("CinemaId e FilmId devono essere maggiori di zero.");
            }

            var cinemaExists = await db.Cinemas.AnyAsync(c => c.Id == input.CinemaId);
            if (!cinemaExists)
            {
                return Results.BadRequest($"CinemaId {input.CinemaId} non valido.");
            }

            var filmExists = await db.Films.AnyAsync(f => f.Id == input.FilmId);
            if (!filmExists)
            {
                return Results.BadRequest($"FilmId {input.FilmId} non valido.");
            }

            var duplicateExists = await db.Proiezioni.AnyAsync(p =>
                p.CinemaId == input.CinemaId &&
                p.FilmId == input.FilmId &&
                p.Data == input.Data &&
                p.Ora == input.Ora);

            if (duplicateExists)
            {
                return Results.Conflict("Proiezione duplicata per stesso cinema, film, data e ora.");
            }

            var entity = new Proiezione
            {
                CinemaId = input.CinemaId,
                FilmId = input.FilmId,
                Data = input.Data,
                Ora = input.Ora
            };

            db.Proiezioni.Add(entity);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Results.Conflict("Proiezione duplicata per stesso cinema, film, data e ora.");
            }

            return Results.Created(
                $"/proiezioni/{entity.Id}",
                new ProiezioneDTO(entity.Id, entity.CinemaId, entity.FilmId, entity.Data, entity.Ora));
        });

        group.MapPut("/{id:int}", async (int id, ProiezioneDTO input, FilmDbContext db) =>
        {
            if (input.CinemaId <= 0 || input.FilmId <= 0)
            {
                return Results.BadRequest("CinemaId e FilmId devono essere maggiori di zero.");
            }

            var entity = await db.Proiezioni.FindAsync(id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            var cinemaExists = await db.Cinemas.AnyAsync(c => c.Id == input.CinemaId);
            if (!cinemaExists)
            {
                return Results.BadRequest($"CinemaId {input.CinemaId} non valido.");
            }

            var filmExists = await db.Films.AnyAsync(f => f.Id == input.FilmId);
            if (!filmExists)
            {
                return Results.BadRequest($"FilmId {input.FilmId} non valido.");
            }

            var duplicateExists = await db.Proiezioni.AnyAsync(p =>
                p.Id != id &&
                p.CinemaId == input.CinemaId &&
                p.FilmId == input.FilmId &&
                p.Data == input.Data &&
                p.Ora == input.Ora);

            if (duplicateExists)
            {
                return Results.Conflict("Proiezione duplicata per stesso cinema, film, data e ora.");
            }

            entity.CinemaId = input.CinemaId;
            entity.FilmId = input.FilmId;
            entity.Data = input.Data;
            entity.Ora = input.Ora;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Results.Conflict("Proiezione duplicata per stesso cinema, film, data e ora.");
            }

            return Results.Ok(new ProiezioneDTO(entity.Id, entity.CinemaId, entity.FilmId, entity.Data, entity.Ora));
        });

        group.MapDelete("/{id:int}", async (int id, FilmDbContext db) =>
        {
            var entity = await db.Proiezioni.FindAsync(id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            db.Proiezioni.Remove(entity);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }
}
