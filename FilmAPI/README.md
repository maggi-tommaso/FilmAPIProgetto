# FilmAPI + SalaLuce.Web

Backend API minima in ASP.NET Core (.NET 9) con frontend statico separato per Iteration 2.

## Iteration 2 - SalaLuce.Web frontend

`SalaLuce.Web` e una nuova app ASP.NET Core Empty (`net9.0`) che usa solo static files, Tailwind CDN e vanilla JS + Fetch API.

- Pagine implementate:
  - `wwwroot/index.html` (salaluce_home)
  - `wwwroot/films.html` (salaluce_film_vault)
  - `wwwroot/registi.html` (salaluce_directors_circle)
  - `wwwroot/proiezioni.html` (salaluce_projection_schedule)
  - `wwwroot/dashboard.html` (salaluce_director_s_booth)
  - `wwwroot/cinemas.html` (blend projection_schedule + director_s_booth)
- Componenti riusabili in `wwwroot/components/`:
  - `header-public.html`, `header-admin.html`
  - `footer-public.html`, `footer-admin.html`
- Layout dinamico obbligatorio via `wwwroot/js/modules/template-loader.js`
- Moduli JS richiesti presenti:
  - `wwwroot/js/modules/api.js`
  - `wwwroot/js/modules/utils.js`
  - `wwwroot/js/modules/template-loader.js`
  - `wwwroot/js/modules/form-handlers.js`
  - `wwwroot/js/modules/navbar.js`
- Script pagina presenti:
  - `wwwroot/js/pages/home.js`
  - `wwwroot/js/pages/films.js`
  - `wwwroot/js/pages/registi.js`
  - `wwwroot/js/pages/cinemas.js`
  - `wwwroot/js/pages/proiezioni.js`
  - `wwwroot/js/pages/dashboard.js`
- Funzionalita incluse:
  - CRUD via modali
  - ricerca e filtri
  - toast informativi
  - modal conferma delete
  - stati loading/empty/error
  - navbar/footer responsive
- Endpoint backend consumati:
  - `/films`, `/registi`, `/cinemas`, `/proiezioni`

### Avvio locale

#### Avvio “tutto insieme” (consigliato)

Da PowerShell nella root del progetto:

```bash
.\dev.ps1
```

Oppure doppio click su `dev.cmd`.

Questo avvia:
- MariaDB via Docker Compose
- Backend `FilmAPI` su `http://localhost:5072` (Swagger: `http://localhost:5072/swagger`)
- Frontend `SalaLuce.Web` su `http://localhost:5076`

Il backend applica automaticamente le migration e, se il DB è vuoto, lo popola con registi e film di esempio.

Per fermare solo il database:

```bash
docker compose down
```

#### Avvio manuale (2 terminali)

1. Avvia il backend FilmAPI (porta default `http://localhost:5072`):

```bash
dotnet run
```

2. In un secondo terminale, avvia il frontend:

```bash
dotnet run --project SalaLuce.Web
```

Il frontend usa di default `http://localhost:5072` come API base URL.
