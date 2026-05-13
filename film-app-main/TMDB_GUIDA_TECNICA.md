# TMDB Integration — Guida Tecnica Completa

> **Progetto:** FilmAPI (Moviola)  
> **Stack:** ASP.NET Core 9 Minimal API · EF Core · MySQL/MariaDB · Vanilla JS Frontend  
> **Data:** 13 Maggio 2026  
> **Scopo:** Documentare ogni aspetto dell'integrazione TMDB per permettere a uno sviluppatore di replicarla da zero.

---

## 1. Panoramica dell'integrazione

Il progetto integra **The Movie Database (TMDB) API v3** in tre modalità distinte:

| Modalità | Trigger | Autenticazione | Scopo |
|---|---|---|---|
| **Ricerca live** | Bottone "Compila da TMDB" nel form Aggiungi Film (admin panel) | Bearer token | Pre-compilare i campi del form con i dati TMDB |
| **Import massivo** | Bottone "Importa da TMDB" nella pagina Film (admin panel) | Bearer token | Scaricare gli ultimi 20 film "now playing", salvarli nel DB, generare proiezioni |
| **Seeder (console)** | Eseguibile standalone `FilmApiSeeder` | Env var `TMDB_BEARER_TOKEN` | Popolare il DB con 60+ film reali, registi, categorie |

### Flusso generale dati

