# RESOCONTO TECNICO — CineBase (RedCurtain)

> Progetto d'esame — Applicazione full-stack per la gestione di un circuito di cinema multisala con acquisto biglietti online.

---

## 1. PANORAMICA DEL PROGETTO

**CineBase** (brand name: *RedCurtain*) e un'applicazione web full-stack che consente a un circuito di cinema multisala di:

- **Pubblicare la programmazione** dei film in sala, con dettagli su orari, sale e prezzi
- **Vendere biglietti online** con selezione grafica del posto a sedere
- **Gestire l'intero flusso di ticketing**: prenotazione posti (hold), ordine, pagamento (credito + carta Stripe), emissione biglietti PDF con QR code
- **Amministrare il sistema** tramite un pannello admin per CRUD di film, registi, cinema, sale, proiezioni, utenti e ricariche credito

Il progetto e strutturato come una **solution .NET 9** composta da 4 progetti:
- `FilmAPI` — backend API REST
- `CineBase.Web` — frontend web (file statici serviti da ASP.NET Core)
- `FilmAPI.Tests` — test unitari e di integrazione
- `FilmApiSeeder` — seeder standalone per popolare il database con dati iniziali da TMDB

---

## 2. ARCHITETTURA GENERALE

### 2.1 Modello a tre livelli (3-Tier)

```
┌──────────────────────────────────────────────────────────────────┐
│                      DOCKER COMPOSE (3 container)                 │
│                                                                  │
│  ┌─────────────────────┐  ┌─────────────────────┐  ┌───────────┐ │
│  │   frontend           │  │   backend            │  │  database  │ │
│  │   CineBase.Web       │  │   FilmAPI             │  │  MariaDB   │ │
│  │   ASP.NET Core       │  │   ASP.NET Core        │  │            │ │
│  │   Static File Server │  │   Minimal API (.NET 9)│  │  :3306     │ │
│  │   :5001              │  │   :5000               │  │            │ │
│  └─────────┬────────────┘  └───────────┬───────────┘  └─────┬─────┘ │
│            │                           │                      │      │
│            └──────── HTTP/REST ────────┘                      │      │
│                        chiamate API                            │      │
│                                       └───── EF Core ────────┘       │
│                                             connessione MySQL        │
└──────────────────────────────────────────────────────────────────┘
```

**Frontend** (`CineBase.Web` — porta `:5001`):
- Server ASP.NET Core minimale che serve file statici (HTML, CSS, JS)
- Single Page Application (SPA) in **Vanilla JavaScript** (nessun framework come React o Vue)
- Autenticazione interamente lato client tramite JWT salvato in `localStorage`
- Tutta la logica di business lato frontend e gestita dai file JS nella cartella `wwwroot/js/`

**Backend** (`FilmAPI` — porta `:5000`):
- **ASP.NET Core Minimal API** su .NET 9
- Architettura a servizi (service layer pattern) con interfacce e implementazioni
- Entity Framework Core come ORM per l'accesso al database
- JWT Bearer authentication con refresh token

**Database** (MariaDB — porta `:3306`):
- MariaDB 10.11, compatibile MySQL
- 19 tabelle, gestite con EF Core Code-First (13 migrazioni)
- Relazioni complesse: many-to-many, one-to-many con vincoli di integrita referenziale

### 2.2 Comunicazione

Il frontend comunica con il backend esclusivamente tramite **chiamate HTTP REST** autenticate via JWT Bearer token. Il modulo `js/api.js` (581 righe) funge da client HTTP centralizzato con:
- Refresh automatico del token quando scade (interceptor pattern)
- Gestione centralizzata degli errori
- Supporto per tutte le operazioni CRUD

---

## 3. STACK TECNOLOGICO

### 3.1 Backend

| Tecnologia | Versione | Ruolo |
|---|---|---|
| .NET SDK | 9.0 | Runtime e framework |
| ASP.NET Core Minimal API | 9.0 | Web framework REST API |
| Entity Framework Core | 9.0.4 | Object-Relational Mapper (ORM) |
| Pomelo.EntityFrameworkCore.MySql | 9.0.0 | Provider EF Core per MySQL/MariaDB |
| Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.4 | Autenticazione JWT |
| BCrypt.Net-Next | 4.1.0 | Hashing delle password (BCrypt) |
| MailKit | 4.16.0 | Invio email via SMTP |
| Stripe.net | 48.2.0 | Gateway pagamenti Stripe |
| QuestPDF | 2026.2.4 | Generazione PDF per biglietti |
| QRCoder | 1.8.0 | Generazione QR code per biglietti |
| ZXing.Net | 0.16.11 | Encoding barcode |
| NSwag.AspNetCore | 14.6.3 | Generazione documentazione Swagger/OpenAPI |
| DotNetEnv | 3.1.1 | Caricamento variabili d'ambiente da file `.env` |

### 3.2 Frontend

| Tecnologia | Ruolo |
|---|---|
| HTML5 / CSS3 / Vanilla JavaScript | Struttura, stile e logica — **nessun framework JS** |
| Tailwind CSS (CDN) | Utility-first CSS framework per lo styling |
| Font Awesome 6.4.0 (CDN) | Icone |
| Google Fonts (Inter) | Tipografia |
| Stripe.js (CDN) | Hosted Checkout Stripe (reindirizzamento) |
| localStorage | Gestione dello stato lato client (token JWT, preferenze, carrello) |

### 3.3 Database

| Tecnologia | Versione |
|---|---|
| MariaDB | 10.11 |
| Driver Pomelo MySQL | 9.0.0 (compatibile MariaDB) |

### 3.4 DevOps e Infrastruttura

