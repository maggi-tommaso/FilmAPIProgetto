Build Iteration 2 frontend as a new static ASP.NET Core app named `SalaLuce.Web`, using only:
- ASP.NET Core Empty (.NET 9)
- static files
- Tailwind CDN
- vanilla JS + Fetch API
- reusable HTML components

It must consume existing backend FilmAPI routes (`/films`, `/registi`, `/cinemas`, `/proiezioni`) and follow the approved Sala Luce visual references (1960s Italian premium cinema style).

Also:
1) add this prompt into `dev_iteration/2/prompt.md`
2) update README Iteration 2 section accordingly.

Use mock references from `dev_iteration/2/stitch_cinebase/`:
- `salaluce_home`
- `salaluce_film_vault`
- `salaluce_directors_circle`
- `salaluce_projection_schedule`
- `salaluce_director_s_booth`

Page style mapping:
- `index.html` -> salaluce_home
- `films.html` -> salaluce_film_vault
- `registi.html` -> salaluce_directors_circle
- `proiezioni.html` -> salaluce_projection_schedule
- `dashboard.html` -> salaluce_director_s_booth
- `cinemas.html` -> blend projection_schedule + director_s_booth

Mandatory reusable components in `wwwroot/components/`:
- `header-public.html`, `header-admin.html`
- `footer-public.html`, `footer-admin.html`

Mandatory dynamic layout loading via `template-loader.js`.

Required JS modules:
- `api.js`, `utils.js`, `template-loader.js`, `form-handlers.js`, `navbar.js`
- page scripts: `home.js`, `films.js`, `registi.js`, `cinemas.js`, `proiezioni.js`, `dashboard.js`

Functional requirements:
- CRUD modals
- search/filtering
- toasts
- confirm delete modal
- loading/empty/error states
- responsive navbar/footer

Visual identity requirements:
- warm ivory/cream, burnt orange/brick red, navy/midnight, matte gold
- serif headline + geometric sans body
- premium retro atmosphere

Final app should run with `dotnet run`.
