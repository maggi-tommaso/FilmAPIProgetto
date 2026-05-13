using Microsoft.EntityFrameworkCore;
using FilmAPI.Data;
using FilmAPI.Model;

using System.Globalization;

namespace FilmAPI.Data;

public class DataSeeder
{
    private readonly FilmDbContext _context;

    public DataSeeder(FilmDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        await SeedAdminAsync();
        await SeedCategorieAsync();
        await SeedDevDataAsync();
    }

    private async Task SeedAdminAsync()
    {
        if (_context.Users.Any())
            return;

        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_SEED_EMAIL") ?? "admin@cinebase.it";
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_SEED_PASSWORD") ?? "Admin123!";

        var admin = new User
        {
            Email = adminEmail,
            NormalizedEmail = adminEmail.ToUpperInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            LocalCredentialsEnabled = true,
            Nome = "Admin",
            Cognome = "CineBase",
            Ruolo = UserRole.Admin,
            DataRegistrazione = DateTime.UtcNow,
            CreditoResiduo = 0,
            AuthVersion = 0
        };

        _context.Users.Add(admin);
        await _context.SaveChangesAsync();
    }

    private async Task SeedCategorieAsync()
    {
        if (_context.Categorie.Any())
            return;

        var categorie = new[]
        {
            "Drammatico", "Commedia", "Avventura", "Fantasy", "Horror", "Azione",
            "Fantascienza", "Thriller", "Animazione", "Documentario", "Romantico", "Storico"
        };

        foreach (var nome in categorie)
        {
            _context.Categorie.Add(new Categoria { Nome = nome });
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedDevDataAsync()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        if (!env.Equals("Development", StringComparison.OrdinalIgnoreCase))
            return;

        if (_context.Cinemas.Any())
            return;

        await SeedDevCinemasAsync();
        await SeedDevRegistiAsync();
        await SeedDevFilmsAsync();
        await SeedDevSaleAsync();
        await SeedDevPostiAsync();
        await SeedDevShowsAsync();
    }

    private async Task SeedDevCinemasAsync()
    {
        var cinemas = new[]
        {
            new Cinema { Nome = "CineBase Roma Centro", Citta = "Roma", Indirizzo = "Via del Corso 123", Latitudine = 41.9028, Longitudine = 12.4964, Telefono = "06 1234567", CodiceLocale = "CBR001" },
            new Cinema { Nome = "CineBase Milano Duomo", Citta = "Milano", Indirizzo = "Corso Buenos Aires 45", Latitudine = 45.4642, Longitudine = 9.1900, Telefono = "02 1234567", CodiceLocale = "CBM001" },
            new Cinema { Nome = "CineBase Napoli Centro", Citta = "Napoli", Indirizzo = "Via Toledo 78", Latitudine = 40.8518, Longitudine = 14.2681, Telefono = "081 1234567", CodiceLocale = "CBN001" },
            new Cinema { Nome = "CineBase Bologna", Citta = "Bologna", Indirizzo = "Via Indipendenza 22", Latitudine = 44.4949, Longitudine = 11.3426, Telefono = "051 1234567", CodiceLocale = "CBB001" },
            new Cinema { Nome = "CineBase Firenze", Citta = "Firenze", Indirizzo = "Via de' Calzaiuoli 15", Latitudine = 43.7696, Longitudine = 11.2558, Telefono = "055 1234567", CodiceLocale = "CBF001" },
            new Cinema { Nome = "CineBase Torino", Citta = "Torino", Indirizzo = "Via Garibaldi 30", Latitudine = 45.0703, Longitudine = 7.6869, Telefono = "011 1234567", CodiceLocale = "CBT001" },
            new Cinema { Nome = "CineBase Palermo", Citta = "Palermo", Indirizzo = "Via della Libertà 56", Latitudine = 38.1157, Longitudine = 13.3615, Telefono = "091 1234567", CodiceLocale = "CBP001" },
            new Cinema { Nome = "CineBase Bari", Citta = "Bari", Indirizzo = "Corso Cavour 89", Latitudine = 41.1171, Longitudine = 16.8719, Telefono = "080 1234567", CodiceLocale = "CBB002" },
            new Cinema { Nome = "CineBase Verona", Citta = "Verona", Indirizzo = "Via Mazzini 12", Latitudine = 45.4384, Longitudine = 10.9916, Telefono = "045 1234567", CodiceLocale = "CBV001" },
            new Cinema { Nome = "CineBase Genova", Citta = "Genova", Indirizzo = "Via XX Settembre 34", Latitudine = 44.4056, Longitudine = 8.9463, Telefono = "010 1234567", CodiceLocale = "CBG001" }
        };

        _context.Cinemas.AddRange(cinemas);
        await _context.SaveChangesAsync();
    }

    private async Task SeedDevRegistiAsync()
    {
        var registi = new[]
        {
            new Regista { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "Regno Unito" },
            new Regista { Nome = "Quentin", Cognome = "Tarantino", Nazionalita = "USA" },
            new Regista { Nome = "Greta", Cognome = "Gerwig", Nazionalita = "USA" },
            new Regista { Nome = "Martin", Cognome = "Scorsese", Nazionalita = "USA" },
            new Regista { Nome = "Steven", Cognome = "Spielberg", Nazionalita = "USA" },
            new Regista { Nome = "Denis", Cognome = "Villeneuve", Nazionalita = "Canada" },
            new Regista { Nome = "David", Cognome = "Fincher", Nazionalita = "USA" },
            new Regista { Nome = "Ridley", Cognome = "Scott", Nazionalita = "Regno Unito" },
            new Regista { Nome = "Paolo", Cognome = "Sorrentino", Nazionalita = "Italia" },
            new Regista { Nome = "Federico", Cognome = "Fellini", Nazionalita = "Italia" },
            new Regista { Nome = "Hayao", Cognome = "Miyazaki", Nazionalita = "Giappone" },
            new Regista { Nome = "Bong", Cognome = "Joon-ho", Nazionalita = "Corea del Sud" },
            new Regista { Nome = "Wes", Cognome = "Anderson", Nazionalita = "USA" },
            new Regista { Nome = "Guillermo", Cognome = "del Toro", Nazionalita = "Messico" },
            new Regista { Nome = "Alfonso", Cognome = "Cuaron", Nazionalita = "Messico" },
            new Regista { Nome = "James", Cognome = "Cameron", Nazionalita = "Canada" },
            new Regista { Nome = "Peter", Cognome = "Jackson", Nazionalita = "Nuova Zelanda" },
            new Regista { Nome = "Sergio", Cognome = "Leone", Nazionalita = "Italia" },
            new Regista { Nome = "Stanley", Cognome = "Kubrick", Nazionalita = "USA" },
            new Regista { Nome = "Francis Ford", Cognome = "Coppola", Nazionalita = "USA" },
            new Regista { Nome = "Tim", Cognome = "Burton", Nazionalita = "USA" },
            new Regista { Nome = "Robert", Cognome = "Zemeckis", Nazionalita = "USA" },
            new Regista { Nome = "Clint", Cognome = "Eastwood", Nazionalita = "USA" },
            new Regista { Nome = "Luca", Cognome = "Guadagnino", Nazionalita = "Italia" },
            new Regista { Nome = "Damien", Cognome = "Chazelle", Nazionalita = "USA" },
            new Regista { Nome = "Taika", Cognome = "Waititi", Nazionalita = "Nuova Zelanda" },
            new Regista { Nome = "Jordan", Cognome = "Peele", Nazionalita = "USA" },
            new Regista { Nome = "Alejandro", Cognome = "Inarritu", Nazionalita = "Messico" },
            new Regista { Nome = "Danny", Cognome = "Boyle", Nazionalita = "Regno Unito" },
            new Regista { Nome = "Paul Thomas", Cognome = "Anderson", Nazionalita = "USA" },
            new Regista { Nome = "Roman", Cognome = "Polanski", Nazionalita = "Polonia" },
            new Regista { Nome = "Darren", Cognome = "Aronofsky", Nazionalita = "USA" }
        };

        _context.Registi.AddRange(registi);
        await _context.SaveChangesAsync();
    }

    private async Task SeedDevFilmsAsync()
    {
        var registiList = await _context.Registi.ToListAsync();
        var registi = new Dictionary<string, Regista>();
        foreach (var r in registiList)
        {
            registi.TryAdd(r.Cognome, r);
            registi[$"{r.Nome} {r.Cognome}"] = r;
        }

        var filmsData = new (string Titolo, string RegistaCognome, int Anno, int Durata, string Descrizione, string Cast, DateOnly Rilascio, string[] Categorie)[]
        {
            ("Oppenheimer", "Nolan", 2023, 180, "La storia del fisico J. Robert Oppenheimer e il suo ruolo nello sviluppo della bomba atomica.", "Cillian Murphy, Emily Blunt, Matt Damon, Robert Downey Jr.", new DateOnly(2023, 7, 19), new[] { "Drammatico", "Storico" }),
            ("Inception", "Nolan", 2010, 148, "Un ladro specializzato nell'estrarre segreti dal subconscio durante il sonno riceve l'incarico di impiantare un'idea nella mente di un CEO.", "Leonardo DiCaprio, Joseph Gordon-Levitt, Ellen Page, Tom Hardy", new DateOnly(2010, 7, 16), new[] { "Azione", "Fantascienza", "Thriller" }),
            ("Interstellar", "Nolan", 2014, 169, "Un gruppo di esploratori viaggia attraverso un wormhole nello spazio per garantire la sopravvivenza dell'umanità.", "Matthew McConaughey, Anne Hathaway, Jessica Chastain", new DateOnly(2014, 11, 7), new[] { "Fantascienza", "Avventura", "Drammatico" }),
            ("Dunkirk", "Nolan", 2017, 106, "Durante la Seconda Guerra Mondiale, soldati alleati rimangono intrappolati sulla spiaggia di Dunkirk.", "Fionn Whitehead, Tom Hardy, Kenneth Branagh, Cillian Murphy", new DateOnly(2017, 7, 21), new[] { "Azione", "Drammatico", "Storico" }),
            ("Pulp Fiction", "Tarantino", 1994, 154, "Le storie di due gangster, un pugile e una coppia di rapinatori si intrecciano in questo capolavoro del cinema.", "John Travolta, Uma Thurman, Samuel L. Jackson, Bruce Willis", new DateOnly(1994, 10, 14), new[] { "Drammatico", "Thriller" }),
            ("Kill Bill: Volume 1", "Tarantino", 2003, 111, "Una spietata assassina si risveglia dal coma e parte per una missione di vendetta contro i suoi ex complici.", "Uma Thurman, Lucy Liu, Vivica A. Fox, Daryl Hannah", new DateOnly(2003, 10, 10), new[] { "Azione", "Thriller" }),
            ("Django Unchained", "Tarantino", 2012, 165, "Uno schiavo liberato e un cacciatore di taglie tedesco attraversano l'America per salvare la moglie del primo da un crudele proprietario terriero.", "Jamie Foxx, Christoph Waltz, Leonardo DiCaprio", new DateOnly(2012, 12, 25), new[] { "Azione", "Drammatico" }),
            ("Barbie", "Gerwig", 2023, 114, "Barbie e Ken frequentano un mondo colorato e ottimistico fino a quando non vengono cacciati dal loro paradiso e partono per il mondo reale.", "Margot Robbie, Ryan Gosling, America Ferrera, Will Ferrell", new DateOnly(2023, 7, 19), new[] { "Commedia", "Fantasy", "Avventura" }),
            ("Piccole Donne", "Gerwig", 2019, 135, "Quattro sorelle adolescenti crescono nell'America del dopo Guerra Civile, ognuna determinata a vivere la vita alle proprie condizioni.", "Saoirse Ronan, Emma Watson, Florence Pugh, Timothee Chalamet", new DateOnly(2019, 12, 25), new[] { "Drammatico", "Romantico" }),
            ("Taxi Driver", "Scorsese", 1976, 114, "Un veterano del Vietnam mentalmente instabile lavora come tassista notturno a New York, dove la violenza lo spinge verso un'esplosione di follia.", "Robert De Niro, Jodie Foster, Cybill Shepherd, Harvey Keitel", new DateOnly(1976, 2, 8), new[] { "Drammatico", "Thriller" }),
            ("The Wolf of Wall Street", "Scorsese", 2013, 180, "La vera storia di Jordan Belfort, un broker di New York che si arricchì in modo fraudolento negli anni '90.", "Leonardo DiCaprio, Jonah Hill, Margot Robbie, Matthew McConaughey", new DateOnly(2013, 12, 25), new[] { "Commedia", "Drammatico" }),
            ("Schindler's List", "Spielberg", 1993, 195, "La vera storia di Oskar Schindler, un industriale tedesco che salvò più di mille ebrei durante l'Olocausto.", "Liam Neeson, Ben Kingsley, Ralph Fiennes, Caroline Goodall", new DateOnly(1993, 12, 15), new[] { "Drammatico", "Storico" }),
            ("Jurassic Park", "Spielberg", 1993, 127, "Un parco a tema con dinosauri clonati diventa un incubo quando le creature preistoriche si liberano.", "Sam Neill, Laura Dern, Jeff Goldblum, Richard Attenborough", new DateOnly(1993, 6, 11), new[] { "Avventura", "Fantascienza", "Azione" }),
            ("Saving Private Ryan", "Spielberg", 1998, 169, "Un gruppo di soldati americani va in missione durante lo sbarco in Normandia per salvare un paracadutista i cui fratelli sono morti in guerra.", "Tom Hanks, Matt Damon, Tom Sizemore, Edward Burns", new DateOnly(1998, 7, 24), new[] { "Azione", "Drammatico", "Storico" }),
            ("Dune - Parte Due", "Villeneuve", 2024, 166, "Paul Atreides si unisce ai Fremen mentre cerca vendetta contro coloro che hanno distrutto la sua famiglia.", "Timothee Chalamet, Zendaya, Rebecca Ferguson, Josh Brolin", new DateOnly(2024, 3, 1), new[] { "Fantascienza", "Avventura", "Azione" }),
            ("Blade Runner 2049", "Villeneuve", 2017, 164, "Un nuovo blade runner scopre un segreto sepolto da tempo che potrebbe gettare nel caos ciò che resta della società.", "Ryan Gosling, Harrison Ford, Ana de Armas, Jared Leto", new DateOnly(2017, 10, 6), new[] { "Fantascienza", "Thriller" }),
            ("Fight Club", "Fincher", 1999, 139, "Un uomo insonne e un venditore di sapone carismatico formano un club di combattimento clandestino che si evolve in qualcosa di molto più grande.", "Brad Pitt, Edward Norton, Helena Bonham Carter, Jared Leto", new DateOnly(1999, 10, 15), new[] { "Drammatico", "Thriller" }),
            ("Seven", "Fincher", 1995, 127, "Due detective danno la caccia a un serial killer che commette omicidi ispirati ai sette peccati capitali.", "Brad Pitt, Morgan Freeman, Gwyneth Paltrow, Kevin Spacey", new DateOnly(1995, 9, 22), new[] { "Thriller", "Horror" }),
            ("Il Gladiatore", "Scott", 2000, 155, "Un generale romano tradito diventa gladiatore per cercare vendetta contro l'imperatore corrotto che ha ucciso la sua famiglia.", "Russell Crowe, Joaquin Phoenix, Connie Nielsen, Oliver Reed", new DateOnly(2000, 5, 5), new[] { "Azione", "Drammatico", "Storico" }),
            ("Alien", "Scott", 1979, 117, "L'equipaggio di un'astronave commerciale risponde a un segnale di soccorso proveniente da un pianeta desolato, scoprendo una forma di vita letale.", "Sigourney Weaver, Tom Skerritt, John Hurt, Ian Holm", new DateOnly(1979, 6, 22), new[] { "Horror", "Fantascienza" }),
            ("La Grande Bellezza", "Sorrentino", 2013, 142, "Uno scrittore e giornalista sessantacinquenne riflette sulla sua vita tra le feste e le terrazze di Roma.", "Toni Servillo, Carlo Verdone, Sabrina Ferilli, Carlo Buccirosso", new DateOnly(2013, 5, 21), new[] { "Drammatico", "Commedia" }),
            ("La Dolce Vita", "Fellini", 1960, 174, "Un giornalista scandalistico vaga per Roma in cerca di scoop e piaceri effimeri in sette giorni e sette notti.", "Marcello Mastroianni, Anita Ekberg, Anouk Aimee, Yvonne Furneaux", new DateOnly(1960, 2, 5), new[] { "Drammatico", "Commedia" }),
            ("La Città Incantata", "Miyazaki", 2001, 125, "Una bambina di dieci anni si ritrova in un mondo magico abitato da spiriti e creature fantastiche.", "Rumi Hiiragi, Miyu Irino, Mari Natsuki, Takashi Naito", new DateOnly(2001, 7, 20), new[] { "Animazione", "Fantasy", "Avventura" }),
            ("Il Mio Vicino Totoro", "Miyazaki", 1988, 86, "Due bambine si trasferiscono in campagna con il padre e scoprono creature magiche nella foresta vicina.", "Noriko Hidaka, Chika Sakamoto, Hitoshi Takagi", new DateOnly(1988, 4, 16), new[] { "Animazione", "Fantasy" }),
            ("Parasite", "Joon-ho", 2019, 132, "Una famiglia povera e disoccupata si insinua nella vita di una ricca famiglia borghese, con conseguenze inaspettate.", "Song Kang-ho, Lee Sun-kyun, Cho Yeo-jeong, Choi Woo-shik", new DateOnly(2019, 5, 30), new[] { "Drammatico", "Thriller", "Commedia" }),
            ("Grand Budapest Hotel", "Wes Anderson", 2014, 99, "Le avventure del concierge di un celebre hotel europeo tra le due guerre mondiali e del suo giovane aiutante.", "Ralph Fiennes, Tony Revolori, Saoirse Ronan, Willem Dafoe", new DateOnly(2014, 3, 7), new[] { "Commedia", "Avventura" }),
            ("La Forma dell'Acqua", "del Toro", 2017, 123, "Un'addetta alle pulizie muta in un laboratorio governativo stringe un legame con una creatura anfibia prigioniera.", "Sally Hawkins, Octavia Spencer, Michael Shannon, Richard Jenkins", new DateOnly(2017, 12, 1), new[] { "Fantasy", "Romantico", "Drammatico" }),
            ("Gravity", "Cuaron", 2013, 91, "Due astronauti rimangono alla deriva nello spazio dopo la distruzione della loro navetta e lottano per sopravvivere.", "Sandra Bullock, George Clooney, Ed Harris", new DateOnly(2013, 10, 4), new[] { "Fantascienza", "Thriller", "Drammatico" }),
            ("Avatar", "Cameron", 2009, 162, "Un ex marine paraplegico viene inviato sul pianeta Pandora, dove si trova diviso tra seguire gli ordini e proteggere il mondo che ha imparato a chiamare casa.", "Sam Worthington, Zoe Saldana, Sigourney Weaver, Stephen Lang", new DateOnly(2009, 12, 18), new[] { "Fantascienza", "Avventura", "Azione" }),
            ("Titanic", "Cameron", 1997, 195, "Una giovane aristocratica si innamora di un artista squattrinato a bordo del Titanic, durante il suo viaggio inaugurale.", "Leonardo DiCaprio, Kate Winslet, Billy Zane, Kathy Bates", new DateOnly(1997, 12, 19), new[] { "Drammatico", "Romantico" }),
            ("Il Signore degli Anelli - Il Ritorno del Re", "Jackson", 2003, 201, "Mentre Frodo e Sam si avvicinano a Mordor, Aragorn guida le forze del bene contro le armate di Sauron nella battaglia finale.", "Elijah Wood, Viggo Mortensen, Ian McKellen, Orlando Bloom", new DateOnly(2003, 12, 17), new[] { "Fantasy", "Avventura", "Azione" }),
            ("C'era una Volta in America", "Leone", 1984, 229, "La storia di un gruppo di gangster ebrei newyorkesi, dall'infanzia fino agli anni '60, attraverso amicizia, tradimento e rimpianto.", "Robert De Niro, James Woods, Elizabeth McGovern, Tuesday Weld", new DateOnly(1984, 6, 1), new[] { "Drammatico", "Storico" }),
            ("2001: Odissea nello Spazio", "Kubrick", 1968, 149, "Un misterioso monolite nero guida l'evoluzione umana dalla preistoria a un viaggio verso Giove e oltre.", "Keir Dullea, Gary Lockwood, William Sylvester, Douglas Rain", new DateOnly(1968, 4, 3), new[] { "Fantascienza", "Avventura" }),
            ("Apocalypse Now", "Coppola", 1979, 147, "Un capitano dell'esercito americano viene inviato in missione nella giungla cambogiana per eliminare un colonnello impazzito.", "Martin Sheen, Marlon Brando, Robert Duvall, Laurence Fishburne", new DateOnly(1979, 8, 15), new[] { "Azione", "Drammatico" }),
            ("The Nightmare Before Christmas", "Burton", 1993, 76, "Jack Skeletron, il re della Città di Halloween, scopre il Natale e decide di portare la festa nel suo mondo.", "Danny Elfman, Chris Sarandon, Catherine O'Hara, William Hickey", new DateOnly(1993, 10, 29), new[] { "Animazione", "Fantasy" }),
            ("Forrest Gump", "Zemeckis", 1994, 142, "La straordinaria vita di un uomo semplice ma dal cuore puro che attraversa i momenti chiave della storia americana.", "Tom Hanks, Robin Wright, Gary Sinise, Sally Field", new DateOnly(1994, 7, 6), new[] { "Drammatico", "Commedia", "Romantico" }),
            ("Gran Torino", "Eastwood", 2008, 116, "Un veterano della guerra di Corea, razzista e burbero, stringe un improbabile legame con il giovane vicino Hmong.", "Clint Eastwood, Bee Vang, Ahney Her, Christopher Carley", new DateOnly(2008, 12, 12), new[] { "Drammatico", "Azione" }),
            ("Chiamami col Tuo Nome", "Guadagnino", 2017, 132, "Nell'estate del 1983, un diciassettenne italo-americano scopre l'amore con uno studente americano ospite nella villa di famiglia in Lombardia.", "Timothee Chalamet, Armie Hammer, Michael Stuhlbarg, Amira Casar", new DateOnly(2017, 1, 22), new[] { "Romantico", "Drammatico" }),
            ("La La Land", "Chazelle", 2016, 128, "Un pianista jazz e un'aspirante attrice si innamorano a Los Angeles mentre inseguono i loro sogni.", "Ryan Gosling, Emma Stone, John Legend, Rosemarie DeWitt", new DateOnly(2016, 12, 9), new[] { "Commedia", "Romantico", "Drammatico" }),
            ("Jojo Rabbit", "Waititi", 2019, 108, "Un ragazzino tedesco della Gioventù Hitleriana scopre che sua madre nasconde una ragazza ebrea in soffitta.", "Roman Griffin Davis, Thomasin McKenzie, Scarlett Johansson, Taika Waititi", new DateOnly(2019, 10, 18), new[] { "Commedia", "Drammatico" }),
            ("Get Out", "Peele", 2017, 104, "Un giovane afroamericano visita la famiglia della sua fidanzata bianca e scopre un inquietante segreto.", "Daniel Kaluuya, Allison Williams, Bradley Whitford, Catherine Keener", new DateOnly(2017, 2, 24), new[] { "Horror", "Thriller" }),
            ("The Revenant", "Inarritu", 2015, 156, "Un esploratore lasciato per morto dai suoi compagni sopravvive a un attacco di orso e intraprende un viaggio di vendetta nel gelido West americano.", "Leonardo DiCaprio, Tom Hardy, Domhnall Gleeson, Will Poulter", new DateOnly(2015, 12, 25), new[] { "Avventura", "Drammatico", "Azione" }),
            ("Trainspotting", "Boyle", 1996, 94, "Un gruppo di giovani tossicodipendenti di Edimburgo cerca di sopravvivere tra sballi, furti e tentativi di disintossicazione.", "Ewan McGregor, Ewen Bremner, Jonny Lee Miller, Robert Carlyle", new DateOnly(1996, 2, 23), new[] { "Drammatico", "Commedia" }),
            ("Il Petroliere", "Paul Thomas Anderson", 2007, 158, "Un cercatore di petrolio spietato e ambizioso si arricchisce all'inizio del XX secolo, scontrandosi con un giovane predicatore.", "Daniel Day-Lewis, Paul Dano, Ciaran Hinds, Kevin J. O'Connor", new DateOnly(2007, 12, 26), new[] { "Drammatico", "Storico" }),
            ("Il Pianista", "Polanski", 2002, 150, "La vera storia del pianista polacco Wladyslaw Szpilman che sopravvisse all'Olocausto nascosto tra le rovine di Varsavia.", "Adrien Brody, Thomas Kretschmann, Emilia Fox, Frank Finlay", new DateOnly(2002, 9, 6), new[] { "Drammatico", "Storico" }),
            ("Il Cigno Nero", "Aronofsky", 2010, 108, "Una ballerina di danza classica ottiene il ruolo principale nel Lago dei Cigni ma la pressione la spinge sull'orlo della follia.", "Natalie Portman, Mila Kunis, Vincent Cassel, Winona Ryder", new DateOnly(2010, 12, 3), new[] { "Drammatico", "Thriller" }),
            ("Memento", "Nolan", 2000, 113, "Un uomo affetto da amnesia anterograda usa tatuaggi e appunti per dare la caccia all'assassino di sua moglie.", "Guy Pearce, Carrie-Anne Moss, Joe Pantoliano, Mark Boone Jr.", new DateOnly(2000, 9, 5), new[] { "Thriller", "Drammatico" }),
            ("Bastardi senza Gloria", "Tarantino", 2009, 153, "Un gruppo di soldati ebrei-americani dà la caccia ai nazisti nella Francia occupata durante la Seconda Guerra Mondiale.", "Brad Pitt, Christoph Waltz, Melanie Laurent, Michael Fassbender", new DateOnly(2009, 8, 21), new[] { "Azione", "Drammatico" }),
            ("Il Buono, il Brutto e il Cattivo", "Leone", 1966, 178, "Tre pistoleri si contendono una fortuna in oro sepolto nel deserto durante la Guerra Civile Americana.", "Clint Eastwood, Lee Van Cleef, Eli Wallach, Aldo Giuffre", new DateOnly(1966, 12, 23), new[] { "Azione", "Avventura" }),
            ("Dune", "Villeneuve", 2021, 155, "Il giovane Paul Atreides viaggia verso il pianeta più pericoloso dell'universo per garantire un futuro alla sua famiglia e al suo popolo.", "Timothee Chalamet, Zendaya, Oscar Isaac, Jason Momoa", new DateOnly(2021, 10, 22), new[] { "Fantascienza", "Avventura", "Azione" })
        };

        var films = new List<Film>();
        foreach (var (titolo, regCognome, anno, durata, desc, cast, rilascio, _) in filmsData)
        {
            if (!registi.TryGetValue(regCognome, out var regista)) continue;
            films.Add(new Film
            {
                Titolo = titolo,
                DataProduzione = new DateTime(anno, 1, 1),
                RegistaId = regista.Id,
                Durata = durata,
                DescrizioneLunga = desc,
                CastText = cast,
                DataRilascio = rilascio
            });
        }

        _context.Films.AddRange(films);
        await _context.SaveChangesAsync();

        var categorie = await _context.Categorie.ToDictionaryAsync(c => c.Nome);
        var filmEntities = await _context.Films.ToDictionaryAsync(f => f.Titolo);

        foreach (var (titolo, _, _, _, _, _, _, cats) in filmsData)
        {
            if (!filmEntities.TryGetValue(titolo, out var film)) continue;
            foreach (var catName in cats)
            {
                if (!categorie.TryGetValue(catName, out var cat)) continue;
                _context.FilmCategorie.Add(new FilmCategoria { FilmId = film.Id, CategoriaId = cat.Id });
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedDevSaleAsync()
    {
        var cinemas = await _context.Cinemas.ToDictionaryAsync(c => c.CodiceLocale);

        var sale = new List<Sala>();

        foreach (var (codice, cinema) in cinemas)
        {
            var num = 1;
            var sala2D = new Sala { CinemaId = cinema.Id, NumeroProgressivo = num++, TipoSala = TipoSala.DueD, Nome = "Sala 1", Supplemento = 0, IsAttiva = true };
            sala2D.ImmagineUrl = GenerateSvgDataUri("2D", "#1a1a2e", "#16213e", "#0f3460");
            sale.Add(sala2D);

            var sala3D = new Sala { CinemaId = cinema.Id, NumeroProgressivo = num++, TipoSala = TipoSala.TreD, Nome = "Sala 2", Supplemento = 2.50m, IsAttiva = true };
            sala3D.ImmagineUrl = GenerateSvgDataUri("3D", "#2d1b69", "#5b2c8e", "#8b5cf6");
            sale.Add(sala3D);

            if (codice is "CBR001" or "CBM001" or "CBT001" or "CBN001")
            {
                var salaIsense = new Sala { CinemaId = cinema.Id, NumeroProgressivo = num++, TipoSala = TipoSala.ISENSE, Nome = "Sala ISENSE", Supplemento = 4.50m, IsAttiva = true };
                salaIsense.ImmagineUrl = GenerateSvgDataUri("ISENSE", "#5c3d0e", "#8b6914", "#d4a017");
                sale.Add(salaIsense);
            }

            if (codice is "CBM001" or "CBR001" or "CBF001" or "CBB001")
            {
                var salaXL = new Sala { CinemaId = cinema.Id, NumeroProgressivo = num++, TipoSala = TipoSala.XL, Nome = "Sala XL", Supplemento = 3.50m, IsAttiva = true };
                salaXL.ImmagineUrl = GenerateSvgDataUri("XL", "#064e3b", "#065f46", "#10b981");
                sale.Add(salaXL);
            }
        }

        _context.Sale.AddRange(sale);
        await _context.SaveChangesAsync();
    }

    private static string GenerateSvgDataUri(string label, string bg1, string bg2, string accent)
    {
        var svg = $"<svg xmlns='http://www.w3.org/2000/svg' width='800' height='400'><defs><linearGradient id='g' x1='0%' y1='0%' x2='100%' y2='100%'><stop offset='0%' style='stop-color:{bg1}'/><stop offset='100%' style='stop-color:{bg2}'/></linearGradient></defs><rect width='800' height='400' fill='url(#g)'/><text x='400' y='170' text-anchor='middle' fill='white' font-size='56' font-family='Arial,sans-serif' font-weight='bold'>{label}</text><text x='400' y='230' text-anchor='middle' fill='{accent}' font-size='22' font-family='Arial,sans-serif' letter-spacing='8'>CINEBASE</text><rect x='250' y='260' width='300' height='3' rx='2' fill='{accent}' opacity='0.5'/></svg>";
        var bytes = System.Text.Encoding.UTF8.GetBytes(svg);
        return $"data:image/svg+xml;base64,{Convert.ToBase64String(bytes)}";
    }

    public static async Task UpdateSalaImmaginiAsync(FilmDbContext context)
    {
        var saleSenzaImmagine = context.Sale.Where(s => s.ImmagineUrl == null).ToList();
        if (saleSenzaImmagine.Count == 0) return;

        foreach (var sala in saleSenzaImmagine)
        {
            sala.ImmagineUrl = sala.TipoSala switch
            {
                TipoSala.ISENSE => GenerateSvgDataUri("ISENSE", "#5c3d0e", "#8b6914", "#d4a017"),
                TipoSala.XL => GenerateSvgDataUri("XL", "#064e3b", "#065f46", "#10b981"),
                TipoSala.TreD => GenerateSvgDataUri("3D", "#2d1b69", "#5b2c8e", "#8b5cf6"),
                _ => GenerateSvgDataUri("2D", "#1a1a2e", "#16213e", "#0f3460")
            };
        }

        await context.SaveChangesAsync();
    }

    private async Task SeedDevPostiAsync()
    {
        var sale = await _context.Sale.ToListAsync();
        var posti = new List<SalaPosto>();

        foreach (var sala in sale)
        {
            var (totalRows, sideSeats, centerSeats, vipSeats) = sala.TipoSala switch
            {
                TipoSala.ISENSE => (18, 5, 24, 14),
                TipoSala.XL => (17, 5, 22, 14),
                TipoSala.TreD => (15, 4, 20, 12),
                _ => (14, 4, 18, 10)
            };

            for (var fila = 1; fila <= totalRows; fila++)
            {
                if (fila <= 5)
                {
                    AddPosti(sala.Id, "PLATEA-SX", fila, sideSeats, 1, fila);
                    AddPosti(sala.Id, "PLATEA-CENTRO", fila, centerSeats, 7, fila);
                    AddPosti(sala.Id, "PLATEA-DX", fila, sideSeats, 7 + centerSeats + 3, fila);
                }
                else if (fila <= totalRows - 2)
                {
                    AddPosti(sala.Id, "GALLERIA-SX", fila, Math.Max(2, sideSeats - 1), 3, fila + 1);
                    AddPosti(sala.Id, "GALLERIA-CENTRO", fila, centerSeats - 2, 8, fila + 1);
                    AddPosti(sala.Id, "GALLERIA-DX", fila, Math.Max(2, sideSeats - 1), 9 + centerSeats, fila + 1);
                }
                else
                {
                    AddPosti(sala.Id, "VIP", fila, vipSeats, 10, fila + 2);
                }
            }

            posti.Add(new SalaPosto { SalaId = sala.Id, Settore = "ACCESS-SX", Fila = totalRows, Numero = 1, PosX = 5, PosY = totalRows + 3, IsWheelchair = true, IsAttivo = true });
            posti.Add(new SalaPosto { SalaId = sala.Id, Settore = "ACCESS-DX", Fila = totalRows, Numero = 1, PosX = 10 + vipSeats + 2, PosY = totalRows + 3, IsWheelchair = true, IsAttivo = true });
        }

        _context.SalaPosti.AddRange(posti);
        await _context.SaveChangesAsync();
        return;

        void AddPosti(int salaId, string settore, int fila, int seats, int startX, int posY)
        {
            for (var posto = 1; posto <= seats; posto++)
            {
                posti.Add(new SalaPosto
                {
                    SalaId = salaId,
                    Settore = settore,
                    Fila = fila,
                    Numero = posto,
                    PosX = startX + posto - 1,
                    PosY = posY,
                    IsWheelchair = false,
                    IsAttivo = true
                });
            }
        }
    }

    private async Task SeedDevShowsAsync()
    {
        var rawDefaultTicketPrice = Environment.GetEnvironmentVariable("DEFAULT_TICKET_PRICE");
        var defaultTicketPrice = !string.IsNullOrWhiteSpace(rawDefaultTicketPrice)
            && (decimal.TryParse(rawDefaultTicketPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var price)
                || decimal.TryParse(rawDefaultTicketPrice, NumberStyles.Number, CultureInfo.GetCultureInfo("it-IT"), out price)
                || decimal.TryParse(rawDefaultTicketPrice, out price))
            ? price
            : 8.50m;

        var cinemas = await _context.Cinemas.ToListAsync();
        var films = await _context.Films.ToListAsync();
        var sale = await _context.Sale.Include(s => s.Cinema).ToListAsync();

        var shows = new List<Show>();
        var baseDate = DateTime.UtcNow.Date;
        var rng = new Random(42);

        var timeSlots = new[] { 14, 16, 18, 20, 22 };

        for (var day = 0; day < 7; day++)
        {
            var date = baseDate.AddDays(day);

            foreach (var sala in sale)
            {
                var cinema = sala.Cinema!;
                var slotCount = rng.Next(2, 4);
                var shuffledSlots = timeSlots.OrderBy(_ => rng.Next()).Take(slotCount).OrderBy(t => t).ToList();

                foreach (var hour in shuffledSlots)
                {
                    var film = films
                        .Where(f => f.Durata <= 240)
                        .OrderBy(_ => rng.Next())
                        .First();

                    shows.Add(new Show
                    {
                        CinemaId = cinema.Id,
                        SalaId = sala.Id,
                        FilmId = film.Id,
                        StartAtUtc = new DateTime(date.Year, date.Month, date.Day, hour, 0, 0, DateTimeKind.Utc),
                        DurataMinutiSnapshot = film.Durata,
                        PrezzoBase = defaultTicketPrice,
                        SupplementoSala = sala.Supplemento
                    });
                }
            }
        }

        _context.Shows.AddRange(shows);
        await _context.SaveChangesAsync();
    }
}
