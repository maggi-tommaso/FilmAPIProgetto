using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class ProfiloService : IProfiloService
{
    private readonly FilmDbContext _context;

    public ProfiloService(FilmDbContext context)
    {
        _context = context;
    }

    public async Task<UserInfoDTO?> GetProfiloAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null) return null;

        return MapToUserInfoDTO(user);
    }

    public async Task<UserInfoDTO?> UpdateProfiloAsync(int userId, ProfiloUpdateDTO dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null) return null;

        user.Nome = dto.Nome;
        user.Cognome = dto.Cognome;
        user.Telefono = dto.Telefono;

        await _context.SaveChangesAsync();

        return MapToUserInfoDTO(user);
    }

    public async Task<CinemaPreferitoDTO?> GetCinemaPreferitoAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.CinemaPreferito)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return null;

        if (user.CinemaPreferito is null)
        {
            return new CinemaPreferitoDTO { CinemaId = null, Cinema = null };
        }

        return new CinemaPreferitoDTO
        {
            CinemaId = user.CinemaPreferitoId,
            Cinema = new CinemaSintesiDTO
            {
                Id = user.CinemaPreferito.Id,
                Nome = user.CinemaPreferito.Nome,
                Citta = user.CinemaPreferito.Citta,
                Indirizzo = user.CinemaPreferito.Indirizzo,
                Telefono = user.CinemaPreferito.Telefono,
                CodiceLocale = user.CinemaPreferito.CodiceLocale
            }
        };
    }

    public async Task<CinemaPreferitoDTO> SetCinemaPreferitoAsync(int userId, int? cinemaId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null) throw new InvalidOperationException("Utente non trovato");

        if (cinemaId.HasValue)
        {
            var cinemaExists = await _context.Cinemas.AnyAsync(c => c.Id == cinemaId.Value);
            if (!cinemaExists) throw new ArgumentException("Cinema non trovato");
        }

        user.CinemaPreferitoId = cinemaId;
        await _context.SaveChangesAsync();

        return await GetCinemaPreferitoAsync(userId) ?? new CinemaPreferitoDTO { CinemaId = null, Cinema = null };
    }

    public async Task<FilmPreferitoDTO?> GetFilmPreferitoAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.FilmPreferito)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return null;

        if (user.FilmPreferito is null)
        {
            return new FilmPreferitoDTO { FilmId = null, Film = null };
        }

        return new FilmPreferitoDTO
        {
            FilmId = user.FilmPreferitoId,
            Film = new FilmSintesiDTO
            {
                Id = user.FilmPreferito.Id,
                Titolo = user.FilmPreferito.Titolo,
                CopertinaPath = user.FilmPreferito.CopertinaPath
            }
        };
    }

    public async Task<FilmPreferitoDTO> SetFilmPreferitoAsync(int userId, int? filmId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null) throw new InvalidOperationException("Utente non trovato");

        if (filmId.HasValue)
        {
            var filmExists = await _context.Films.AnyAsync(f => f.Id == filmId.Value);
            if (!filmExists) throw new ArgumentException("Film non trovato");
        }

        user.FilmPreferitoId = filmId;
        await _context.SaveChangesAsync();

        return await GetFilmPreferitoAsync(userId) ?? new FilmPreferitoDTO { FilmId = null, Film = null };
    }

    public async Task<bool> DeleteAccountAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .Include(u => u.ActionTokens)
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return false;

        _context.RefreshTokens.RemoveRange(user.RefreshTokens);
        _context.AccountActionTokens.RemoveRange(user.ActionTokens);
        _context.UserExternalLogins.RemoveRange(user.ExternalLogins);
        _context.Users.Remove(user);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<AccountExportDTO?> ExportAccountDataAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.CinemaPreferito)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return null;

        var ordini = await _context.Ordini
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => new OrdineExportDTO
            {
                Id = o.Id,
                CodiceOrdine = o.CodiceOrdine,
                CreatedAtUtc = o.CreatedAtUtc,
                TotaleLordo = o.TotaleLordo,
                Stato = o.Stato.ToString()
            })
            .ToListAsync();

        var biglietti = await _context.Biglietti
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.Ordine!.CreatedAtUtc)
            .Select(b => new BigliettoExportDTO
            {
                Id = b.Id,
                CodiceBiglietto = b.CodiceBiglietto,
                Stato = b.Stato.ToString(),
                FilmTitolo = b.Show != null && b.Show.Film != null ? b.Show.Film.Titolo : "N/D",
                ShowStartAtUtc = b.Show != null ? b.Show.StartAtUtc : default
            })
            .ToListAsync();

        return new AccountExportDTO
        {
            User = MapToUserInfoDTO(user),
            Ordini = ordini,
            Biglietti = biglietti,
            ExportTimestampUtc = DateTime.UtcNow
        };
    }

    private static UserInfoDTO MapToUserInfoDTO(User user)
    {
        return new UserInfoDTO
        {
            Id = user.Id,
            Email = user.Email,
            Nome = user.Nome,
            Cognome = user.Cognome,
            Telefono = user.Telefono,
            Ruolo = user.Ruolo.ToString(),
            DataRegistrazione = user.DataRegistrazione,
            EmailVerified = user.EmailVerifiedAtUtc != null,
            PrivacyConsentAtUtc = user.PrivacyConsentAtUtc,
            TermsAcceptedAtUtc = user.TermsAcceptedAtUtc
        };
    }
}