```
TMDB API (api.themoviedb.org)
        │
        ▼
┌───────────────────┐    ┌─────────────────────┐    ┌──────────────────┐
│  TmdbService.cs   │    │ TmdbImportService.cs │    │  TmdbClient.cs   │
│  (ricerca live)   │    │ (import massivo)     │    │  (seeder console)│
└────────┬──────────┘    └──────────┬──────────┘    └────────┬─────────┘
         │                          │                        │
         ▼                          ▼                        ▼
┌─────────────────────────────────────────────────────────────────┐
│                        FilmDbContext                            │
│  Tabelle: Films, Registi, Categorie, FilmCategorie, Shows, ...  │
└─────────────────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────────┐
│  Frontend (Vanilla JS): api.js → films.html / programmazione    │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. Configurazione API Key

### 2.1 Web API (backend runtime)

**File:** `backend/FilmAPI/appsettings.json`
```json
"TMDB": {
    "ApiKey": "be2dfdc93e090bab65cf76c95ab9bda0",
    "BearerToken": "eyJhbGciOiJIUzI1NiJ9..."
}
```

- `ApiKey` — **non utilizzato** dal codice. Presente solo per riferimento.
- `BearerToken` — **unica chiave usata**. È un TMDB API v4 Bearer token (JWT).

**Lettura nel codice:** `builder.Configuration["TMDB:BearerToken"]` in `Program.cs` (righe 94 e 104).

### 2.2 Seeder (console app)

**File:** `backend/.env`
```
TMDB_BEARER_TOKEN=<tmdb_bearer_token>
```

Il seeder carica `.env` via `DotNetEnv` e legge `Environment.GetEnvironmentVariable("TMDB_BEARER_TOKEN")`.

**Template:** `backend/.env.example` (riga 74).

### 2.3 Nomi chiave usati

| Contesto | Nome configurazione | Tipo |
|---|---|---|
| Web API | `TMDB:BearerToken` | `appsettings.json` |
| Seeder | `TMDB_BEARER_TOKEN` | `.env` environment variable |

---

## 3. Registrazione servizi (Dependency Injection)

**File:** `backend/FilmAPI/Program.cs`

### 3.1 HttpClient — Ricerca live

```csharp
builder.Services.AddHttpClient<ITmdbService, TmdbService>(client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", builder.Configuration["TMDB:BearerToken"]);
    client.Timeout = TimeSpan.FromSeconds(15);
});
```

- Registrato come **typed client** (`AddHttpClient<TInterface, TImpl>`)
- `TmdbService` riceve `HttpClient` già configurato via constructor injection
- Timeout: **15 secondi**

### 3.2 HttpClient — Import massivo

```csharp
builder.Services.AddHttpClient("TmdbImport", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", builder.Configuration["TMDB:BearerToken"]);
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

- Registrato come **named client** (`"TmdbImport"`)
- `TmdbImportService` lo risolve via `IHttpClientFactory.CreateClient("TmdbImport")`
- Timeout: **30 secondi** (più lungo perché fa molte chiamate in sequenza)

### 3.3 Servizi custom

```csharp
builder.Services.AddScoped<ITmdbImportService, TmdbImportService>();
```

| Interfaccia | Implementazione | Lifetime |
|---|---|---|
| `ITmdbService` | `TmdbService` | Transient (via typed HttpClient) |
| `ITmdbImportService` | `TmdbImportService` | Scoped |

### 3.4 Endpoint mapping

```csharp
app.MapTmdbEndpoints();        // /tmdb/search
app.MapTmdbImportEndpoints();  // /admin/import-tmdb
```

---

## 4. File creati o modificati (elenco completo)

### 4.1 Backend — Servizi

| File | Ruolo |
|---|---|
| `Services/ITmdbService.cs` | Interfaccia `ITmdbService` + DTO `TmdbSearchResult` |
| `Services/TmdbService.cs` | Ricerca TMDB live per autofill frontend |
| `Services/ITmdbImportService.cs` | Interfaccia `ITmdbImportService` + DTO `TmdbImportResult` |
| `Services/TmdbImportService.cs` | Import massivo now-playing + generazione proiezioni |

### 4.2 Backend — Endpoint

| File | Metodo | Protezione |
|---|---|---|
| `Endpoints/TmdbEndpoints.cs` | `GET /tmdb/search?title=` | `PowerUserOrAdmin` |
| `Endpoints/TmdbImportEndpoints.cs` | `POST /admin/import-tmdb` | `AdminOnly` |

### 4.3 Backend — Modelli / Entity

| File | Campi TMDB |
|---|---|
| `Model/Film.cs` | `TmdbId`, `TitoloOriginale`, `LinguaOriginale`, `PosterUrl`, `BackdropUrl`, `TrailerUrl`, `VotoMedio`, `CastText`, `DataRilascio`, `CopertinaPath` |
| `Model/Regista.cs` | (nessun campo TMDB-specifico, usato per mappare il regista) |
| `Model/Categoria.cs` | (nessun campo TMDB-specifico, usato per mappare i generi) |
| `Model/FilmCategoria.cs` | Join table Film↔Categoria (composite key: `FilmId`, `CategoriaId`) |

### 4.4 Backend — DTO

| File | DTO |
|---|---|
| `DTO/FilmDTO.cs` | `FilmDTO`, `FilmCreateDTO`, `FilmUpdateDTO` — contengono tutti gli 8 campi TMDB |

### 4.5 Backend — Configurazione

| File | Modifica |
|---|---|
| `appsettings.json` | Sezione `"TMDB": { "ApiKey": "...", "BearerToken": "..." }` |
| `Program.cs` | Righe 88-110: registrazione HttpClient + servizi. Righe 220-221: endpoint mapping |

### 4.6 Backend — Migrazione

| File | Descrizione |
|---|---|
| `Migrations/20260511200843_AddTmdbFieldsToFilm.cs` | Aggiunge 7 colonne TMDB alla tabella `Films` (Up) / le rimuove (Down) |

### 4.7 Backend — Seeder (standalone)

| File | Ruolo |
|---|---|
| `scripts/FilmApiSeeder/TmdbClient.cs` | Client TMDB completo per il seeder (309 righe) |
| `scripts/FilmApiSeeder/SeedCatalog.cs` | 62 film target + mappatura generi + 20 cinema |
| `scripts/FilmApiSeeder/Program.cs` | Orchestrazione seeding (881 righe) |
| `scripts/FilmApiSeeder/FilmApiSeeder.csproj` | Riferimento al progetto FilmAPI (stessi modelli/DbContext) |

### 4.8 Frontend

| File | Modifica |
|---|---|
| `wwwroot/js/api.js` | Metodi `searchTmdb()` e `importTmdb()` |
| `wwwroot/js/pages/films.js` | Funzioni `setupTmdbAutofill()` e `fillFormFromTmdb()` (~120 righe) |
| `wwwroot/films.html` | Pulsante "Compila da TMDB" (autofill) + pulsante "Importa da TMDB" (import massivo) |

---

## 5. Endpoint TMDB utilizzati

### 5.1 Tabella riepilogativa

| # | Endpoint TMDB | Parametri | Usato da | Scopo |
|---|---|---|---|---|
| 1 | `GET /movie/now_playing` | `language=it-IT`, `region=IT`, `page=1` | `TmdbImportService` | Recuperare i film attualmente al cinema in Italia |
| 2 | `GET /search/movie` | `query=...`, `language=it-IT`, `page=1` | `TmdbService`, `TmdbClient` | Cercare film per titolo |
| 3 | `GET /movie/{id}` | `language=it-IT`, `append_to_response=videos,credits` | `TmdbService`, `TmdbImportService`, `TmdbClient` | Dettaglio completo film (con cast, crew, video) |
| 4 | `GET /discover/movie` | `with_genres=...`, `sort_by=popularity.desc`, `page=...` | `TmdbClient` (seeder) | Scoprire film per genere (fallback seeder) |
| 5 | `GET /person/{id}` | — | `TmdbClient` (seeder) | Dettaglio regista (per nazionalità) |
| 6 | `GET /configuration` | — | `TmdbClient` (seeder) | Base URL immagini e dimensioni disponibili |

### 5.2 Dettaglio chiamate principali

**Now Playing (import massivo):**
```
GET https://api.themoviedb.org/3/movie/now_playing?language=it-IT&page=1&region=IT
Authorization: Bearer {token}
```

**Dettaglio film con credits e video:**
```
GET https://api.themoviedb.org/3/movie/{tmdbId}?language=it-IT&append_to_response=videos,credits
Authorization: Bearer {token}
```

**Ricerca per titolo:**
```
GET https://api.themoviedb.org/3/search/movie?query={title}&language=it-IT&page=1
Authorization: Bearer {token}
```

---

## 6. Mapping dati nel database locale

### 6.1 Tabella `Films` — mapping completo

| Campo TMDB (JSON) | Campo DB (`Films`) | Tipo DB | Note |
|---|---|---|---|
| `title` | `Titolo` | `varchar(200)` | Titolo in italiano (grazie a `language=it-IT`) |
| `original_title` | `TitoloOriginale` | `varchar(200)` | Titolo in lingua originale |
| `overview` | `DescrizioneLunga` | `varchar(2000)` | Trama |
| `release_date` | `DataRilascio` | `date` (nullable) | Data uscita |
| `release_date` | `DataProduzione` | `datetime` (required) | Stesso valore, necessario per vincolo DB |
| `runtime` | `Durata` | `int` | In minuti. Default 90 se non disponibile |
| `poster_path` | `PosterUrl` | `varchar(500)` | URL completo: `https://image.tmdb.org/t/p/w500{path}` |
| `poster_path` | `CopertinaPath` | `varchar(500)` | **Stesso URL** — il frontend usa questo campo per le immagini |
| `backdrop_path` | `BackdropUrl` | `varchar(500)` | URL completo: `https://image.tmdb.org/t/p/w1280{path}` |
| `vote_average` | `VotoMedio` | `decimal(65,30)` | Media voti TMDB |
| `original_language` | `LinguaOriginale` | `varchar(10)` | Codice lingua (es. "en", "it") |
| `id` | `TmdbId` | `int` (nullable) | **Usato come chiave anti-duplicazione** |
| `videos.results` | `TrailerUrl` | `varchar(500)` | URL YouTube del primo trailer trovato |
| `credits.cast[0..9].name` | `CastText` | `varchar(2000)` | Top 10 attori, separati da virgola |
| `credits.crew[job=Director].name` | `RegistaId` (FK) | `int` | Mappato a tabella `Registi` (Nome + Cognome) |
| `genres[].name` | `FilmCategorie` (join) | — | Mappato a tabella `Categorie` via tabella join |

### 6.2 Mapping generi TMDB → Categorie locali

Il mapping è **bidirezionale** (inglese e italiano). Estratto da `TmdbImportService.cs`:

| Genere TMDB (EN) | Genere TMDB (IT) | Categoria DB |
|---|---|---|
| Action | Azione | Azione |
| Adventure | Avventura | Avventura |
| Animation | Animazione | Animazione |
| Comedy | Commedia | Commedia |
| Crime | Crimine | Thriller |
| Documentary | Documentario | Documentario |
| Drama | Dramma | Drammatico |
| Family | Famiglia | Animazione |
| Fantasy | Fantasy | Fantasy |
| History | Storia | Storico |
| Horror | Horror | Horror |
| Music | Musica | Documentario |
| Mystery | Mistero | Thriller |
| Romance | Romantico | Romantico |
| Science Fiction | Fantascienza | Fantascienza |
| Thriller | Thriller | Thriller |
| War | Guerra | Storico |
| Western | Western | Azione |
| TV Movie | — | Drammatico |

**Logica:** Se il genere TMDB non matcha il dizionario, viene creato così com'è nel DB (`GetOrCreateCategoriaAsync`).

### 6.3 Mapping regista

Il nome completo del regista da TMDB (es. `"Christopher Nolan"`) viene **splittato**:
- **Cognome:** ultima parola (es. `"Nolan"`)
- **Nome:** tutto il resto (es. `"Christopher"`)

La coppia `(Nome, Cognome)` viene cercata in `Registi`. Se non trovata, creata con nazionalità `"Da determinare"`.

---

## 7. Gestione immagini

### 7.1 URL base e dimensioni

| Tipo | URL Pattern | Dimensione |
|---|---|---|
| Poster | `https://image.tmdb.org/t/p/w500{poster_path}` | `w500` (larghezza 500px) |
| Backdrop | `https://image.tmdb.org/t/p/w1280{backdrop_path}` | `w1280` (larghezza 1280px) |

### 7.2 Dove vengono salvate

Entrambi i campi vengono popolati:
- `Film.PosterUrl` — URL TMDB
- `Film.CopertinaPath` — **stesso URL** (necessario perché il frontend mostra le immagini da `CopertinaPath`)
- `Film.BackdropUrl` — URL TMDB

**IMPORTANTE:** Il frontend usa `CopertinaPath` per visualizzare le immagini, NON `PosterUrl`. Popolare sempre entrambi.

### 7.3 Fallback

Se `poster_path` o `backdrop_path` sono `null`:
- Si lascia il campo DB a `null`
- Il frontend mostra `/assets/images/defaults/cover-default.jpg` grazie alla funzione `getCoverImage()`

### 7.4 Seeder — configurazione immagini

Il seeder (`TmdbClient.cs`) chiama `GET /configuration` per ottenere `secure_base_url` e `poster_sizes`. Preferisce `w780`, altrimenti `original`, altrimenti fallback statico.

---

## 8. Gestione errori

### 8.1 Timeout

| Servizio | Timeout | Comportamento |
|---|---|---|
| `TmdbService` (ricerca) | 15 secondi | `TaskCanceledException` → log warning → `return null` |
| `TmdbImportService` (import) | 30 secondi per richiesta | `TaskCanceledException` → log error → salta il film |
| `CancellationTokenSource` interno | 15 secondi per chiamata | Uguale al timeout HttpClient |

### 8.2 Chiave errata / non autorizzata

Se TMDB risponde `401`:
- `TmdbService`: log warning con status code → `return null`
- `TmdbImportService`: aggiunge errore al risultato → salta il film

### 8.3 API non raggiungibile

`HttpRequestException` catturata dal `try/catch` generico → log error → skip.

### 8.4 Rate limit

TMDB impone ~50 richieste/sec. Non c'è gestione esplicita del rate limit nel codice. Le chiamate sono sequenziali, quindi poco probabile colpire il limite.

### 8.5 Campi null / mancanti

Ogni campo TMDB opzionale viene letto con `TryGetProperty()` + controllo `ValueKind`. Se assente, il campo DB rimane `null` senza errori. La `Durata` ha un default di `90` minuti.

---

## 9. Anti-duplicazione

### 9.1 Duplicazione film

Il campo `Film.TmdbId` (nullable int) è la chiave di deduplicazione.

**Logica in `TmdbImportService.UpsertFilmAsync()`:**
```csharp
var existing = await _db.Films.FirstOrDefaultAsync(f => f.TmdbId == detail.TmdbId);
```

- Se il film esiste già (stesso `TmdbId`): **aggiorna solo i campi NULL** (non sovrascrive dati esistenti)
- Se non esiste: crea nuovo record

### 9.2 Duplicazione proiezioni (Show)

Indice univoco sul DB: `{CinemaId, SalaId, StartAtUtc}`

**Logica in `GenerateShowsAsync()`:**
- Costruisce chiave composita: `$"{cinema.Id}|{sala.Id}|{film.Id}|{utcTime:yyyyMMddHHmm}"`
- Tiene un `HashSet<string>` in memoria delle chiavi già esistenti (query iniziale) + create in questa esecuzione
- Salta se chiave già presente

### 9.3 Duplicazione categorie

- La tabella `Categorie` ha indice univoco su `Nome`
- `GetOrCreateCategoriaAsync()` cerca per nome prima di creare
- La join `FilmCategorie` ha composite key `{FilmId, CategoriaId}` — impedisce duplicati

### 9.4 Duplicazione registi

- `GetOrCreateRegistaAsync()` cerca per `Nome` + `Cognome` prima di creare
- Nessun indice univoco sul DB per nome+cognome (potenziale miglioramento)

---

## 10. Automazioni presenti

### 10.1 Import Now Playing + generazione proiezioni

**Trigger:** Click su "Importa da TMDB" nell'admin panel → `POST /admin/import-tmdb`

**Cosa fa:**
1. Chiama `GET /movie/now_playing` da TMDB
2. Per ognuno dei primi 20 film:
   - Chiama `GET /movie/{id}` con credits e videos
   - Crea/aggiorna `Film` nel DB
   - Associa `Regista` e `Categorie`
3. Per ogni film importato × ogni cinema × 15 giorni × 4 orari:
   - Genera record `Show` (15:00, 18:00, 21:00, 23:30 ora italiana)
   - Distribuisce in round-robin tra le sale attive del cinema
   - Converte in UTC per `StartAtUtc`

**Parametri personalizzabili:** `?movieCount=20&showDays=15`

### 10.2 Ricerca live / autofill

**Trigger:** Click su "Compila automaticamente da TMDB" nel form Aggiungi/Modifica Film

**Cosa fa:**
1. Prende il titolo dal campo form
2. Chiama `GET /tmdb/search?title=...`
3. Popola tutti i campi del form con i dati TMDB
4. Tenta di matchare regista e categorie con quelli esistenti nel DB

**Non salva nulla automaticamente** — l'utente deve cliccare "Salva".

### 10.3 Seeder (manuale)

**Trigger:** Esecuzione console app `FilmApiSeeder`

**Cosa fa:**
1. Carica 62 film target da `SeedCatalog.cs`
2. Per ciascuno cerca su TMDB e importa con dettagli completi
3. Crea 20 cinema italiani con sale e piantine posti
4. Genera proiezioni per 7 giorni

---

## 11. Problemi risolti (bug noti → fix)

### 11.1 Immagini non visibili dopo import

**Problema:** Dopo l'import massivo, le immagini dei film non apparivano nel frontend.

**Causa:** Il servizio `TmdbImportService` popolava `PosterUrl` ma **non** `CopertinaPath`. Il frontend usa esclusivamente `CopertinaPath` per mostrare le immagini (vedi `getCoverImage()` in `programmazione.js`, `films.js`, etc.).

**Fix:** Aggiunta assegnazione `CopertinaPath = detail.PosterUrl` sia in creazione che in aggiornamento film.

**File modificato:** `Services/TmdbImportService.cs`

### 11.2 Timezone proiezioni

**Problema:** Gli orari delle proiezioni (15:00, 18:00, etc.) devono essere in ora italiana, ma il DB salva in UTC.

**Soluzione:** Risoluzione timezone con fallback a catena:
```
Europe/Rome → W. Europe Standard Time → Central Europe Standard Time → UTC
```

---

## 12. Dipendenze necessarie

### 12.1 NuGet packages

**Nessun pacchetto TMDB-specifico.** L'integrazione usa solo:
- `Microsoft.Extensions.Http` (incluso in ASP.NET Core) — per `IHttpClientFactory` e `AddHttpClient`
- `System.Text.Json` (incluso in .NET) — per parsing JSON delle risposte TMDB
- `DotNetEnv` (3.1.1) — per caricare `.env` (solo seeder)

### 12.2 Pacchetti già nel progetto

```xml
<!-- Da FilmAPI.csproj (nessuna dipendenza TMDB aggiuntiva) -->
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.11" />
<PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="9.0.0" />
<PackageReference Include="DotNetEnv" Version="3.1.1" />
<!-- System.Text.Json è incluso in .NET 9 -->
```

### 12.3 Using principali

```csharp
using System.Text.Json;              // JsonDocument, JsonElement
using System.Net.Http.Headers;       // AuthenticationHeaderValue
using Microsoft.EntityFrameworkCore;  // EF Core
using FilmAPI.Data;                  // FilmDbContext
using FilmAPI.Model;                 // Film, Regista, Categoria, Show, etc.
```

---

## 13. Procedura passo-passo da zero

Per replicare l'integrazione TMDB in un nuovo progetto ASP.NET Core:

### Fase A — Configurazione

1. **Ottenere un Bearer Token TMDB**
   - Registrarsi su https://www.themoviedb.org/settings/api
   - Ottenere l'API Key (v3) e generare un Bearer Token (v4)

2. **Salvare la chiave in `appsettings.json`**
   ```json
   "TMDB": {
       "BearerToken": "eyJ..."
   }
   ```

3. **(Opzionale) Aggiungere a `.env` per il seeder**
   ```
   TMDB_BEARER_TOKEN=eyJ...
   ```

### Fase B — Modello dati

4. **Aggiungere campi TMDB all'entity Film**
   - `TmdbId` (int?, nullable)
   - `TitoloOriginale` (string, max 200)
   - `LinguaOriginale` (string, max 10)
   - `PosterUrl` (string, max 500)
   - `BackdropUrl` (string, max 500)
   - `TrailerUrl` (string, max 500)
   - `VotoMedio` (decimal?, nullable)
   - `CopertinaPath` (string, max 500) — **obbligatorio per il frontend**

5. **Creare migration EF Core**
   ```bash
   dotnet ef migrations add AddTmdbFieldsToFilm
   ```

6. **Aggiornare i DTO** (`FilmDTO`, `FilmCreateDTO`, `FilmUpdateDTO`) con gli stessi campi

### Fase C — Servizi

7. **Creare `ITmdbService.cs` e `TmdbSearchResult`**
   - Interfaccia con `SearchMovieAsync(string title)`
   - DTO con tutti i campi di risposta TMDB

8. **Creare `TmdbService.cs`**
   - Iniettare `HttpClient` (già configurato con Bearer token)
   - Implementare la ricerca: `/search/movie` → `/movie/{id}` con `append_to_response=videos,credits`
   - Parsing JSON con `System.Text.Json`
   - Gestire timeout, errori HTTP, campi mancanti

9. **Creare `ITmdbImportService.cs` e `TmdbImportResult`**
   - Interfaccia con `ImportNowPlayingAsync(int movieCount, int showDays)`
   - DTO con contatori e log

10. **Creare `TmdbImportService.cs`**
    - Iniettare `IHttpClientFactory`, `FilmDbContext`, `ILogger`, `IConfiguration`
    - Fetch now-playing → fetch dettagli → upsert film → genera proiezioni
    - Mapping generi con dizionario bilingue
    - Gestione registi (split nome/cognome)
    - Gestione timezone Italia → UTC
    - Anti-duplicazione (per TmdbId e per chiave Show)

### Fase D — Registrazione

11. **In `Program.cs`:**
    ```csharp
    // HttpClient per ricerca
    builder.Services.AddHttpClient<ITmdbService, TmdbService>(client => {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", builder.Configuration["TMDB:BearerToken"]);
        client.Timeout = TimeSpan.FromSeconds(15);
    });

    // HttpClient per import
    builder.Services.AddHttpClient("TmdbImport", client => {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", builder.Configuration["TMDB:BearerToken"]);
        client.Timeout = TimeSpan.FromSeconds(30);
    });

    builder.Services.AddScoped<ITmdbImportService, TmdbImportService>();
    ```

### Fase E — Endpoint

12. **Creare `TmdbEndpoints.cs`** — `GET /tmdb/search` (protetto `PowerUserOrAdmin`)
13. **Creare `TmdbImportEndpoints.cs`** — `POST /admin/import-tmdb` (protetto `AdminOnly`)
14. **Mappare in `Program.cs`:**
    ```csharp
    app.MapTmdbEndpoints();
    app.MapTmdbImportEndpoints();
    ```

### Fase F — Frontend

15. **In `api.js`:**
    ```javascript
    searchTmdb: (title) => apiFetch(`/tmdb/search?title=${encodeURIComponent(title)}`),
    importTmdb: (params = {}) => apiFetch(`/admin/import-tmdb?${new URLSearchParams(params)}`, { method: 'POST' })
    ```

16. **In `films.js`:**
    - Funzione `setupTmdbAutofill()`: bottone "Compila da TMDB" → popola form
    - Funzione `importFromTmdb()`: bottone "Importa da TMDB" → chiama API → mostra risultato

17. **In `films.html`:**
    - Bottone autofill nel form Aggiungi/Modifica Film
    - Bottone import nella toolbar della pagina

### Fase G — Verifica

18. **Testare la ricerca live:**
    ```bash
    curl -X GET "http://localhost:5000/tmdb/search?title=Inception" \
      -H "Authorization: Bearer {jwt_admin}"
    ```

19. **Testare l'import massivo:**
    ```bash
    curl -X POST "http://localhost:5000/admin/import-tmdb?movieCount=5&showDays=3" \
      -H "Authorization: Bearer {jwt_admin}"
    ```

20. **Verificare nel frontend:**
    - Login come admin → pagina Film → "Importa da TMDB"
    - Verificare immagini, generi, registi
    - Controllare proiezioni in `/shows.html`

---

## Appendice A — Struttura directory rilevante

```
backend/FilmAPI/
├── appsettings.json              ← TMDB:BearerToken
├── Program.cs                    ← DI + endpoint mapping
├── Services/
│   ├── ITmdbService.cs           ← Interfaccia ricerca + DTO
│   ├── TmdbService.cs            ← Ricerca live (199 righe)
│   ├── ITmdbImportService.cs     ← Interfaccia import + DTO
│   └── TmdbImportService.cs      ← Import massivo (566 righe)
├── Endpoints/
│   ├── TmdbEndpoints.cs          ← GET /tmdb/search
│   └── TmdbImportEndpoints.cs    ← POST /admin/import-tmdb
├── Model/
│   ├── Film.cs                   ← Campi TMDB
│   ├── Regista.cs                ← Nome, Cognome
│   └── Categoria.cs              ← Nome
├── DTO/
│   └── FilmDTO.cs                ← 3 DTO con campi TMDB
├── Migrations/
│   └── 20260511200843_AddTmdbFieldsToFilm.cs
└── Data/
    └── FilmDbContext.cs

backend/scripts/FilmApiSeeder/
├── TmdbClient.cs                 ← Client TMDB seeder (309 righe)
├── SeedCatalog.cs                ← 62 film target + mapping generi
├── Program.cs                    ← Orchestrazione seeder
└── FilmApiSeeder.csproj

frontend/Moviola.Web/wwwroot/
├── js/
│   ├── api.js                    ← searchTmdb(), importTmdb()
│   └── pages/
│       └── films.js              ← setupTmdbAutofill(), fillFormFromTmdb()
└── films.html                    ← Pulsanti TMDB
```

---

## Appendice B — Comandi rapidi

```bash
# Avviare backend
cd backend/FilmAPI && dotnet run

# Avviare frontend
cd frontend/Moviola.Web && dotnet run

# Eseguire seeder
cd backend/scripts/FilmApiSeeder && dotnet run

# Verificare connessione TMDB
curl -s "https://api.themoviedb.org/3/movie/now_playing?language=it-IT" \
  -H "Authorization: Bearer $(jq -r '.TMDB.BearerToken' backend/FilmAPI/appsettings.json)" \
  | jq '.results | length'
```
