using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class RegistaEndpoints
{
    public static IEndpointRouteBuilder MapRegistaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/registi").WithTags("Registi");

        group.MapGet("/", async (FilmDbContext db) =>
        {
            var items = await db.Registi
                .AsNoTracking()
                .Select(r => new RegistaDTO(r.Id, r.Nome, r.Cognome, r.Nazionalita))
                .ToListAsync();

            return Results.Ok(items);
        });

        group.MapGet("/{id:int}", async (int id, FilmDbContext db) =>
        {
            var regista = await db.Registi
                .AsNoTracking()
                .Where(r => r.Id == id)
                .Select(r => new RegistaDTO(r.Id, r.Nome, r.Cognome, r.Nazionalita))
                .FirstOrDefaultAsync();

            return regista is null ? Results.NotFound() : Results.Ok(regista);
        });

        group.MapPost("/", async (RegistaDTO input, FilmDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(input.Nome) ||
                string.IsNullOrWhiteSpace(input.Cognome) ||
                string.IsNullOrWhiteSpace(input.Nazionalita))
            {
                return Results.BadRequest("Nome, Cognome e Nazionalita sono obbligatori.");
            }

            var entity = new Regista
            {
                Nome = input.Nome.Trim(),
                Cognome = input.Cognome.Trim(),
                Nazionalita = input.Nazionalita.Trim()
            };

            db.Registi.Add(entity);
            await db.SaveChangesAsync();

            var dto = new RegistaDTO(entity.Id, entity.Nome, entity.Cognome, entity.Nazionalita);
            return Results.Created($"/registi/{entity.Id}", dto);
        });

        group.MapPut("/{id:int}", async (int id, RegistaDTO input, FilmDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(input.Nome) ||
                string.IsNullOrWhiteSpace(input.Cognome) ||
                string.IsNullOrWhiteSpace(input.Nazionalita))
            {
                return Results.BadRequest("Nome, Cognome e Nazionalita sono obbligatori.");
            }

            var entity = await db.Registi.FindAsync(id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            entity.Nome = input.Nome.Trim();
            entity.Cognome = input.Cognome.Trim();
            entity.Nazionalita = input.Nazionalita.Trim();

            await db.SaveChangesAsync();
            return Results.Ok(new RegistaDTO(entity.Id, entity.Nome, entity.Cognome, entity.Nazionalita));
        });

        group.MapDelete("/{id:int}", async (int id, FilmDbContext db) =>
        {
            var entity = await db.Registi.FindAsync(id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            db.Registi.Remove(entity);

            try
            {
                await db.SaveChangesAsync();
                return Results.NoContent();
            }
            catch (DbUpdateException)
            {
                return Results.Conflict("Impossibile eliminare il regista: esistono film collegati.");
            }
        });

        return app;
    }
}
