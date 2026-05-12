using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace FilmAPI.Data;

public static class DbSeeder
{
    public static async Task SeedIfEmptyAsync(FilmDbContext db, CancellationToken ct = default)
    {
        var desiredRegisti = new List<Regista>
        {
            new() { Nome = "Federico", Cognome = "Fellini", Nazionalita = "Italiana" },
            new() { Nome = "Sergio", Cognome = "Leone", Nazionalita = "Italiana" },
            new() { Nome = "Paolo", Cognome = "Sorrentino", Nazionalita = "Italiana" },
            new() { Nome = "Roberto", Cognome = "Benigni", Nazionalita = "Italiana" },
            new() { Nome = "Akira", Cognome = "Kurosawa", Nazionalita = "Giapponese" },
            new() { Nome = "Hayao", Cognome = "Miyazaki", Nazionalita = "Giapponese" },
            new() { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "Britannica" },
            new() { Nome = "Steven", Cognome = "Spielberg", Nazionalita = "Statunitense" },
            new() { Nome = "Quentin", Cognome = "Tarantino", Nazionalita = "Statunitense" },
            new() { Nome = "Martin", Cognome = "Scorsese", Nazionalita = "Statunitense" },
        };

        var existingRegisti = await db.Registi
            .AsNoTracking()
            .Select(r => new { r.Id, r.Nome, r.Cognome })
            .ToListAsync(ct);

        var existingKeySet = existingRegisti
            .Select(r => $"{r.Nome}|{r.Cognome}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var registiToAdd = desiredRegisti
            .Where(r => !existingKeySet.Contains($"{r.Nome}|{r.Cognome}"))
            .ToList();

        if (registiToAdd.Count > 0)
        {
            await db.Registi.AddRangeAsync(registiToAdd, ct);
            await db.SaveChangesAsync(ct);
        }

        var registiByKey = await db.Registi
            .AsNoTracking()
            .Select(r => new { r.Id, r.Nome, r.Cognome })
            .ToDictionaryAsync(
                r => $"{r.Nome}|{r.Cognome}",
                r => r.Id,
                StringComparer.OrdinalIgnoreCase,
                ct);

        int RId(string nome, string cognome) => registiByKey[$"{nome}|{cognome}"];

        var desiredFilms = new List<Film>
        {
            new()
            {
                Titolo = "La dolce vita",
                DataProduzione = new DateOnly(1960, 2, 5),
                Durata = 174,
                RegistaId = RId("Federico", "Fellini"),
            },
            new()
            {
                Titolo = "8½",
                DataProduzione = new DateOnly(1963, 2, 14),
                Durata = 138,
                RegistaId = RId("Federico", "Fellini"),
            },
            new()
            {
                Titolo = "Il buono, il brutto, il cattivo",
                DataProduzione = new DateOnly(1966, 12, 23),
                Durata = 161,
                RegistaId = RId("Sergio", "Leone"),
            },
            new()
            {
                Titolo = "C'era una volta il West",
                DataProduzione = new DateOnly(1968, 12, 21),
                Durata = 165,
                RegistaId = RId("Sergio", "Leone"),
            },
            new()
            {
                Titolo = "La grande bellezza",
                DataProduzione = new DateOnly(2013, 5, 21),
                Durata = 142,
                RegistaId = RId("Paolo", "Sorrentino"),
            },
            new()
            {
                Titolo = "La vita è bella",
                DataProduzione = new DateOnly(1997, 12, 20),
                Durata = 116,
                RegistaId = RId("Roberto", "Benigni"),
            },
            new()
            {
                Titolo = "I sette samurai",
                DataProduzione = new DateOnly(1954, 4, 26),
                Durata = 207,
                RegistaId = RId("Akira", "Kurosawa"),
            },
            new()
            {
                Titolo = "La città incantata",
                DataProduzione = new DateOnly(2001, 7, 20),
                Durata = 125,
                RegistaId = RId("Hayao", "Miyazaki"),
            },
            new()
            {
                Titolo = "Inception",
                DataProduzione = new DateOnly(2010, 7, 16),
                Durata = 148,
                RegistaId = RId("Christopher", "Nolan"),
            },
            new()
            {
                Titolo = "Interstellar",
                DataProduzione = new DateOnly(2014, 11, 7),
                Durata = 169,
                RegistaId = RId("Christopher", "Nolan"),
            },
            new()
            {
                Titolo = "Schindler's List",
                DataProduzione = new DateOnly(1993, 12, 15),
                Durata = 195,
                RegistaId = RId("Steven", "Spielberg"),
            },
            new()
            {
                Titolo = "Jurassic Park",
                DataProduzione = new DateOnly(1993, 6, 11),
                Durata = 127,
                RegistaId = RId("Steven", "Spielberg"),
            },
            new()
            {
                Titolo = "Pulp Fiction",
                DataProduzione = new DateOnly(1994, 10, 14),
                Durata = 154,
                RegistaId = RId("Quentin", "Tarantino"),
            },
            new()
            {
                Titolo = "Django Unchained",
                DataProduzione = new DateOnly(2012, 12, 25),
                Durata = 165,
                RegistaId = RId("Quentin", "Tarantino"),
            },
            new()
            {
                Titolo = "Taxi Driver",
                DataProduzione = new DateOnly(1976, 2, 8),
                Durata = 114,
                RegistaId = RId("Martin", "Scorsese"),
            },
            new()
            {
                Titolo = "Goodfellas",
                DataProduzione = new DateOnly(1990, 9, 19),
                Durata = 146,
                RegistaId = RId("Martin", "Scorsese"),
            },
        };

        var existingFilms = await db.Films
            .AsNoTracking()
            .Select(f => new { f.Titolo, f.RegistaId })
            .ToListAsync(ct);

        var existingFilmKeySet = existingFilms
            .Select(f => $"{f.RegistaId}|{f.Titolo}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var filmsToAdd = desiredFilms
            .Where(f => !existingFilmKeySet.Contains($"{f.RegistaId}|{f.Titolo}"))
            .ToList();

        if (filmsToAdd.Count > 0)
        {
            await db.Films.AddRangeAsync(filmsToAdd, ct);
            await db.SaveChangesAsync(ct);
        }

        var adminEmail = "admin@salaluce.local";
        var hasAdmin = await db.Utenti.AnyAsync(u => u.Email == adminEmail, ct);
        if (!hasAdmin)
        {
            db.Utenti.Add(new Utente
            {
                Username = "admin",
                Nome = "Admin",
                Cognome = "SalaLuce",
                Email = adminEmail,
                EmailConfermata = true,
                Provider = "local",
                PasswordHash = HashPassword("123"),
                CreatoIlUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync(ct);
        }

        var admin = await db.Utenti.FirstOrDefaultAsync(u => u.Email == adminEmail, ct);
        if (admin is not null)
        {
            var seededTickets = await db.BigliettiUtente.AnyAsync(b => b.UtenteId == admin.Id, ct);
            if (!seededTickets)
            {
                var now = DateTime.UtcNow;
                var tickets = new[]
                {
                    new BigliettoUtente
                    {
                        UtenteId = admin.Id,
                        Codice = "ADM-VAL-001",
                        AcquistatoIlUtc = now.AddDays(-20),
                        ConvalidatoIlUtc = now.AddDays(-19),
                        IsConvalidato = true
                    },
                    new BigliettoUtente
                    {
                        UtenteId = admin.Id,
                        Codice = "ADM-VAL-002",
                        AcquistatoIlUtc = now.AddDays(-8),
                        ConvalidatoIlUtc = now.AddDays(-7),
                        IsConvalidato = true
                    },
                    new BigliettoUtente
                    {
                        UtenteId = admin.Id,
                        Codice = "ADM-PEN-001",
                        AcquistatoIlUtc = now.AddDays(-1),
                        ConvalidatoIlUtc = null,
                        IsConvalidato = false
                    }
                };

                await db.BigliettiUtente.AddRangeAsync(tickets, ct);

                await db.SaveChangesAsync(ct);
            }
        }
    }

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}

