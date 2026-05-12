using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Data;

public class FilmDbContext : DbContext
{
    public FilmDbContext(DbContextOptions<FilmDbContext> options) : base(options)
    {
    }

    public DbSet<Regista> Registi => Set<Regista>();
    public DbSet<Film> Films => Set<Film>();
    public DbSet<Cinema> Cinemas => Set<Cinema>();
    public DbSet<Proiezione> Proiezioni => Set<Proiezione>();
    public DbSet<Utente> Utenti => Set<Utente>();
    public DbSet<SessioneAccesso> SessioniAccesso => Set<SessioneAccesso>();
    public DbSet<BigliettoUtente> BigliettiUtente => Set<BigliettoUtente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Regista>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).ValueGeneratedOnAdd();

            entity.Property(r => r.Nome)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(r => r.Cognome)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(r => r.Nazionalita)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasMany(r => r.Films)
                .WithOne(f => f.Regista)
                .HasForeignKey(f => f.RegistaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Film>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Id).ValueGeneratedOnAdd();

            entity.Property(f => f.Titolo)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(f => f.Durata)
                .IsRequired();

            entity.Property(f => f.CopertinaPath)
                .HasMaxLength(500);

            entity.Property(f => f.FilmatoPath)
                .HasMaxLength(500);

            entity.HasMany(f => f.Proiezioni)
                .WithOne(p => p.Film)
                .HasForeignKey(p => p.FilmId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Cinema>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).ValueGeneratedOnAdd();

            entity.Property(c => c.Nome)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(c => c.Indirizzo)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(c => c.Citta)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasMany(c => c.Proiezioni)
                .WithOne(p => p.Cinema)
                .HasForeignKey(p => p.CinemaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Proiezione>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).ValueGeneratedOnAdd();

            entity.Property(p => p.Data)
                .IsRequired();

            entity.Property(p => p.Ora)
                .IsRequired();

            entity.HasIndex(p => new { p.CinemaId, p.FilmId, p.Data, p.Ora })
                .IsUnique();
        });

        modelBuilder.Entity<Utente>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).ValueGeneratedOnAdd();

            entity.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(u => u.Nome)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.Cognome)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(320);

            entity.Property(u => u.GoogleSubject)
                .HasMaxLength(255);

            entity.Property(u => u.PasswordHash)
                .HasMaxLength(200);

            entity.Property(u => u.Provider)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("local");

            entity.Property(u => u.EmailConfermata)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(u => u.EmailTokenConferma);

            entity.Property(u => u.EmailTokenScadeIlUtc);

            entity.Property(u => u.CreatoIlUtc)
                .IsRequired();

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.HasIndex(u => u.Username)
                .IsUnique();

            entity.HasIndex(u => u.GoogleSubject)
                .IsUnique();
        });

        modelBuilder.Entity<SessioneAccesso>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.CreatoIlUtc)
                .IsRequired();

            entity.Property(s => s.ScadeIlUtc)
                .IsRequired();

            entity.HasOne(s => s.Utente)
                .WithMany(u => u.Sessioni)
                .HasForeignKey(s => s.UtenteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => s.UtenteId);
            entity.HasIndex(s => s.ScadeIlUtc);
        });

        modelBuilder.Entity<BigliettoUtente>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Id).ValueGeneratedOnAdd();

            entity.Property(b => b.Codice)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(b => b.AcquistatoIlUtc)
                .IsRequired();

            entity.HasOne(b => b.Utente)
                .WithMany(u => u.Biglietti)
                .HasForeignKey(b => b.UtenteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(b => b.Codice)
                .IsUnique();

            entity.HasIndex(b => b.UtenteId);
            entity.HasIndex(b => b.IsConvalidato);
        });
    }
}