| Tecnologia | Ruolo |
|---|---|
| Docker Compose | Orchestrazione dei 3 container (db, backend, frontend) |
| GitHub | Version control |
| `.env` file | Configurazione centralizzata (72 variabili d'ambiente) |

### 3.5 Testing

| Tecnologia | Versione | Ruolo |
|---|---|---|
| xUnit | 2.9.2 | Framework di test |
| FluentAssertions | 8.8.0 | Assertion fluent e leggibili |
| Moq | 4.20.72 | Mocking per unit test |
| Microsoft.AspNetCore.Mvc.Testing | 9.0.4 | Integration test via WebApplicationFactory |
| EF Core InMemory + SQLite | 9.0.4 | Database in memoria per integration test |
| Coverlet | 6.0.4 | Code coverage |
| PdfPig | 0.1.14 | Parsing e validazione PDF generati |

### 3.6 API Esterne Integrate

| Servizio | Ruolo |
|---|---|
| **TMDB API v3** (The Movie Database) | Ricerca e import film, poster, trailer, cast, generi |
| **Stripe API** | Gateway pagamenti con Hosted Checkout + Webhook |
| **Google OAuth 2.0 / OpenID Connect** | Social login |
| **Microsoft Entra ID / OpenID Connect** | Social login |
| **SMTP** (Gmail / SendGrid) | Invio email transazionali (biglietti PDF, verifica, reset password) |

---

## 4. SCHEMA DEL DATABASE

### 4.1 Modello Entità-Relazione Concettuale

Il database gestisce le seguenti entita e relazioni principali:

```
┌──────────┐       ┌────────────────┐       ┌─────────────┐
│ REGISTI  │──1:N──│     FILMS      │──M:N──│  CATEGORIE  │
└──────────┘       └───────┬────────┘       └─────────────┘
                           │ 1:N
                           ▼
                    ┌──────────┐
                    │  SHOWS   │ (proiezioni)
                    └────┬─────┘
                    1:N  │  1:1
              ┌──────────┼──────────┐
              ▼          ▼          ▼
        ┌─────────┐ ┌───────┐ ┌──────────────┐
        │ CINEMAS │ │ SALA  │ │SHOWPOSTISTATO│
        └────┬────┘ └───┬───┘ └──────────────┘
             │ 1:N       │ 1:N
             ▼           ▼
      ┌──────────┐ ┌───────────┐
      │  SHOWS   │ │ SALAPOSTI │ (piantina posti)
      └──────────┘ └───────────┘

┌──────────┐       ┌──────────┐       ┌───────────┐
│  USERS   │──1:N──│  ORDINI  │──1:N──│ BIGLIETTI │
└────┬─────┘       └──────────┘       └───────────┘
     │ 1:N
     ├──────────────► REFRESHTOKENS
     ├──────────────► USEREXTERNALLOGINS (Google/Microsoft)
     ├──────────────► MOVIMENTICREDITO (wallet)
     ├──────────────► VALUTAZIONIFILM (rating 1-5)
     ├──────────────► ACCOUNTACTIONTOKENS (reset pwd, verifica)
     └──────────────► USERSECURITYAUDITLOG
```

### 4.2 Tabelle Dettagliate

#### Tabella: Films (Film)
| Colonna | Tipo | Descrizione |
|---|---|---|
| Id | INT PK | Identificativo univoco |
| Titolo | VARCHAR | Titolo del film |
| Durata | INT | Durata in minuti |
| Descrizione | TEXT | Sinossi/trama |
| Cast | TEXT | Elenco attori principali |
| RegistaId | INT FK | Riferimento al regista |
| Anno | INT | Anno di uscita |
| PosterUrl | VARCHAR | URL del poster (da TMDB o locale) |
| TrailerUrl | VARCHAR | URL del trailer YouTube |
| DataInizioProgrammazione | DATE | Inizio programmazione nel circuito |
| DataFineProgrammazione | DATE | Fine programmazione |
| TmdbId | INT? | ID su The Movie Database (nullable) |
| RatingMedio | DOUBLE? | Media delle valutazioni utenti (nullable) |

#### Tabella: Registi
| Colonna | Tipo | Descrizione |
|---|---|---|
| Id | INT PK | Identificativo |
| Nome | VARCHAR | Nome |
| Cognome | VARCHAR | Cognome |
| Nazionalita | VARCHAR | Nazione di origine |

#### Tabella: Categorie (Generi)
| Colonna | Tipo | Descrizione |
|---|---|---|
| Id | INT PK | Identificativo |
| Nome | VARCHAR | Nome del genere (es. Azione, Commedia, Horror) |

#### Tabella: FilmCategorie (Many-to-Many)
| Colonna | Tipo | Descrizione |
|---|---|---|
| FilmId | INT PK, FK | Riferimento al film |
| CategoriaId | INT PK, FK | Riferimento alla categoria |

#### Tabella: Cinemas
| Colonna | Tipo | Descrizione |
|---|---|---|
| Id | INT PK | Identificativo |
| Nome | VARCHAR | Nome del cinema |
| Indirizzo | VARCHAR | Indirizzo fisico |
| Citta | VARCHAR | Citta |
| Latitudine | DOUBLE | Coordinata GPS |
| Longitudine | DOUBLE | Coordinata GPS |

#### Tabella: Sale
| Colonna | Tipo | Descrizione |
|---|---|---|
| Id | INT PK | Identificativo |
| Nome | VARCHAR | Nome della sala |
| CinemaId | INT FK | Cinema di appartenenza |
| Tipo | ENUM | Tipologia: 2D, 3D, ISENSE, XL |
| Supplemento | DECIMAL | Costo aggiuntivo sul prezzo base |
| FileDimensioneX | INT | Larghezza in pixel della piantina |
| FileDimensioneY | INT | Altezza in pixel della piantina |
| PostiPerFila | INT | Posti per fila (per sale regolari) |
| NumeroFile | INT | Numero file (per sale regolari) |

#### Tabella: SalaPosti (Piantina posti)
| Colonna | Tipo | Descrizione |
|---|---|---|
| Id | INT PK | Identificativo |
| SalaId | INT FK | Sala di appartenenza |
| Settore | VARCHAR | Settore (Platea, Galleria, ecc.) |
| Fila | VARCHAR | Lettera della fila (A, B, C...) |
| Numero | INT | Numero del posto |
| PosX | INT | Coordinata X nella piantina (px) |
| PosY | INT | Coordinata Y nella piantina (px) |
| AccessibileDisabili | BOOL | Posto accessibile |
| PostoDisabili | BOOL | Posto riservato disabili |

#### Tabella: Shows (Proiezioni)
| Colonna | Tipo | Descrizione |
|---|---|---|
| Id | INT PK | Identificativo |
| FilmId | INT FK | Film proiettato |
| CinemaId | INT FK | Cinema |
| SalaId | INT FK | Sala |
| Orario | DATETIME (UTC) | Data e ora della proiezione |
| Prezzo | DECIMAL | Prezzo del biglietto per questa proiezione |
| VersioneLingua | VARCHAR | Lingua (Originale, Doppiato ITA, ecc.) |

#### Tabella: ShowPostiStato (Stato posti per proiezione)
| Colonna | Tipo | Descrizione |
|---|---|---|
| Id | INT PK | Identificativo |
| ShowId | INT FK | Proiezione |
| SalaPostoId | INT FK | Posto a sedere |
| Stato | ENUM | Hold (prenotato temporaneamente) o Sold (venduto) |
| HoldToken | VARCHAR? | Token univoco per il rilascio dell'hold |
| HoldExpiry | DATETIME? | Scadenza dell'hold (default 10 minuti) |
| UserId | INT FK? | Utente che ha acquistato/prenotato |
| OrdineId | INT FK? | Ordine associato |

#### Tabella: Users (Utenti)
| Colonna | Tipo | Descrizione |
|---|---|---|
| Id | INT PK | Identificativo |
| Nome | VARCHAR | Nome |
| Cognome | VARCHAR | Cognome |
| Email | VARCHAR(UNIQUE) | Email (usata come username) |
| PasswordHash | VARCHAR | Hash BCrypt della password |
| Ruolo | ENUM | User, PowerUser, Admin |
| Credito | DECIMAL | Saldo credito disponibile |
| CinemaPreferitoId | INT FK? | Cinema preferito |
| FilmPreferitoId | INT FK? | Film preferito |
| ConsensoPrivacy | BOOL | Accettazione privacy policy |
| ConsensoTermini | BOOL | Accettazione termini di servizio |
| DataRegistrazione | DATETIME | Data creazione account |
| EmailVerificata | BOOL | Stato verifica email |
| AuthVersion | INT | Versione autenticazione (per invalidazione globale token) |
| TentativiLoginFalliti | INT | Contatore tentativi errati |
| BloccoLoginFinoA | DATETIME? | Data fine blocco temporaneo |

#### Tabella: RefreshTokens
| Colonna | Tipo | Descrizione |
|---|---|---|
| Id | INT PK | Identificativo |
| UserId | INT FK | Utente proprietario |
| Token | VARCHAR(UNIQUE) | Token JWT di refresh |
| DeviceId | VARCHAR | Identificativo dispositivo |
| CreatedAt | DATETIME | Data creazione |
| ExpiresAt | DATETIME | Data scadenza (7 giorni) |
| RevokedAt | DATETIME? | Data revoca (logout) |

#### Tabella: Ordini
| Colonna | Tipo | Descrizione |
|---|---|---|
| Id | INT PK | Identificativo |
| UserId | INT FK | Utente acquirente |
| Stato | ENUM | Pending, Paid, Failed, Cancelled, Expired, CheckoutInProgress |
| Totale | DECIMAL | Importo totale |
| StripeSessionId | VARCHAR? | ID sessione Stripe |
| CreatedAt | DATETIME | Data creazione |
| PaidAt | DATETIME? | Data pagamento |

#### Tabella: Biglietti
| Colonna | Tipo | Descrizione |
|---|---|---|
| Id | INT PK | Identificativo |
| OrdineId | INT FK | Ordine di appartenenza |
| ShowId | INT FK | Proiezione |
| SalaPostoId | INT FK | Posto specifico |
| Codice | VARCHAR(UNIQUE) | Codice alfanumerico univoco |
| Barcode | VARCHAR | Codice a barre |
| Prezzo | DECIMAL | Prezzo pagato |
| Stato | ENUM | Issued, Validated, Cancelled, Refunded |
| DataValidazione | DATETIME? | Quando e stato validato all'ingresso |
| ValidatoreId | INT FK? | Operatore che ha validato |

#### Tabella: MovimentiCredito (Wallet)
| Colonna | Tipo | Descrizione |
|---|---|---|
| Id | INT PK | Identificativo |
| UserId | INT FK | Utente |
| Tipo | ENUM | TopUp (ricarica), DebitOrder (acquisto), Refund (rimborso), Adjustment (rettifica) |
| Importo | DECIMAL | Importo (positivo = accredito, negativo = addebito) |
| Descrizione | VARCHAR | Causale |
| Data | DATETIME | Data movimento |

#### Tabella: AccountActionTokens
| Colonna | Tipo | Descrizione |
|---|---|---|
| Id | INT PK | Identificativo |
| UserId | INT FK | Utente destinatario |
| Token | VARCHAR(UNIQUE) | Token monouso |
| Purpose | ENUM | PasswordReset, SetPassword, EmailVerification, AdminInvite |
| CreatedAt | DATETIME | Data creazione |
| ExpiresAt | DATETIME | Data scadenza |
| UsedAt | DATETIME? | Data utilizzo |

#### Tabella: UserExternalLogins
| Colonna | Tipo | Descrizione |
|---|---|---|
| Id | INT PK | Identificativo |
| UserId | INT FK | Utente locale associato |
| Provider | ENUM | Google, Microsoft |
| ProviderUserId | VARCHAR | ID utente sul provider esterno |
| Email | VARCHAR | Email sul provider esterno |

#### Tabella: ExternalAuthStates
| Colonna | Tipo | Descrizione |
|---|---|---|
| Id | INT PK | Identificativo |
| State | VARCHAR(UNIQUE) | Parametro `state` anti-CSRF per OAuth2 |
| RedirectUrl | VARCHAR | URL a cui reindirizzare dopo il login |
| CreatedAt | DATETIME | Data creazione |
| ExpiresAt | DATETIME | Scadenza (10 minuti) |

#### Tabella: ExternalAuthExchangeCodes
| Colonna | Tipo | Descrizione |
|---|---|---|
| Id | INT PK | Identificativo |
| Code | VARCHAR(UNIQUE) | Codice di scambio monouso |
| AccessToken | VARCHAR | JWT access token da scambiare |
| RefreshToken | VARCHAR | JWT refresh token da scambiare |
| CreatedAt | DATETIME | Data creazione |
| ExpiresAt | DATETIME | Scadenza (5 minuti) |
| UsedAt | DATETIME? | Data utilizzo |

#### Tabella: UserSecurityAuditLog
| Colonna | Tipo | Descrizione |
|---|---|---|
| Id | INT PK | Identificativo |
| UserId | INT FK? | Utente coinvolto |
| Evento | VARCHAR | Tipo evento (LoginSuccess, LoginFailed, PasswordChanged, ecc.) |
| Dettaglio | TEXT | Dettagli aggiuntivi |
| IpAddress | VARCHAR | Indirizzo IP |
| DataOra | DATETIME | Timestamp evento |

#### Tabella: ValutazioniFilm (Rating)
| Colonna | Tipo | Descrizione |
|---|---|---|
| Id | INT PK | Identificativo |
| UserId | INT FK | Utente che valuta |
| FilmId | INT FK | Film valutato |
| Stelle | INT | Valutazione da 1 a 5 |
| Data | DATETIME | Data valutazione |

### 4.3 Indici e Vincoli

- **Uniqueness**: Email utenti, Token refresh, Codice biglietto, Token azioni
- **Foreign Keys**: Tutte le relazioni hanno vincoli FK con eliminazione a cascata dove appropriato
- **Indici**: Su colonne usate frequentemente nei filtri (orario proiezioni, email utenti, stato ordini)
- **Seed iniziale**: 12 categorie (generi), 1 admin predefinito, dati di sviluppo opzionali (cinema, film, sale, proiezioni)

---

## 5. BACKEND — API REST

### 5.1 Struttura del progetto FilmAPI

```
backend/FilmAPI/
├── Program.cs                    Entry point (294 righe)
├── FilmAPI.csproj                Progetto .NET 9 con 15+ pacchetti NuGet
├── appsettings.json              Configurazione applicativa
├── Dockerfile                    Build multi-stage per container
├── Data/
│   ├── FilmDbContext.cs          EF Core DbContext (336 righe) - 19 DbSet
│   └── DataSeeder.cs             Seed iniziale (426 righe)
├── Model/ (28 file)              Classi entita
├── DTO/ (15 file)                Data Transfer Objects
├── Endpoints/ (19 file)          Definizione route Minimal API
├── Services/ (57 file)           Service layer (interfacce + implementazioni)
└── Migrations/ (25 file)         13 migrazioni EF Core + snapshot
```

### 5.2 Minimal API — Pattern

Il backend usa il pattern **Minimal API** di ASP.NET Core 9. Invece di usare i Controller tradizionali, le route sono definite tramite `MapGroup()` e `MapGet/MapPost/MapPut/MapDelete` direttamente in `Program.cs` o in file di estensione nella cartella `Endpoints/`.

**Esempio di struttura di un endpoint:**
```csharp
// In Program.cs
var authGroup = app.MapGroup("/auth");
authGroup.MapPost("/login", async (LoginRequest req, IAuthService svc) => { ... });
authGroup.MapPost("/register", async (RegisterRequest req, IAuthService svc) => { ... });

var filmsGroup = app.MapGroup("/films");
filmsGroup.MapGet("/", async (IFilmService svc, int page, string? search) => { ... })
    .RequireAuthorization(); // opzionale
```

### 5.3 Dependency Injection

Tutti i servizi sono registrati nel container DI di ASP.NET Core in `Program.cs`:
```csharp
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFilmService, FilmService>();
builder.Services.AddScoped<IStripeGateway, StripeGateway>();
// ... ~30 servizi registrati
builder.Services.AddDbContext<FilmDbContext>(opts => opts.UseMySql(connectionString));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...);
```

### 5.4 Mappatura Completa degli Endpoint

#### Gruppo: `/auth` — Autenticazione locale

| Method | Path | Auth Richiesta | Descrizione |
|---|---|---|---|
| POST | `/auth/register` | Public | Registrazione nuovo utente (nome, cognome, email, password, consensi) |
| POST | `/auth/login` | Public | Login — restituisce `access_token` JWT (15 min) + `refresh_token` (7 giorni) |
| POST | `/auth/refresh` | Public | Rinnova access token usando il refresh token |
| POST | `/auth/logout` | Authenticated | Revoca il refresh token corrente |
| GET | `/auth/me` | Authenticated | Dati dell'utente autenticato |
| POST | `/auth/change-password` | Authenticated | Cambio password (richiede password attuale) |
| POST | `/auth/forgot-password` | Public | Invia email con link per reset password |
| POST | `/auth/reset-password` | Public | Reimposta password tramite token monouso |
| POST | `/auth/set-password` | Public | Imposta password per primo accesso (da invito admin) |
| POST | `/auth/verify-email` | Public | Verifica indirizzo email tramite token |
| GET | `/auth/security/me` | Authenticated | Info sicurezza (ultimi accessi, tentativi falliti) |
| POST | `/auth/set-password/request` | Authenticated | Richiedi nuovo link per impostare password |

#### Gruppo: `/auth/external` — Social Login (OAuth2/OpenID Connect)

| Method | Path | Auth | Descrizione |
|---|---|---|---|
| GET | `/auth/external/providers` | Public | Elenca provider disponibili (Google, Microsoft) |
| GET | `/auth/external/google/start` | Public | Redirect a Google OAuth2 — genera `state` anti-CSRF |
| GET | `/auth/external/google/callback` | Public | Callback Google — scambia `code` per token utente Google |
| GET | `/auth/external/microsoft/start` | Public | Redirect a Microsoft Entra ID |
| GET | `/auth/external/microsoft/callback` | Public | Callback Microsoft |
| POST | `/auth/external/exchange` | Public | Scambia `exchange_code` (ottenuto da callback) per JWT locali |

**Flusso social login:**
1. Frontend reindirizza a `/auth/external/google/start`
2. Backend genera `state` → salva in `ExternalAuthStates` → redirect a Google
3. Utente accetta → Google reindirizza a `/auth/external/google/callback` con `code`
4. Backend scambia `code` per token Google, ottiene email/nome, crea/associa utente locale
5. Backend reindirizza a `social-login-complete.html?code=EXCHANGE_CODE`
6. Frontend chiama `POST /auth/external/exchange` con l'exchange code
7. Backend restituisce JWT access + refresh token → frontend salva in localStorage

#### Gruppo: `/films` — Gestione Film

| Method | Path | Auth | Descrizione |
|---|---|---|---|
| GET | `/films` | Public | Lista film con paginazione, ricerca per titolo |
| GET | `/films/{id}` | Public | Dettaglio singolo film |
| GET | `/films/{id}/scheda` | Public | Scheda film con proiezioni disponibili |
| POST | `/films` | PowerUserOrAdmin | Crea nuovo film |
| PUT | `/films/{id}` | PowerUserOrAdmin | Aggiorna film esistente |
| DELETE | `/films/{id}` | PowerUserOrAdmin | Elimina film |

#### Gruppo: `/registi` — Gestione Registi

| Method | Path | Auth | Descrizione |
|---|---|---|---|
| GET | `/registi` | Public | Lista con paginazione e ricerca |
| GET | `/registi/{id}` | Public | Dettaglio regista |
| GET | `/registi/{id}/films` | Public | Film diretti dal regista |
| POST | `/registi` | PowerUserOrAdmin | Crea regista |
| PUT | `/registi/{id}` | PowerUserOrAdmin | Aggiorna regista |
| DELETE | `/registi/{id}` | PowerUserOrAdmin | Elimina regista |

#### Gruppo: `/cinemas` — Gestione Cinema

| Method | Path | Auth | Descrizione |
|---|---|---|---|
| GET | `/cinemas` | Public | Lista cinema con paginazione |
| GET | `/cinemas/{id}` | Public | Dettaglio cinema con sale |
| POST | `/cinemas` | AdminOnly | Crea cinema |
| PUT | `/cinemas/{id}` | AdminOnly | Aggiorna cinema |
| DELETE | `/cinemas/{id}` | AdminOnly | Elimina cinema |

#### Gruppo: `/categorie` — Gestione Categorie (Generi)

| Method | Path | Auth | Descrizione |
|---|---|---|---|
| GET | `/categorie` | Public | Lista completa categorie |
| GET | `/categorie/{id}` | Public | Dettaglio categoria |
| POST | `/categorie` | PowerUserOrAdmin | Crea categoria |
| PUT | `/categorie/{id}` | PowerUserOrAdmin | Aggiorna categoria |
| DELETE | `/categorie/{id}` | PowerUserOrAdmin | Elimina categoria |

#### Gruppo: `/cinemas/{cinemaId}/sale` e `/sale` — Gestione Sale

| Method | Path | Auth | Descrizione |
|---|---|---|---|
| GET | `/cinemas/{cinemaId}/sale` | Public | Sale di un cinema specifico |
| POST | `/cinemas/{cinemaId}/sale` | PowerUserOrAdmin | Crea nuova sala |
| GET | `/sale/{salaId}` | Public | Dettaglio sala |
| PUT | `/sale/{salaId}` | PowerUserOrAdmin | Aggiorna sala |
| DELETE | `/sale/{salaId}` | PowerUserOrAdmin | Elimina sala |
| **GET** | **`/sale/{salaId}/posti`** | **Public** | **Piantina posti a sedere (array di SalaPosto)** |
| **PUT** | **`/sale/{salaId}/posti`** | **PowerUserOrAdmin** | **Salva/modifica piantina personalizzata** |

#### Gruppo: `/shows` — Gestione Proiezioni

| Method | Path | Auth | Descrizione |
|---|---|---|---|
| GET | `/shows` | Public | Lista proiezioni con filtri (cinema, film, data) |
| GET | `/shows/{id}` | Public | Dettaglio proiezione |
| POST | `/shows` | PowerUserOrAdmin | Crea proiezione |
| PUT | `/shows/{id}` | PowerUserOrAdmin | Aggiorna proiezione |
| DELETE | `/shows/{id}` | PowerUserOrAdmin | Elimina proiezione |

#### Gruppo: `/programmazione` — Programmazione Pubblica

| Method | Path | Auth | Descrizione |
|---|---|---|---|
| GET | `/programmazione/films` | Public | Film attualmente in programmazione (filtri, tabs, ricerca) |
| GET | `/programmazione/cinemas` | Public | Cinema con coordinate GPS per mappa |
| GET | `/my-cinemas` | Public | Lista cinema del circuito |
| GET | `/my-cinemas/{cinemaId}/schedule` | Public | Programmazione giornaliera per cinema |

#### Gruppo: `/checkout` — Flusso di Acquisto (CORE)

| Method | Path | Auth | Descrizione |
|---|---|---|---|
| **GET** | **`/checkout/shows/{showId}/seat-map`** | **Authenticated** | **Mappa posti con stato (libero/hold/sold) per una proiezione** |
| **POST** | **`/checkout/holds`** | **Authenticated** | **Blocca posti selezionati (hold — 10 min TTL)** |
| **POST** | **`/checkout/holds/{holdToken}/refresh`** | **Authenticated** | **Rinnova hold (altri 10 minuti)** |
| **DELETE** | **`/checkout/holds/{holdToken}`** | **Authenticated** | **Rilascia hold (libera i posti)** |
| **POST** | **`/checkout/orders`** | **Authenticated** | **Crea ordine dai posti in hold** |
| GET | `/checkout/orders` | Authenticated | Storico ordini utente |
| GET | `/checkout/orders/{orderId}` | Authenticated | Dettaglio ordine |
| GET | `/checkout/orders/{orderId}/pdf` | Authenticated | Scarica PDF con tutti i biglietti dell'ordine |
| **POST** | **`/checkout/orders/{orderId}/pay`** | **Authenticated** | **Paga ordine con credito + eventuale differenza con carta** |
| POST | `/checkout/orders/{orderId}/cancel` | Authenticated | Annulla ordine (libera posti) |
| **POST** | **`/checkout/orders/{orderId}/stripe-checkout-session`** | **Authenticated** | **Crea sessione Stripe Hosted Checkout** |
| GET | `/checkout/orders/{orderId}/checkout-status` | Authenticated | Verifica stato pagamento Stripe |
| POST | `/checkout/orders/{orderId}/reconcile-checkout-session` | Authenticated | Riconcilia sessione Stripe dopo pagamento |
| GET | `/checkout/tickets` | Authenticated | Biglietti dell'utente |
| GET | `/checkout/tickets/{ticketId}` | Authenticated | Dettaglio biglietto |
| POST | `/checkout/tickets/{ticketId}/refund` | Authenticated | Richiedi rimborso biglietto |
| POST | `/checkout/tickets/self-validate` | Authenticated | Auto-validazione biglietto (QR scan) |
| GET | `/checkout/tickets/lookup/{code}` | Authenticated | Cerca biglietto per codice |

#### Gruppo: `/payments` — Webhook Stripe

| Method | Path | Auth | Descrizione |
|---|---|---|---|
| **POST** | **`/payments/stripe/webhook`** | **Public (firma Stripe)** | **Riceve eventi Stripe (`checkout.session.completed`, ecc.)** |

#### Gruppo: `/credito` e `/admin/credito` — Gestione Credito/Wallet

| Method | Path | Auth | Descrizione |
|---|---|---|---|
| GET | `/credito/me` | Authenticated | Saldo e storico movimenti |
| GET | `/admin/credito/users` | PowerUserOrAdmin | Cerca utenti per email |
| GET | `/admin/credito/ricariche` | PowerUserOrAdmin | Storico ricariche |
| POST | `/admin/credito/ricariche` | PowerUserOrAdmin | Ricarica credito a un utente |

#### Gruppo: `/admin/utenti` — Gestione Utenti (Admin)

| Method | Path | Auth | Descrizione |
|---|---|---|---|
| GET | `/admin/utenti` | AdminOnly | Lista utenti |
| GET | `/admin/utenti/paged` | AdminOnly | Utenti paginati con ricerca e filtro ruolo |
| PUT | `/admin/utenti/{id}/ruolo` | AdminOnly | Cambia ruolo utente |
| POST | `/admin/utenti/inviti` | AdminOnly | Invita nuovo admin/PowerUser (invia email con link) |
| POST | `/admin/utenti/{id}/password-setup` | AdminOnly | Richiedi setup password per un utente |
| GET | `/admin/utenti/{id}/security` | AdminOnly | Info sicurezza di un utente |

#### Gruppo: Validazione Biglietti (Admin)

| Method | Path | Auth | Descrizione |
|---|---|---|---|
| GET | `/admin/tickets/validate/{code}` | PowerUserOrAdmin | Cerca biglietto per codice |
| POST | `/admin/tickets/validate` | PowerUserOrAdmin | Valida biglietto (ingresso in sala) |

#### Gruppo: `/tmdb` e `/admin` TMDB

| Method | Path | Auth | Descrizione |
|---|---|---|---|
| GET | `/tmdb/search` | PowerUserOrAdmin | Cerca film su TMDB (autofill form) |
| POST | `/admin/import-tmdb` | AdminOnly | Importa film "now playing" da TMDB |
| POST | `/admin/tmdb-enrich-posters` | AdminOnly | Arricchisce poster dei film esistenti |

#### Gruppo: `/media`

| Method | Path | Auth | Descrizione |
|---|---|---|---|
| POST | `/media/covers` | PowerUserOrAdmin | Upload copertina personalizzata per film |

#### Gruppo: `/profilo`

| Method | Path | Auth | Descrizione |
|---|---|---|---|
| GET | `/profilo` | Authenticated | Dati profilo |
| PUT | `/profilo` | Authenticated | Aggiorna profilo |
| GET | `/profilo/cinema-preferito` | Authenticated | Cinema preferito |
| PUT | `/profilo/cinema-preferito/{cinemaId}` | Authenticated | Imposta cinema preferito |
| GET | `/profilo/film-preferito` | Authenticated | Film preferito |
| PUT | `/profilo/film-preferito/{filmId}` | Authenticated | Imposta film preferito |
| DELETE | `/profilo/me` | Authenticated | Elimina account (GDPR) |
| GET | `/profilo/me/export` | Authenticated | Esporta dati personali (GDPR) |

#### Gruppo: `/profilo/notifiche`

| Method | Path | Auth | Descrizione |
|---|---|---|---|
| GET | `/profilo/notifiche` | Authenticated | Notifiche e suggerimenti film da valutare |
| POST | `/profilo/notifiche/valuta` | Authenticated | Valuta film (1-5 stelle) |

#### Gruppo: `/config`

| Method | Path | Auth | Descrizione |
|---|---|---|---|
| GET | `/config/frontend` | Public | Configurazione frontend (es. Stripe publishable key) |

---

## 6. BACKEND — SERVICE LAYER

### 6.1 Architettura a Servizi

Il backend segue il pattern **Service Layer**: ogni area funzionale e incapsulata in un servizio dedicato con interfaccia esplicita. Questo permette:
- **Testabilita**: i servizi possono essere mockati negli unit test
- **Disaccoppiamento**: gli endpoint non contengono logica di business
- **Manutenibilita**: responsabilita chiare e singole

### 6.2 Elenco dei Servizi

| Servizio (Interfaccia + Implementazione) | Responsabilita |
|---|---|
| `AuthService` | Login, registrazione, refresh JWT, cambio password, verifica email |
| `ExternalAuthService` | Gestione flussi OAuth2/OpenID Connect (Google, Microsoft) |
| `GoogleExternalAuthProvider` | Implementazione specifica per Google OAuth |
| `MicrosoftExternalAuthProvider` | Implementazione specifica per Microsoft Entra ID |
| `AccountTokenService` | Generazione e validazione token monouso (reset pwd, inviti) |
| `AccountEmailService` | Invio email transazionali (benvenuto, reset, verifica) |
| `ProfiloService` | CRUD profilo, preferenze, eliminazione account, export GDPR |
| `FilmService` | CRUD film con ricerca e paginazione |
| `RegistaService` | CRUD registi |
| `CinemaService` | CRUD cinema |
| `CategoriaService` | CRUD categorie/generi |
| `SalaService` | CRUD sale e gestione piantine posti |
| `ShowService` | CRUD proiezioni con filtri |
| `ProgrammazioneService` | Query complesse per programmazione (film in sala, per cinema, per data) |
| `SeatHoldService` | Blocco/sblocco posti con TTL (10 minuti), gestione concorrenza |
| `CheckoutService` | Creazione ordine, calcolo prezzi, flusso pagamento, riconciliazione Stripe |
| `BigliettoService` | Emissione biglietti, lookup per codice, rimborsi |
| `PagamentoService` | Orchestrazione pagamento (credito + Stripe) |
| `StripeGateway` | Integrazione con Stripe API (sessioni checkout, webhook) |
| `CreditoService` | Gestione wallet credito (ricariche, addebiti, storico) |
| `ValidazioneBigliettoService` | Validazione biglietti all'ingresso |
| `EmailService` | Invio email via SMTP (MailKit) |
| `PdfService` | Generazione PDF biglietti con QR code e barcode |
| `MediaService` | Upload e gestione copertine film |
| `UserAdminService` | Gestione utenti admin (ruoli, inviti, sicurezza) |
| `TmdbService` | Client per TMDB API (ricerca film, dettagli, poster) |
| `TmdbImportService` | Import massivo film da TMDB con mapping generi/registi |
| `NotificheService` | Suggerimenti film da valutare basati su acquisti recenti |
| `UserSecurityAuditService` | Registrazione eventi di sicurezza |
| `RedirectUrlValidator` | Validazione URL di redirect (anti open-redirect) |
| `RefreshTokenCleanupService` | Pulizia periodica token scaduti (background service) |
| `ExpiredHoldCleanupService` | Rilascio automatico hold scaduti (background service) |
| `WeeklyShowPlanner` | Pianificazione automatica proiezioni settimanali |
| `TicketPriceNormalizer` | Calcolo e normalizzazione prezzi biglietti |

### 6.3 Background Services

Il backend include servizi in background per operazioni periodiche:
- **RefreshTokenCleanupService**: rimuove token di refresh scaduti o revocati dal database
- **ExpiredHoldCleanupService**: rilascia automaticamente i posti in hold quando il TTL (10 minuti) scade, rendendoli nuovamente disponibili per l'acquisto

---

## 7. AUTENTICAZIONE E AUTORIZZAZIONE

### 7.1 Schema JWT

Il sistema utilizza **JSON Web Token** (JWT) con Bearer authentication:

**Access Token** (durata: 15 minuti configurabili):
```json
{
  "sub": "123",           // userId
  "email": "utente@esempio.it",
  "role": "User",          // User, PowerUser, Admin
  "auth_version": 1,       // per invalidazione globale
  "iat": ...,              // issued at
  "exp": ...,              // expiration (15 min)
  "iss": "CineBaseAPI",
  "aud": "CineBaseWeb"
}
```

**Refresh Token** (durata: 7 giorni):
- Salvato nella tabella `RefreshTokens` con associato `DeviceId`
- Il `DeviceId` (UUID) identifica il dispositivo/browser; permette sessioni multiple
- All'uso, il refresh token viene ruotato (vecchio revocato, nuovo emesso)
- Al logout, tutti i refresh token del dispositivo vengono revocati

**Flusso di refresh:**
1. Access token scade (dopo 15 min)
2. Frontend chiama `POST /auth/refresh` con il refresh token
3. Backend valida refresh token → revoca il vecchio → emette nuovo access + refresh token
4. Se refresh token e scaduto/revocato → l'utente deve rifare login

### 7.2 Ruoli e Policy di Autorizzazione

| Ruolo | Policy .NET | Descrizione | Permessi tipici |
|---|---|---|---|
| **User** | `Authenticated` | Utente base registrato | Profilo, acquisto biglietti, visualizzazione cronologia |
| **PowerUser** | `PowerUserOrAdmin` | Operatore/manager | CRUD film, registi, categorie, sale, proiezioni, validazione biglietti, ricariche credito |
| **Admin** | `AdminOnly` | Amministratore sistema | Tutto + gestione utenti, ruoli, inviti, configurazione cinema, import TMDB |

Le policy sono definite in `Program.cs`:
```csharp
builder.Services.AddAuthorization(opts => {
    opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    opts.AddPolicy("PowerUserOrAdmin", p => p.RequireRole("PowerUser", "Admin"));
});
```

### 7.3 Sicurezza delle Password

- **BCrypt** (via libreria BCrypt.Net-Next) con cost factor predefinito
- Le password non vengono mai salvate in chiaro
- Anti-lockout: dopo N tentativi falliti, l'account viene temporaneamente bloccato

### 7.4 Social Login

**Provider supportati:**
- **Google** OAuth 2.0 / OpenID Connect
- **Microsoft** Entra ID (Azure AD) / OpenID Connect

**Meccanismo anti-CSRF:**
- Il parametro `state` di OAuth2 viene generato casualmente e salvato in `ExternalAuthStates`
- Alla callback, il `state` viene verificato per prevenire attacchi CSRF

**Exchange Code:**
- Dopo il login social, invece di restituire direttamente i JWT nell'URL (insicuro), il backend genera un **exchange code** monouso (5 minuti di validita)
- Il frontend lo scambia con `POST /auth/external/exchange` per ottenere i JWT

**Associazione account:**
- Se l'email del provider corrisponde a un utente esistente → collegamento automatico
- Se l'email non esiste → creazione nuovo utente con password vuota (potra impostarla dopo)

### 7.5 Audit di Sicurezza

Tutti gli eventi rilevanti di sicurezza vengono registrati nella tabella `UserSecurityAuditLog`:
- Login riusciti e falliti
- Cambi password
- Refresh token
- Blocchi account
- Collegamento account social

---

## 8. FRONTEND — ARCHITETTURA

### 8.1 Single Page Application Vanilla JS

Il frontend e una **SPA in Vanilla JavaScript puro** — non utilizza React, Vue, Angular o alcun framework. Questa scelta dimostra la padronanza dei fondamenti del web development.

**Vantaggi di questa scelta:**
- Nessuna dipendenza da bundler (Webpack, Vite) o transpiler
- Caricamento diretto dal filesystem o da server statico
- Bundle minimo, performance elevate
- Massimo controllo sul DOM e sullo stato

### 8.2 Struttura delle Directory

```
frontend/CineBase.Web/wwwroot/
├── index.html                          Landing page
├── login.html, registrazione.html      Autenticazione
├── programmazione.html                 Browser film in programmazione
├── scheda-film.html                    Dettaglio film
├── my-cinemas.html                     Elenco cinema
├── acquista.html                       Selezione posti
├── pagamento.html                      Pagamento (credito + carta)
├── esito-acquisto.html                 Conferma acquisto
├── profilo.html                        Profilo utente
├── dashboard.html                      Dashboard admin
├── films.html                          CRUD film admin
├── registi.html, cinemas.html          CRUD registi, cinema
├── categorie.html, shows.html          CRUD categorie, proiezioni
├── sale.html                           Gestione sale + piantine
├── utenti.html                         Gestione utenti admin
├── ricarica-credito.html               Admin credito
├── validazione-biglietti.html          Validazione biglietti
├── recupera-password.html              Reset password
├── reimposta-password.html             Reimposta password
├── verifica-email.html                 Verifica email
├── social-login-complete.html          Completamento social login
├── privacy.html, termini.html          Documenti legali
├── css/
│   └── styles.css                      Stili custom (brand, temi, animazioni)
├── js/
│   ├── api.js                          HTTP client (581 righe)
│   ├── auth.js                         Gestione autenticazione client (262 righe)
│   ├── route-guard.js                  Protezione pagine per ruolo (166 righe)
│   ├── navbar.js                       Navbar dinamica
│   ├── template-loader.js              Caricamento componenti HTML
│   ├── utils.js                        Utility (toast, formatters)
│   ├── theme.js                        Tema chiaro/scuro
│   ├── date-rail.js                    Selettore date interattivo
│   ├── form-handlers.js                Gestione form
│   ├── tailwind-config.js              Configurazione Tailwind
│   ├── admin-shell.js                  Layout admin condiviso
│   └── pages/                          Logica specifica per pagina (19 file)
└── components/                         Componenti HTML riutilizzabili
    ├── navbar-landing.html
    ├── footer-landing.html
    └── footer-admin.html
```

### 8.3 Moduli JavaScript Core

#### `js/api.js` — HTTP Client Centralizzato

Il cuore del frontend. Gestisce:
- **Tutte le chiamate API** come funzioni asincrone (es. `getFilms()`, `login()`, `createOrder()`)
- **Auto-refresh del token**: intercetta le risposte 401, tenta il refresh automatico e ritenta la chiamata originale
- **Gestione errori**: mostra toast di notifica, reindirizza al login se necessario
- **Headers**: aggiunge automaticamente `Authorization: Bearer <token>` e `X-Device-Id`

```javascript
// Esempio d'uso in una pagina
import { filmService } from './api.js';
const film = await filmService.getById(filmId);
```

#### `js/auth.js` — State Management Autenticazione

- Salva/carica i token da `localStorage`
- Espone `isAuthenticated()`, `getUser()`, `getRole()`, `isAdmin()`, `isPowerUserOrAbove()`
- Gestisce login, logout, refresh
- Emette eventi custom (`auth:changed`) per aggiornare navbar e UI

#### `js/route-guard.js` — Protezione Pagine

Eseguito **prima** della renderizzazione del DOM (script in `<head>` con `blocking="render"`):

```javascript
// route-guard.js
if (requiresAuth && !authService.isAuthenticated()) {
    window.location.href = `/login.html?redirect=${encodeURIComponent(location.pathname)}`;
}
if (requiresAdmin && !authService.isAdmin()) {
    window.location.href = '/dashboard.html'; // redirect a dashboard
}
```

#### `js/template-loader.js` — Componenti HTML

Carica navbar e footer dinamicamente da file HTML nella cartella `components/`, permettendo:
- Riutilizzo del codice (DRY)
- Navbar diversa per area pubblica vs admin
- Footer diverso per landing vs admin

### 8.4 Sistema di Tema

Il sistema di tema (`js/theme.js` + variabili CSS in `styles.css`) supporta:
- **Light mode** (default)
- **Dark mode** (attivabile dall'utente)
- Preferenza salvata in `localStorage`
- Rispetta la preferenza di sistema (`prefers-color-scheme`)

### 8.5 Styling

- **Tailwind CSS** caricato da CDN per stili utility-first
- **Stili custom** in `css/styles.css` per:
  - Colori del brand RedCurtain (rosso/bordeaux)
  - Animazioni (fade-in, slide, transizioni pagina)
  - Estensioni e varianti Tailwind personalizzate
- **Font Inter** da Google Fonts per tipografia pulita
- **Font Awesome 6.4.0** per icone

### 8.6 Pagine Principali

#### Landing Page (`index.html`)
- Hero section con call-to-action
- Film in evidenza (ora in sala)
- Collegamenti a programmazione e cinema

#### Programmazione (`programmazione.html`)
- Griglia di film attualmente in programmazione
- Filtri per genere, data, cinema
- Tabs: "Ora in sala" / "Prossimamente"
- Search bar con ricerca in tempo reale
- Date rail interattivo per selezionare il giorno

#### Scheda Film (`scheda-film.html`)
- Dettaglio completo: poster, trama, cast, durata, regista, generi
- Rating medio degli utenti (1-5 stelle)
- Elenco proiezioni disponibili raggruppate per cinema e data
- Pulsante "Acquista biglietti" che porta alla selezione posti

#### Acquista (`acquista.html`)
- Mappa interattiva dei posti a sedere (canvas/CSS grid)
- I posti sono colorati per stato: verde (libero), giallo (hold), rosso (venduto), blu (selezionato)
- Click per selezionare/deselezionare posti
- Riepilogo laterale con posti scelti e prezzo totale
- Pulsante "Procedi al pagamento"

#### Pagamento (`pagamento.html`)
- Riepilogo ordine (film, data, orario, cinema, sala, posti)
- Opzione pagamento con credito disponibile
- Eventuale differenza pagata con carta di credito via Stripe
- Redirect a Stripe Hosted Checkout o pagamento diretto con credito

#### Area Admin
- **Dashboard** (`dashboard.html`): riepilogo statistiche
- **Film** (`films.html`): tabella CRUD con ricerca TMDB e autofill
- **Sale** (`sale.html`): editor visuale della piantina posti (trascinamento, aggiunta/rimozione file e settori)
- **Shows** (`shows.html`): creazione proiezioni con selettori a cascata (cinema → sala → film → orario)
- **Utenti** (`utenti.html`): tabella utenti, cambio ruoli, inviti nuovi admin
- **Ricarica Credito** (`ricarica-credito.html`): cerca utente, scegli importo, ricarica

---

## 9. FLUSSI PRINCIPALI

### 9.1 Flusso Completo di Acquisto Biglietti

Questo e il flusso di business piu importante del sistema:

```
FASE 1: NAVIGAZIONE E SELEZIONE
─────────────────────────────────
Scheda film → Scegli data/orario → Vai a selezione posti

FASE 2: SELEZIONE POSTI (acquista.html)
─────────────────────────────────────────
1. GET /checkout/shows/{showId}/seat-map
   → Riceve array di posti con stato (libero/hold/sold)
   → Frontend disegna la mappa interattiva

2. Utente clicca sui posti desiderati (max 10)

3. POST /checkout/holds
   Body: { showId, posti: [{salaPostoId, settore, fila, numero}] }
   → Server: verifica che ogni posto sia libero
   → Crea ShowPostiStato con stato "Hold" e TTL 10 minuti
   → Restituisce { holdToken, expiry }

FASE 3: GESTIONE HOLD
───────────────────────
- Hold attivo per 10 minuti (configurabile)
- Timer countdown visibile all'utente
- POST /checkout/holds/{holdToken}/refresh → rinnova per altri 10 min
- DELETE /checkout/holds/{holdToken} → rilascia posti (utente annulla)
- Scadenza automatica → ExpiredHoldCleanupService rilascia i posti

FASE 4: CREAZIONE ORDINE
──────────────────────────
4. POST /checkout/orders
   Body: { holdToken }
   → Server: verifica hold ancora valido
   → Crea record Ordine (stato: Pending)
   → Crea record Biglietti (stato: Issued)
   → Associa ShowPostiStato all'ordine
   → Restituisce { orderId, totale, biglietti[] }

FASE 5: PAGAMENTO
───────────────────
5a. PAGAMENTO CON SOLO CREDITO:
    POST /checkout/orders/{orderId}/pay
    Body: { useCredit: true }
    → Se credito >= totale: addebita credito, ordine → Paid
    → Biglietti restano Issued

5b. PAGAMENTO MISTO (CREDITO + STRIPE):
    POST /checkout/orders/{orderId}/pay
    Body: { useCredit: true }
    → Addebita credito disponibile
    → La differenza va pagata con Stripe:
    
    POST /checkout/orders/{orderId}/stripe-checkout-session
    → Server crea sessione Stripe Checkout
    → Restituisce URL di redirect
    → Frontend reindirizza a Stripe Hosted Checkout
    
    L'utente paga su Stripe → Stripe redirect a esito-acquisto.html
    → Webhook Stripe: POST /payments/stripe/webhook
    → Verifica firma webhook → ordine → Paid
    
    POST /checkout/orders/{orderId}/reconcile-checkout-session
    → Frontend verifica stato finale ordine

FASE 6: CONFERMA E BIGLIETTI
───────────────────────────────
- esito-acquisto.html mostra riepilogo e link per scaricare PDF
- GET /checkout/orders/{orderId}/pdf → PDF con tutti i biglietti
- Email automatica con PDF allegato (via MailKit/SMTP)
- Ogni biglietto ha: codice alfanumerico, QR code, barcode

FASE 7: UTILIZZO BIGLIETTO
─────────────────────────────
- Utente mostra QR code all'ingresso
- Operatore scansiona con validazione-biglietti.html
- POST /admin/tickets/validate → biglietto → Validated
- Oppure self-validate: POST /checkout/tickets/self-validate
```

### 9.2 Flusso di Import TMDB

```
1. Admin accede a dashboard.html → Import TMDB
2. Frontend: GET /tmdb/search?query=... (ricerca live con autocomplete)
3. Admin seleziona un film dai risultati
4. Frontend compila automaticamente il form (titolo, trama, cast, poster, anno, generi)
5. Admin regola i dati e salva
6. POST /films → film creato con dati TMDB

OPPURE (import massivo):

1. POST /admin/import-tmdb
2. Backend chiama TMDB API: /movie/now_playing
3. Per ogni film:
   a. Controlla se gia presente (per tmdbId)
   b. Cerca/crea regista
   c. Mappa generi TMDB → categorie locali
   d. Crea record Film con tutti i dati
   e. Scarica poster se disponibile
```

### 9.3 Flusso di Validazione Biglietti

```
1. Operatore (PowerUser/Admin) accede a validazione-biglietti.html
2. Inserisce codice biglietto manualmente O scannerizza QR code
3. GET /admin/tickets/validate/{code}
   → Restituisce: dettaglio biglietto, stato attuale, dati proiezione
4. Operatore verifica i dati e clicca "Valida"
5. POST /admin/tickets/validate
   Body: { code }
   → Verifica che biglietto sia in stato "Issued" (non gia validato, non cancellato, non rimborsato)
   → Aggiorna stato a "Validated"
   → Registra data e operatore

OPPURE (self-validate):
1. Utente scannerizza il proprio QR code su validazione-biglietti.html
2. POST /checkout/tickets/self-validate
   → Stesso processo ma senza operatore
```

### 9.4 Sistema Credito/Wallet

```
Ricarica:
1. Admin: ricarica-credito.html → cerca utente per email
2. Admin inserisce importo e causale
3. POST /admin/credito/ricariche
   → Crea MovimentoCredito (tipo: TopUp, importo positivo)
   → Aggiorna saldo User.Credito

Acquisto:
1. Durante pagamento, credito viene usato prima della carta
2. POST /checkout/orders/{orderId}/pay
   → Crea MovimentoCredito (tipo: DebitOrder, importo negativo)
   → Aggiorna saldo

Rimborso:
1. Utente richiede rimborso biglietto
2. POST /checkout/tickets/{ticketId}/refund
   → Crea MovimentoCredito (tipo: Refund, importo positivo)
   → Aggiorna saldo e stato biglietto
```

---

## 10. INTEGRAZIONI ESTERNE

### 10.1 Stripe — Gateway Pagamenti

**Libreria:** Stripe.net 48.2.0 (server) + Stripe.js CDN (client)

**Modalita di pagamento:**
1. **Hosted Checkout** (principale): l'utente viene reindirizzato a una pagina di pagamento ospitata da Stripe
2. **Pagamento con credito**: saldo wallet interno, gestito completamente dal backend

**Flusso Hosted Checkout:**
```
Frontend → POST /checkout/orders/{id}/stripe-checkout-session
Backend → Stripe API: POST /v1/checkout/sessions
        → Salva StripeSessionId nell'ordine
Backend → Restituisce { redirectUrl: "https://checkout.stripe.com/..." }
Frontend → window.location = redirectUrl
Utente paga su Stripe
Stripe → Webhook POST /payments/stripe/webhook
        → Firma HMAC verificata con webhook secret
        → Evento "checkout.session.completed"
        → Ordine → Paid, Biglietti → Issued
        → Invia email con PDF
Stripe → Redirect a FRONTEND_BASE_URL/esito-acquisto.html?orderId=...
Frontend → POST /checkout/orders/{id}/reconcile-checkout-session
        → Verifica stato finale
```

**Sicurezza Stripe:**
- Webhook firmati con `STRIPE_WEBHOOK_SECRET` (verifica HMAC-SHA256)
- Idempotenza: ogni ordine ha un solo Stripe Session ID
- Riconciliazione: anche se il webhook fallisce, il frontend forza una riconciliazione

### 10.2 TMDB — The Movie Database

**Libreria:** chiamate HTTP dirette (HttpClient) con Bearer Token

**Endpoint TMDB utilizzati:**
- `GET /3/search/movie` — ricerca film per titolo
- `GET /3/movie/{id}` — dettaglio film
- `GET /3/movie/{id}/credits` — cast e regista
- `GET /3/movie/{id}/images` — poster e backdrop
- `GET /3/movie/{id}/videos` — trailer YouTube
- `GET /3/movie/now_playing` — film attualmente nelle sale
- `GET /3/genre/movie/list` — mappatura generi

**Configurazione:** `TMDB_BEARER_TOKEN` nel file `.env`

### 10.3 Google OAuth 2.0 / OpenID Connect

**Endpoint Google utilizzati:**
- `https://accounts.google.com/o/oauth2/v2/auth` — autorizzazione
- `https://oauth2.googleapis.com/token` — scambio code per token
- `https://openidconnect.googleapis.com/v1/userinfo` — info profilo utente

**Configurazione:** `GOOGLE_OAUTH_CLIENT_ID`, `GOOGLE_OAUTH_CLIENT_SECRET`

### 10.4 Microsoft Entra ID / OpenID Connect

**Endpoint Microsoft utilizzati:**
- `https://login.microsoftonline.com/common/oauth2/v2.0/authorize` — autorizzazione
- `https://login.microsoftonline.com/common/oauth2/v2.0/token` — scambio code per token
- `https://graph.microsoft.com/v1.0/me` — info profilo utente

**Configurazione:** `MICROSOFT_OAUTH_CLIENT_ID`, `MICROSOFT_OAUTH_CLIENT_SECRET`

### 10.5 SMTP — Invio Email

**Libreria:** MailKit 4.16.0

**Email inviate dal sistema:**
| Tipo Email | Trigger | Contenuto |
|---|---|---|
| Verifica email | Dopo registrazione | Link con token per verificare l'email |
| Reset password | Utente dimentica password | Link con token monouso per reimpostare |
| Invito admin | Admin invita un nuovo utente | Link per impostare la password iniziale |
| Biglietti PDF | Dopo acquisto completato | PDF con tutti i biglietti in allegato |
| Rimborso | Dopo rimborso approvato | Conferma rimborso |

**Configurazione:** `SMTP_HOST`, `SMTP_PORT`, `SMTP_USER`, `SMTP_PASSWORD`

---

## 11. SICUREZZA

### 11.1 Protezione delle Comunicazioni

- **CORS**: configurato per permettere solo origini autorizzate (`localhost:5001`, `127.0.0.1:5001`)
- **JWT**: tutti gli endpoint sensibili richiedono Bearer token valido
- **HTTPS**: in produzione, tutte le comunicazioni sono cifrate

### 11.2 Protezione dei Dati

- **Password**: hash BCrypt (mai in chiaro)
- **Token**: i refresh token nel database non sono i JWT stessi ma riferimenti; il JWT contiene solo informazioni non sensibili
- **Stripe**: le chiavi private non sono mai esposte al frontend (solo la publishable key via `/config/frontend`)

### 11.3 Rate Limiting

Endpoint `/auth/*` limitati a **10 richieste al minuto per IP**. Configurabile via `DISABLE_RATE_LIMITING`.

### 11.4 Security Headers

Il frontend server (`CineBase.Web`) imposta header di sicurezza:
```
Content-Security-Policy: default-src 'self'; script-src 'self' cdn.jsdelivr.net cdn.tailwindcss.com js.stripe.com ...
X-Frame-Options: DENY
X-Content-Type-Options: nosniff
Referrer-Policy: strict-origin-when-cross-origin
Permissions-Policy: camera=(), microphone=(), geolocation=()
```

### 11.5 Anti-Lockout e Audit

- Tentativi di login falliti tracciati (`TentativiLoginFalliti`)
- Dopo soglia raggiunta, account bloccato temporaneamente (`BloccoLoginFinoA`)
- Audit log (`UserSecurityAuditLog`) registra IP, timestamp e tipo evento
- `AuthVersion` nei JWT permette invalidazione globale (incrementata dopo cambio password o compromissione)

### 11.6 GDPR e Privacy

- Consensi espliciti: `ConsensoPrivacy` e `ConsensoTermini` salvati al momento della registrazione
- **Esportazione dati**: `GET /profilo/me/export` restituisce tutti i dati personali in formato JSON
- **Diritto all'oblio**: `DELETE /profilo/me` elimina l'account e tutti i dati associati
- Documenti legali: `privacy.html` e `termini.html`

---

## 12. TESTING

### 12.1 Framework

| Componente | Tecnologia |
|---|---|
| Test framework | xUnit 2.9.2 |
| Assertion | FluentAssertions 8.8.0 |
| Mocking | Moq 4.20.72 |
| Integration test host | WebApplicationFactory (ASP.NET Core) |
| Database test | EF Core InMemory + SQLite |
| Code coverage | Coverlet 6.0.4 |

### 12.2 Unit Test (3)

Testano servizi in isolamento con dipendenze mockate:

| File | Cosa testa |
|---|---|
| `FilmServiceTests.cs` | Logica CRUD film (creazione, validazione, ricerca) |
| `RegistaServiceTests.cs` | Logica CRUD registi |
| `CinemaServiceTests.cs` | Logica CRUD cinema |

### 12.3 Integration Test (13)

Testano l'API dal punto di vista HTTP, usando un database in memoria:

| File | Cosa testa |
|---|---|
| `ApiIntegrationTests.cs` | Test generici API (health check, configurazione) |
| `AuthIntegrationTests.cs` | Flusso registrazione → login → refresh → me → change password |
| `CategoriaIntegrationTests.cs` | CRUD categorie con autorizzazione |
| `CheckoutIntegrationTests.cs` | Flusso completo: hold posti → ordine → pagamento |
| `CheckoutHostedIntegrationTests.cs` | Integrazione Stripe checkout (mockata) |
| `PagamentoCreditoIntegrationTests.cs` | Pagamento con credito (ricarica → acquisto → saldo) |
| `ProgrammazioneIntegrationTests.cs` | Query programmazione (filtri, date) |
| `RbacIntegrationTests.cs` | Controllo accessi basato su ruoli (RBAC) |
| `SalaIntegrationTests.cs` | CRUD sale, gestione piantine posti |
| `ShowIntegrationTests.cs` | CRUD proiezioni |
| `TicketIntegrationTests.cs` | Emissione e lookup biglietti |
| `ValidazioneTicketIntegrationTests.cs` | Validazione biglietti all'ingresso |

### 12.4 CustomWebApplicationFactory

Classe helper che:
- Crea un host ASP.NET Core in memoria per i test
- Configura database in memoria (InMemory o SQLite) invece di MariaDB
- Applica le migrazioni automaticamente
- Esegue il seed dei dati di test
- Espone un `HttpClient` per chiamare gli endpoint

---

## 13. DEVOPS E DEPLOYMENT

### 13.1 Docker Compose

Il progetto include un `docker-compose.yml` che orchestra 3 container:

```yaml
services:
  db:
    image: mariadb:10.11
    environment:
      MARIADB_ROOT_PASSWORD: ${DB_PASSWORD}
      MARIADB_DATABASE: ${DB_NAME}
    ports:
      - "3306:3306"
    volumes:
      - mariadb_data:/var/lib/mysql

  backend:
    build: ./backend/FilmAPI
    environment:
      DB_HOST: db
      ...
    ports:
      - "5000:5000"
    depends_on:
      - db

  frontend:
    build: ./frontend/CineBase.Web
    ports:
      - "5001:5001"
    depends_on:
      - backend

volumes:
  mariadb_data:
```

### 13.2 Variabili d'Ambiente

Tutte le configurazioni sensibili o variabili per ambiente sono gestite tramite file `.env` (basato su `.env.example`, 72 variabili):

| Categoria | Variabili principali |
|---|---|
| Database | `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` |
| JWT | `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_ACCESS_TOKEN_EXPIRY_MINUTES`, `JWT_REFRESH_TOKEN_EXPIRY_DAYS` |
| Admin Seed | `ADMIN_SEED_EMAIL`, `ADMIN_SEED_PASSWORD` |
| Stripe | `STRIPE_SECRET_API_KEY`, `STRIPE_WEBHOOK_SECRET` |
| SMTP | `SMTP_HOST`, `SMTP_PORT`, `SMTP_USER`, `SMTP_PASSWORD`, `SMTP_FROM_EMAIL` |
| OAuth | `GOOGLE_OAUTH_CLIENT_ID`, `GOOGLE_OAUTH_CLIENT_SECRET`, `MICROSOFT_OAUTH_CLIENT_ID`, `MICROSOFT_OAUTH_CLIENT_SECRET` |
| TMDB | `TMDB_BEARER_TOKEN` |
| App | `DEFAULT_TICKET_PRICE`, `HOLD_TTL_MINUTES`, `MAX_SEATS_PER_ORDER`, `FRONTEND_BASE_URL` |

### 13.3 Avvio in Sviluppo

Script `start-dev.bat` per Windows:
1. Verifica presenza `.env`
2. Avvia Docker Compose build
3. Avvia i 3 container (db, backend, frontend)

### 13.4 Dockerfile Backend

Multi-stage build .NET 9:
- **Stage 1** (`sdk`): compila il progetto
- **Stage 2** (`runtime`): immagine minimale ASP.NET per produzione

### 13.5 Migrazioni Database

Entity Framework Core gestisce lo schema con **13 migrazioni** Code-First. All'avvio, `Program.cs` applica automaticamente `dbContext.Database.Migrate()`.

---

## 14. DOCUMENTAZIONE

Il progetto include una cartella `docs/` con 18 tutorial/guide in italiano:

| Documento | Contenuto |
|---|---|
| `FRONTEND_ARCHITECTURE.md` | Guida all'architettura frontend vanilla JS |
| `TUTORIAL_AUTENTICAZIONE_WEB.md` | JWT, login, registrazione, refresh |
| `TUTORIAL_SOCIAL_LOGIN_GOOGLE_MICROSOFT.md` | Flussi OAuth2 completi |
| `TUTORIAL_STRIPE_GATEWAY_PAGAMENTI.md` | Integrazione Stripe |
| `TUTORIAL_STRIPE_CLI.md` | Stripe CLI per test webhook in locale |
| `TUTORIAL_EMAIL_MAILKIT_BIGLIETTI_PDF_QRCODE.md` | Invio email, PDF, QR code |
| `TUTORIAL_ITERAZIONE_4_MULTISALA_TICKETING.md` | Sistema multisala e ticketing |
| `TUTORIAL_INDEX_PROGRAMMAZIONE_FRONTEND_CINEBASE.md` | Frontend programmazione |
| `GUIDE_TESTING_INTRO.md` | Introduzione al testing |
| `TUTORIAL_UNIT_TESTS.md` | Unit test con Moq e xUnit |
| `TUTORIAL_INTEGRATION_TESTS.md` | Integration test con WebApplicationFactory |
| Altri... | Strategie di integrazione e feature specifiche |

E presente anche `TMDB_GUIDA_TECNICA.md` nella root con documentazione sull'integrazione TMDB.

---

## RIEPILOGO FINALE

**CineBase (RedCurtain)** e un progetto didattico completo che copre tutti gli aspetti dello sviluppo software moderno:

| Aspetto | Realizzazione |
|---|---|
| **Backend** | .NET 9 Minimal API, ~100 endpoint REST, architettura a servizi con DI |
| **Frontend** | SPA Vanilla JavaScript, 20+ pagine, sistema di autenticazione e routing client-side |
| **Database** | MariaDB con 19 tabelle, relazioni complesse, EF Core Code-First con 13 migrazioni |
| **Autenticazione** | JWT access/refresh, 3 ruoli, social login Google/Microsoft, BCrypt, anti-lockout |
| **Pagamenti** | Stripe Hosted Checkout + wallet credito interno + webhook |
| **Ticketing** | Hold posti (10 min TTL), ordini, biglietti PDF con QR code, validazione |
| **Integrazioni** | TMDB API, Google OAuth, Microsoft Entra ID, SMTP/MailKit |
| **Sicurezza** | CSP, CORS, rate limiting, audit log, GDPR, anti-CSRF (OAuth state) |
| **Testing** | 3 unit test + 13 integration test con xUnit, Moq, FluentAssertions |
| **DevOps** | Docker Compose (3 container), .env configuration, multi-stage Dockerfile |
| **Documentazione** | 18 tutorial, guida tecnica TMDB, resoconto tecnico |
| **Standard** | Dependency Injection, Service Layer pattern, Code-First migrations, RESTful design |

Totale: circa **15.000+ righe di codice** tra backend C# e frontend JavaScript, **19 tabelle** database, **~100 endpoint** API.
