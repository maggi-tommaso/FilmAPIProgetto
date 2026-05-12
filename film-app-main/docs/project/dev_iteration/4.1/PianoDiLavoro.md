# Piano di Lavoro - Iterazione 4.1

Autore: OpenCode

Documento operativo derivato da `docs/project/dev_iteration/4/PianoDiLavoro.md`, sezione `13) Piano Rimozione Definitiva Legacy Proiezione/Prenotazione (Iterazione Futura)`.

Branch target suggerito: `dev_iteration_4_1`

---

## Stato Avanzamento Fasi

| Fase | Stato | Data | Note |
| --- | --- | --- | --- |
| FASE 0 - Preflight e mappa dipendenze legacy | **Completata** | 2026-05-05 | Riferimenti legacy mappati in backend, frontend, seeder, test e snapshot |
| FASE 1 - Migrazione frontend residuo da `/proiezioni` al dominio nuovo | **Completata** | 2026-05-05 | `api.js` e `home.js` migrati; workspace admin rinominato a `shows.html`/`shows.js`; nessuna chiamata runtime legacy |
| FASE 2 - Aggiornamento `FilmApiSeeder` | **Completata** | 2026-05-05 | Rimossi accessi a `dbContext.Proiezioni` e `dbContext.Prenotazioni`; seeder build OK |
| FASE 3 - Rimozione endpoint e DI legacy | **Completata** | 2026-05-05 | Smontati endpoint `/proiezioni` e `/prenotazioni`; registrazioni DI legacy rimosse |
| FASE 4 - Rimozione service, DTO, model e navigation legacy | **Completata** | 2026-05-05 | Eliminato codice runtime legacy; DTO profilo/admin splittati in file non legacy |
| FASE 5 - Pulizia `FilmDbContext` | **Completata** | 2026-05-05 | Rimossi `DbSet`, configurazioni EF e relazioni legacy dal context |
| FASE 6 - Migration EF drop tabelle legacy | **Completata** | 2026-05-05 | Migration creata e applicata; `Prenotazioni` droppata prima di `Proiezioni`; snapshot pulito; DB verificato con `mysqlsh` |
| FASE 7 - Pulizia test legacy e copertura sostitutiva | **Completata** | 2026-05-05 | Rimossi test legacy/skipped; aggiunta copertura `POST /shows` anonimo non autorizzato |
| FASE 8 - Build e test automatici completi | **Completata** | 2026-05-05 | Build backend/frontend/seeder/test OK; suite `198 PASS`, `0 FAIL`, `0 SKIP` |
| FASE 9 - Smoke test runtime mirati | **Completata** | 2026-05-05 | Pagine `index`, `programmazione`, `shows`, `profilo`, `acquista`, `validazione-biglietti` 200; API `/shows` 200, `/proiezioni` 404, `/prenotazioni` 404; verifica manuale admin/user OK |
| FASE 10 - Ricerca finale riferimenti residui | **Completata** | 2026-05-05 | Nessun match legacy in runtime backend, frontend, seeder, test e snapshot; migration storiche escluse |
| FASE 11 - Aggiornamento documentazione | **Completata** | 2026-05-05 | `status.md`, `changelog.md` e questo piano aggiornati con chiusura debito tecnico |

Esito finale: Iterazione 4.1 completata. Il dominio legacy `Proiezione`/`Prenotazione` è rimosso da runtime backend, frontend, seeder, test, snapshot EF e database locale aggiornato. Il database `film-api-db` è stato verificato con `mysqlsh`: `SHOW TABLES LIKE 'Proiezioni'` e `SHOW TABLES LIKE 'Prenotazioni'` non restituiscono righe. Rimangono riferimenti solo nelle migration storiche e nella documentazione.

---

## 1) Obiettivo Iterazione

Chiudere definitivamente il debito tecnico del dominio legacy `Proiezione`/`Prenotazione` introdotto nelle iterazioni precedenti e mantenuto come compat layer durante l'evoluzione multisala/ticketing dell'Iterazione 4.

Al termine dell'Iterazione 4.1, CineBase deve essere consolidata esclusivamente sul dominio nuovo:

- `Show` per la programmazione operativa multisala;
- `Sala` e `SalaPosto` per struttura cinema e piantina;
- `Ordine` e `Biglietto` per acquisto, ticketing e profilo utente;
- `Checkout`, `Pagamento`, `Credito`, `ValidazioneBiglietto` per flussi transazionali e staff.

Il vecchio dominio `Proiezione`/`Prenotazione` non deve più esistere in runtime backend, frontend, seeder, test, context EF o database aggiornato.

## 1.1 Contesto storico

L'Iterazione 3 aveva introdotto:

- autenticazione JWT con refresh token;
- RBAC `Admin`, `PowerUser`, `User`, anonimo;
- area profilo con prenotazioni virtuali;
- dominio `Proiezione(CinemaId, FilmId, Data, Ora)`;
- dominio `Prenotazione` senza posti reali, senza ordine e senza pagamento.

L'Iterazione 4 ha trasformato CineBase in piattaforma multisala con acquisto reale:

- modello `Show` come sostituto operativo di `Proiezione`;
- sale, piantine, stati posto-show e hold concorrenti;
- checkout, ordini, pagamenti Stripe Checkout hosted, credito piattaforma e pagamento misto;
- biglietti digitali, PDF, email e validazione ingresso;
- frontend pubblico film-centric e frontend admin per sale/show/credito/validazione.

Durante l'Iterazione 4 il legacy non è stato eliminato subito per ridurre il rischio di regressione. La chiusura finale è stata rimandata perché a fine iterazione restavano riferimenti residui in frontend, seeder, test, DI e schema DB.

## 1.2 Stato reale di partenza

Backend:

- `POST /proiezioni`, `PUT /proiezioni/{id}`, `DELETE /proiezioni/{id}` risultano già rimossi.
- `MapPrenotazioniEndpoints()` risulta smontata da `Program.cs`; gli endpoint `/prenotazioni` non dovrebbero essere più esposti.
- `GET /proiezioni` e `GET /proiezioni/{id}` possono essere ancora attivi come bridge read-only verso `Shows`.
- `IProiezioneService`/`ProiezioneService` possono essere ancora registrati e usati dal bridge read-only.
- `IPrenotazioneService`/`PrenotazioneService` possono essere ancora registrati per compatibilità di compilazione.
- `FilmDbContext` può contenere ancora `DbSet<Proiezione>` e `DbSet<Prenotazione>`.
- Le tabelle `Proiezioni` e `Prenotazioni` sono ancora considerate presenti nello schema DB fino a migration dedicata.

Frontend:

- La UI di prenotazione legacy è già stata rimossa da `profilo.html`.
- I metodi write legacy sono già stati rimossi da `api.js`.
- Possono rimanere ancora `API.getProiezioni()` e `API.getProiezione()`.
- `home.js` può usare ancora `API.getProiezioni()` per calcolare i film in evidenza.
- Alcuni link, anchor o testi possono ancora usare `Prenotazioni` come nome storico dell'area profilo pur non chiamando più endpoint legacy.

Test:

- Alcuni test legacy possono essere marcati `Skip`.
- Possono esistere test e helper che referenziano `Proiezione`, `Prenotazione`, `/proiezioni` o `/prenotazioni`.
- L'obiettivo di questa iterazione è arrivare a zero skip dovuti al legacy.

Tooling:

- `backend/scripts/FilmApiSeeder` può usare ancora `dbContext.Proiezioni` e `dbContext.Prenotazioni` nei reset.
- Il seeder deve essere aggiornato prima di rimuovere i `DbSet` dal `FilmDbContext`, altrimenti non compila.

Migration:

- Le migration storiche possono contenere riferimenti legittimi a `Proiezione` e `Prenotazione`.
- La nuova migration deve pulire il `FilmDbContextModelSnapshot.cs` e droppare le tabelle legacy.
- Non si modificano migration storiche già applicate salvo richiesta esplicita di rebaseline.

## 1.3 Scope dell'iterazione

### In scope

- Rimozione definitiva degli endpoint runtime `/proiezioni` e `/prenotazioni`.
- Rimozione di service, interface, DTO, model e navigation property legacy.
- Migrazione del frontend residuo da `/proiezioni` a `/shows` o programmazione v2.
- Aggiornamento del seeder realistico `FilmApiSeeder`.
- Pulizia `FilmDbContext` e snapshot EF.
- Creazione migration dedicata per drop tabelle `Prenotazioni` e `Proiezioni`.
- Rimozione o riscrittura dei test legacy/skipped.
- Verifica build e test automatici.
- Smoke test mirati sui flussi toccati indirettamente.
- Aggiornamento documentazione di progetto.

### Out of scope

- Nuove funzionalità di programmazione, checkout, credito, ticketing o validazione.
- Rework UI non necessario al cleanup legacy.
- Rebaseline delle migration storiche.
- Migrazione semantica delle vecchie `Prenotazioni` in `Biglietti`, perché mancano posto reale, pagamento e ordine.
- Nuove pagine admin oltre al rename del workspace show esistente.

## 1.4 Architettura repository

```text
repo-root/
|- backend/FilmAPI/          (API .NET 9 Minimal API + MariaDB)
|- backend/scripts/FilmApiSeeder/ (console seeder TMDB/dev data)
|- frontend/CineBase.Web/    (MPA statico, vanilla JS + Tailwind)
|- tests/backend/            (xUnit + integration)
|- docs/
```

## 1.5 Nomenclatura canonica

| Concetto | Stato dopo Iterazione 4.1 |
| --- | --- |
| `Show` | Termine canonico per spettacolo/programmazione multisala |
| `Proiezione` | Termine legacy da rimuovere dal runtime |
| `Ordine` | Source of truth dell'acquisto |
| `Biglietto` | Source of truth del posto acquistato/validabile |
| `Prenotazione` | Termine legacy da rimuovere dal runtime |
| `shows.html` | Path admin canonico per il workspace show multisala |

Regola pratica: se un file runtime usa ancora `Proiezione` o `Prenotazione`, deve essere rimosso, rinominato o migrato, salvo migration storiche o documentazione.

## 1.6 Regole operative vincolanti

- Non iniziare eliminando i model: prima rimuovere o migrare i riferimenti runtime ancora attivi.
- Non lasciare test `[Fact(Skip)]` o `[Theory(Skip)]` come documentazione storica del legacy.
- Non modificare migration storiche già applicate.
- Non usare una ricerca globale su `Proiezione|Prenotazione` come criterio assoluto se include `Migrations/`: le migration storiche possono contenere riferimenti legittimi.
- La pagina admin show è stata rinominata a `shows.html` per azzerare anche i riferimenti nominali legacy nel frontend runtime.
- Se i dati storici in `Prenotazioni` hanno valore reale, esportarli prima della migration di drop.
- In ambiente sviluppo/demo, se i dati sono fittizi e non rappresentabili come biglietti pagati, possono essere droppati con la migration dedicata.
- La nuova migration deve essere ispezionata prima dell'applicazione al database.

---

## 2) Requisiti Funzionali e Tecnici

## 2.1 Backend runtime

Devono essere vere tutte queste condizioni:

1. Nessun endpoint `/proiezioni` è mappato.
2. Nessun endpoint `/prenotazioni` è mappato.
3. `Program.cs` non registra `IProiezioneService` o `IPrenotazioneService`.
4. `Program.cs` non chiama `MapProiezioniEndpoints()` o `MapPrenotazioniEndpoints()`.
5. Non esistono file endpoint legacy nel runtime backend.
6. Non esistono service/interface legacy nel runtime backend.
7. Non esistono model legacy nel runtime backend.
8. Non esistono DTO legacy, oppure i DTO ancora utili sono stati spostati in file coerenti non legacy.

## 2.2 Frontend runtime

Devono essere vere tutte queste condizioni:

1. `api.js` non espone `getProiezioni`, `getProiezione`, `getPrenotazioni` o metodi CUD legacy.
2. `home.js` non chiama più `/proiezioni`.
3. La home calcola i film in evidenza da `/shows`, da programmazione v2 o da un endpoint home dedicato se già esistente.
4. Le pagine admin usano solo API `Show` per la programmazione.
5. Link e anchor verso `profilo.html#prenotazioni` sono sostituiti se la sezione non esiste più.
6. La UI non introduce nuove pagine o nuove chiamate legacy.

## 2.3 Seeder

`FilmApiSeeder` deve:

- compilare senza `DbSet<Proiezione>` o `DbSet<Prenotazione>`;
- resettare il dominio nuovo nell'ordine corretto;
- non emettere log o messaggi operativi che parlano di reset `Proiezioni`/`Prenotazioni` se non in documentazione storica.

## 2.4 Database e migration

La migration dedicata deve:

- chiamarsi `DropLegacyProiezionePrenotazioneTables`, salvo ragione tecnica documentata;
- droppare prima `Prenotazioni`, poi `Proiezioni`;
- non droppare tabelle del dominio nuovo;
- aggiornare `FilmDbContextModelSnapshot.cs` rimuovendo le entità legacy;
- permettere `dotnet ef database update` in ambiente target;
- lasciare il database senza tabelle `Prenotazioni` e `Proiezioni`.

## 2.5 Test

La suite deve:

- non contenere test skipped per legacy;
- non referenziare model, DTO, helper o endpoint legacy;
- mantenere o aggiungere copertura sostitutiva per `/shows`, RBAC show e profilo v2;
- passare con `0 FAIL`.

---

## 3) Fasi di Implementazione

### FASE 0 - Preflight e mappa dipendenze legacy

**Obiettivo**: produrre una mappa aggiornata dei riferimenti prima di modificare file.

**Attività**:

1. Eseguire ricerca completa su backend, frontend e test:

```bash
rg -n "Proiezione|Prenotazione|Proiezioni|Prenotazioni|/proiezioni|/prenotazioni|getProiezioni|getProiezione|getPrenotazioni|MapProiezioni|MapPrenotazioni|IProiezioneService|IPrenotazioneService" backend frontend tests --glob "!**/bin/**" --glob "!**/obj/**"
```

2. Eseguire ricerca backend runtime escludendo migration storiche:

```bash
rg -n "Proiezione|Prenotazione" backend/FilmAPI --glob "!Migrations/**" --glob "!bin/**" --glob "!obj/**"
```

3. Eseguire ricerca frontend runtime:

```bash
rg -n "/proiezioni|/prenotazioni|getProiezioni|getProiezione|getPrenotazioni|Prenotazioni|proiezioni" frontend/CineBase.Web/wwwroot
```

4. Eseguire ricerca test:

```bash
rg -n "Proiezione|Prenotazione|/proiezioni|/prenotazioni|CreateProiezioneAsync|Fact\(Skip|Theory\(Skip" tests/backend
```

5. Annotare nel piano o nelle note di lavoro i file da toccare.

**Verifica fase**:

- mappa dipendenze aggiornata disponibile;
- migration storiche identificate come non bloccanti;
- ordine di intervento confermato.

**Checklist fase**:

- [x] Ricerca globale eseguita
- [x] Ricerca backend runtime eseguita
- [x] Ricerca frontend runtime eseguita
- [x] Ricerca test eseguita
- [x] File impattati annotati

---

### FASE 1 - Migrazione frontend residuo da `/proiezioni` al dominio nuovo

**Obiettivo**: rimuovere tutte le dipendenze frontend dal bridge backend `/proiezioni` prima di smontarlo.

**Attività**:

1. In `frontend/CineBase.Web/wwwroot/js/api.js`:
   - rimuovere `getProiezioni`;
   - rimuovere `getProiezione`;
   - verificare che le pagine admin usino `getShows`, `getShow`, `createShow`, `updateShow`, `deleteShow`.
2. In `frontend/CineBase.Web/wwwroot/js/pages/home.js`:
   - sostituire `API.getProiezioni()` con `API.getShows({ page: 1, pageSize: 100 })` se il DTO show è sufficiente;
   - in alternativa usare `API.getProgrammazioneFilms({ tab: "featured", page: 1, pageSize: 100 })` se il DTO contiene già score, disponibilità o conteggi utili;
   - rinominare variabili locali da `proiezioni` a `shows`;
   - calcolare date da `show.startAtUtc` o campo reale del `ShowDTO`;
   - cambiare label utente da `proiezioni` a `show`, `spettacoli` o `orari`.
3. Verificare link e copy in:
   - `frontend/CineBase.Web/wwwroot/components/navbar-landing.html`;
   - `frontend/CineBase.Web/wwwroot/components/footer-landing.html`;
   - `frontend/CineBase.Web/wwwroot/js/admin-shell.js`;
   - eventuali pagine che mostrano `Prenotazioni` come voce profilo.
4. Sostituire anchor obsolete come `profilo.html#prenotazioni` con anchor reali o `profilo.html`.

**Verifica fase**:

```bash
rg -n "Prenotazioni|prenotazioni|Proiezioni|proiezioni|getProiezioni|getProiezione|/proiezioni|/prenotazioni" frontend/CineBase.Web/wwwroot
```

Sono accettabili solo:

- nessun nome file legacy nel frontend runtime;
- eventuali label storiche scelte esplicitamente, anche se è preferibile usare `Show` o `Programmazione`.

**Checklist fase**:

- [x] `api.js` senza metodi legacy
- [x] `home.js` migrato a `Show`/programmazione v2
- [x] Link profilo/navbar/footer verificati
- [x] Nessuna chiamata frontend a `/proiezioni` o `/prenotazioni`
- [x] Home anonima e autenticata senza chiamate 404 legacy

---

### FASE 2 - Aggiornamento `FilmApiSeeder`

**Obiettivo**: rendere il seeder indipendente dai `DbSet` legacy prima della pulizia del context.

**File principale**: `backend/scripts/FilmApiSeeder/Program.cs`

**Attività**:

1. Rimuovere ogni accesso a `dbContext.Proiezioni`.
2. Rimuovere ogni accesso a `dbContext.Prenotazioni`.
3. Verificare l'ordine di reset del dominio nuovo:
   - `MovimentiCredito`;
   - `Biglietti`;
   - `ShowPostiStato`;
   - `Ordini`;
   - `Shows`;
   - `SalaPosti`;
   - `Sale`;
   - dati catalogo/cinema/film se `ResetAllAsync`.
4. Rinominare log o messaggi operativi ancora legati a `proiezioni`/`prenotazioni`.

**Verifica fase**:

```bash
dotnet build backend/scripts/FilmApiSeeder/FilmApiSeeder.csproj
```

```bash
rg -n "Proiezione|Prenotazione|Proiezioni|Prenotazioni" backend/scripts/FilmApiSeeder --glob "!bin/**" --glob "!obj/**"
```

**Checklist fase**:

- [x] Nessun accesso del seeder a `Proiezioni`
- [x] Nessun accesso del seeder a `Prenotazioni`
- [x] Ordine reset nuovo dominio verificato
- [x] Seeder build verde

---

### FASE 3 - Rimozione endpoint e DI legacy

**Obiettivo**: smontare definitivamente gli endpoint runtime legacy e le relative registrazioni DI.

**File principale**: `backend/FilmAPI/Program.cs`

**Attività**:

1. Rimuovere `builder.Services.AddScoped<IProiezioneService, ProiezioneService>();`.
2. Rimuovere `builder.Services.AddScoped<IPrenotazioneService, PrenotazioneService>();`.
3. Rimuovere `app.MapProiezioniEndpoints();`.
4. Rimuovere anche il commento o mapping residuo `app.MapPrenotazioniEndpoints()`.
5. Verificare che restino mappati gli endpoint nuovi:
   - `app.MapShowsEndpoints()`;
   - `app.MapCheckoutEndpoints()`;
   - `app.MapProgrammazioneEndpoints()`;
   - `app.MapPagamentoEndpoints()`;
   - `app.MapValidazioneBigliettiEndpoints()`.
6. Eliminare:
   - `backend/FilmAPI/Endpoints/ProiezioniEndpoints.cs`;
   - `backend/FilmAPI/Endpoints/PrenotazioniEndpoints.cs`.

**Verifica fase**:

```bash
rg -n "MapProiezioni|MapPrenotazioni|IProiezioneService|IPrenotazioneService|ProiezioneService|PrenotazioneService" backend/FilmAPI --glob "!Migrations/**" --glob "!bin/**" --glob "!obj/**"
```

**Checklist fase**:

- [x] Registrazione `IProiezioneService` rimossa
- [x] Registrazione `IPrenotazioneService` rimossa
- [x] Mapping `/proiezioni` rimosso
- [x] Mapping/commento `/prenotazioni` rimosso
- [x] File endpoint legacy eliminati

---

### FASE 4 - Rimozione service, DTO, model e navigation legacy

**Obiettivo**: eliminare il codice runtime del dominio legacy.

#### 4.1 Service e interface

Eliminare:

- `backend/FilmAPI/Services/IProiezioneService.cs`
- `backend/FilmAPI/Services/ProiezioneService.cs`
- `backend/FilmAPI/Services/IPrenotazioneService.cs`
- `backend/FilmAPI/Services/PrenotazioneService.cs`

#### 4.2 DTO

Eliminare:

- `backend/FilmAPI/DTO/ProiezioneDTO.cs`

Gestire con cautela `backend/FilmAPI/DTO/ProfiloPrenotazioniAdminDTO.cs`:

- non eliminarlo meccanicamente senza controllare il contenuto;
- se contiene DTO ancora usati da profilo o admin utenti, dividerlo prima;
- spostare `ProfiloUpdateDTO` in `ProfiloDTO.cs` o file coerente;
- spostare `UserAdminDTO`/`UpdateRuoloDTO` in DTO admin utenti dedicato se ancora usati;
- eliminare solo `PrenotazioneCreateDTO` e `PrenotazioneDTO`;
- eliminare `ProfiloPrenotazioniAdminDTO.cs` solo se non contiene più nulla di utile.

Verifica mirata:

```bash
rg -n "ProfiloUpdateDTO|UserAdminDTO|UpdateRuoloDTO|PrenotazioneCreateDTO|PrenotazioneDTO" backend/FilmAPI tests/backend --glob "!bin/**" --glob "!obj/**"
```

#### 4.3 Model e navigation property

Eliminare:

- `backend/FilmAPI/Model/Proiezione.cs`
- `backend/FilmAPI/Model/Prenotazione.cs`

Pulire:

- `backend/FilmAPI/Model/Cinema.cs`: rimuovere `ICollection<Proiezione> Proiezioni`;
- `backend/FilmAPI/Model/Film.cs`: rimuovere `ICollection<Proiezione> Proiezioni`;
- `backend/FilmAPI/Model/User.cs`: rimuovere `ICollection<Prenotazione> Prenotazioni`.

**Verifica fase**:

```bash
rg -n "Proiezione|Prenotazione|Proiezioni|Prenotazioni" backend/FilmAPI/Model backend/FilmAPI/DTO backend/FilmAPI/Services backend/FilmAPI/Endpoints --glob "!bin/**" --glob "!obj/**"
```

**Checklist fase**:

- [x] Service/interface legacy eliminati
- [x] `ProiezioneDTO.cs` eliminato
- [x] DTO profilo/admin splittati se necessario
- [x] Model legacy eliminati
- [x] Navigation property legacy rimosse
- [x] Nessun riferimento legacy in Model/DTO/Services/Endpoints

---

### FASE 5 - Pulizia `FilmDbContext`

**Obiettivo**: rimuovere dal context EF ogni conoscenza del dominio legacy.

**File principale**: `backend/FilmAPI/Data/FilmDbContext.cs`

**Attività**:

1. Rimuovere `public DbSet<Proiezione> Proiezioni { get; set; }`.
2. Rimuovere `public DbSet<Prenotazione> Prenotazioni { get; set; }`.
3. Rimuovere blocchi `modelBuilder.Entity<Proiezione>`.
4. Rimuovere blocchi `modelBuilder.Entity<Prenotazione>`.
5. Verificare che le configurazioni del nuovo dominio restino intatte:
   - `Film`;
   - `Cinema`;
   - `User`;
   - `Sala`/`SalaPosto`;
   - `Show`/`ShowPostoStato`;
   - `Ordine`;
   - `Biglietto`;
   - `MovimentoCredito`.

**Verifica fase**:

```bash
rg -n "Proiezione|Prenotazione|Proiezioni|Prenotazioni" backend/FilmAPI/Data/FilmDbContext.cs
```

**Checklist fase**:

- [x] `DbSet<Proiezione>` rimosso
- [x] `DbSet<Prenotazione>` rimosso
- [x] Configurazioni EF legacy rimosse
- [x] Configurazioni nuovo dominio preservate
- [x] `FilmDbContext.cs` senza match legacy

---

### FASE 6 - Migration EF drop tabelle legacy

**Obiettivo**: rimuovere dallo schema DB le tabelle legacy con una migration dedicata e controllata.

**Nome migration consigliato**:

```bash
dotnet ef migrations add DropLegacyProiezionePrenotazioneTables --project backend/FilmAPI/FilmAPI.csproj --startup-project backend/FilmAPI/FilmAPI.csproj
```

**Controlli obbligatori sulla migration generata**:

1. `Up()` deve droppare prima `Prenotazioni`, poi `Proiezioni`.
2. `Up()` non deve droppare tabelle del nuovo dominio:
   - `Shows`;
   - `Ordini`;
   - `Biglietti`;
   - `ShowPostiStato`;
   - `Sale`;
   - `SalaPosti`;
   - `MovimentiCredito`.
3. `Down()` può ricreare `Proiezioni` e `Prenotazioni` come rollback strutturale, ma non recupererà i dati storici.
4. `FilmDbContextModelSnapshot.cs` non deve più contenere entità `FilmAPI.Model.Proiezione` o `FilmAPI.Model.Prenotazione`.

**Verifica snapshot**:

```bash
rg -n "FilmAPI.Model.Proiezione|FilmAPI.Model.Prenotazione|ToTable\(\"Proiezioni\"|ToTable\(\"Prenotazioni\"" backend/FilmAPI/Migrations/FilmDbContextModelSnapshot.cs
```

**Script SQL facoltativo da ispezionare**:

```bash
dotnet ef migrations script --project backend/FilmAPI/FilmAPI.csproj --startup-project backend/FilmAPI/FilmAPI.csproj
```

**Controllo DB prima della migration, se si lavora su MariaDB/MySQL**:

```sql
SELECT TABLE_NAME, CONSTRAINT_NAME, REFERENCED_TABLE_NAME
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE()
  AND REFERENCED_TABLE_NAME IN ('Proiezioni', 'Prenotazioni');
```

Output atteso:

- solo la FK da `Prenotazioni` a `Proiezioni`, oppure nessuna FK se il DB è già parzialmente pulito;
- nessuna FK dal nuovo dominio verso `Proiezioni` o `Prenotazioni`.

**Applicazione migration**:

```bash
dotnet ef database update --project backend/FilmAPI/FilmAPI.csproj --startup-project backend/FilmAPI/FilmAPI.csproj
```

**Verifica DB dopo migration**:

```sql
SHOW TABLES LIKE 'Proiezioni';
SHOW TABLES LIKE 'Prenotazioni';
```

**Checklist fase**:

- [x] Migration creata
- [x] `Up()` ispezionato
- [x] Drop `Prenotazioni` prima di `Proiezioni` verificato
- [x] Nessun drop del nuovo dominio
- [x] Snapshot senza entità legacy
- [x] Migration applicata in ambiente target
- [x] Tabelle legacy assenti dal DB aggiornato

Verifica DB finale eseguita con:

```bash
mysqlsh --sql --uri "root:root@localhost:3306/film-api-db" -e "SELECT DATABASE() AS db_name; SHOW TABLES LIKE 'Proiezioni'; SHOW TABLES LIKE 'Prenotazioni';"
```

Output rilevante:

- `db_name = film-api-db`
- nessuna riga per `Proiezioni`
- nessuna riga per `Prenotazioni`

---

### FASE 7 - Pulizia test legacy e copertura sostitutiva

**Obiettivo**: eliminare test legacy/skipped e preservare copertura dei comportamenti ancora rilevanti sul dominio nuovo.

**Eliminare o riscrivere**:

- `tests/backend/Unit/ProiezioneServiceTests.cs`
- `tests/backend/Integration/ProiezioneCompatIntegrationTests.cs`
- `tests/backend/Integration/PrenotazioneIntegrationTests.cs`

**Pulire**:

- `tests/backend/Integration/ApiIntegrationTests.cs`:
  - rimuovere test `P1`-`P10` se ancora presenti;
  - rimuovere test `E2`, `E3` se dipendono da `/proiezioni`;
  - rimuovere helper `CreateProiezioneAsync`;
  - rimuovere using DTO/model non più esistenti.
- `tests/backend/Integration/RbacIntegrationTests.cs`:
  - rimuovere `/proiezioni` e `/prenotazioni` dalla matrice RBAC legacy;
  - confermare copertura su `/shows`, `/checkout`, `/admin/tickets/validate`, `/admin/credito`.
- `tests/backend/Integration/CustomWebApplicationFactory.cs`:
  - verificare che non semini o manipoli `Proiezioni`/`Prenotazioni`.

**Copertura sostitutiva minima da confermare o aggiungere**:

- `GET /shows` pubblico.
- `GET /shows/{id}` pubblico.
- `POST /shows` consentito a `PowerUser`/`Admin`.
- `POST /shows` vietato a `User` e anonimo.
- `PUT /shows/{id}` consentito a `PowerUser`/`Admin`.
- `DELETE /shows/{id}` consentito a `PowerUser`/`Admin` se non ci sono dipendenze bloccanti.
- Anti-overlap show nella stessa sala.
- Show contemporanei permessi in sale diverse.
- Profilo utente indipendente da prenotazioni legacy e ancora capace di esporre ordini/biglietti/credito.

**Verifica fase**:

```bash
rg -n "Fact\(Skip|Theory\(Skip|Proiezione|Prenotazione|/proiezioni|/prenotazioni|CreateProiezioneAsync" tests/backend --glob "!bin/**" --glob "!obj/**"
```

**Checklist fase**:

- [x] Test legacy rimossi o riscritti
- [x] Helper legacy rimossi
- [x] RBAC legacy sostituito da RBAC nuovo dominio
- [x] Nessun riferimento legacy nei test
- [x] Nessuno skip legacy residuo

---

### FASE 8 - Build e test automatici completi

**Obiettivo**: dimostrare che la rimozione legacy non rompe backend, frontend, seeder o suite test.

Eseguire in quest'ordine:

```bash
dotnet build backend/FilmAPI/FilmAPI.csproj
```

Pass condition:

- 0 errori;
- nessun errore da model/DTO/service legacy mancanti.

```bash
dotnet build backend/scripts/FilmApiSeeder/FilmApiSeeder.csproj
```

Pass condition:

- 0 errori;
- nessun riferimento a `DbSet<Proiezione>` o `DbSet<Prenotazione>`.

```bash
dotnet build frontend/CineBase.Web/CineBase.Web.csproj
```

Pass condition:

- 0 errori;
- nessun asset JS mancante.

```bash
dotnet build tests/backend/FilmAPI.Tests.csproj
```

Pass condition:

- 0 errori;
- nessun DTO/model legacy referenziato.

```bash
dotnet test tests/backend/FilmAPI.Tests.csproj
```

Pass condition:

- tutti i test PASS;
- 0 FAIL;
- 0 SKIP dovuti a legacy;
- eventuali skip non legacy documentati in `status.md`.

**Checklist fase**:

- [x] Build backend verde
- [x] Build seeder verde
- [x] Build frontend verde
- [x] Build test project verde
- [x] Test backend verdi
- [x] Nessuno skip legacy

---

### FASE 9 - Smoke test runtime mirati

**Obiettivo**: verificare i flussi più esposti a regressione dopo la rimozione del bridge.

Smoke test consigliati:

- Home (`index.html`):
  - carica film in evidenza;
  - non chiama `/proiezioni`;
  - non mostra errori console;
  - CTA verso programmazione funziona.
- Programmazione pubblica:
  - tabs e filtri funzionano;
  - scheda film raggiungibile;
  - scelta cinema non regressa.
- Admin `shows.html`:
  - continua a gestire `Show` via `/shows`;
  - crea/modifica/elimina show con ruoli corretti;
  - nessuna chiamata a `/proiezioni`.
- Profilo:
  - mostra ordini/biglietti/credito;
  - non cerca `/prenotazioni`;
  - link navbar/footer puntano a sezioni esistenti.
- Checkout:
  - seat map carica;
  - hold posti funziona;
  - ordine pending creato;
  - pagamento solo credito o flusso hosted non regredisce.
- Validazione biglietti:
  - lookup codice funziona;
  - validazione con cinema operativo corretto funziona;
  - mismatch cinema continua a fallire.

**Checklist fase**:

- [x] Home verificata
- [x] Programmazione verificata
- [x] Admin show verificato
- [x] Profilo verificato
- [x] Checkout verificato
- [x] Validazione biglietti verificata

Nota verifica manuale:

- pagine admin verificate manualmente: OK
- pagine utente normale verificate manualmente: OK

---

### FASE 10 - Ricerca finale riferimenti residui

**Obiettivo**: confermare che il legacy è assente da runtime, seeder, frontend, test e snapshot.

Backend runtime senza migration storiche:

```bash
rg -n "Proiezione|Prenotazione|Proiezioni|Prenotazioni|/proiezioni|/prenotazioni|MapProiezioni|MapPrenotazioni|IProiezioneService|IPrenotazioneService" backend/FilmAPI --glob "!Migrations/**" --glob "!bin/**" --glob "!obj/**"
```

Seeder:

```bash
rg -n "Proiezione|Prenotazione|Proiezioni|Prenotazioni" backend/scripts/FilmApiSeeder --glob "!bin/**" --glob "!obj/**"
```

Frontend runtime:

```bash
rg -n "/proiezioni|/prenotazioni|getProiezioni|getProiezione|getPrenotazioni|createPrenotazione|deletePrenotazione|Prenotazione|Prenotazioni" frontend/CineBase.Web/wwwroot
```

Test:

```bash
rg -n "Proiezione|Prenotazione|Proiezioni|Prenotazioni|/proiezioni|/prenotazioni|Fact\(Skip|Theory\(Skip" tests/backend --glob "!bin/**" --glob "!obj/**"
```

Migration snapshot:

```bash
rg -n "FilmAPI.Model.Proiezione|FilmAPI.Model.Prenotazione|ToTable\(\"Proiezioni\"|ToTable\(\"Prenotazioni\"" backend/FilmAPI/Migrations/FilmDbContextModelSnapshot.cs
```

Regola di interpretazione:

- Nessun match deve restare in backend runtime, seeder, frontend runtime, test e snapshot.
- Sono accettabili match nelle migration storiche precedenti alla nuova migration.
- Sono accettabili match nella documentazione, purché `status.md` e `changelog.md` dichiarino che il debito è chiuso.

**Checklist fase**:

- [x] Backend runtime senza match legacy
- [x] Seeder senza match legacy
- [x] Frontend runtime senza chiamate legacy
- [x] Test senza match legacy/skipped legacy
- [x] Snapshot senza entità/tabelle legacy

---

### FASE 11 - Aggiornamento documentazione

**Obiettivo**: dichiarare chiuso il debito tecnico legacy e rendere tracciabile l'esito dell'iterazione.

Aggiornare:

- `docs/project/status.md`
- `docs/project/changelog.md`
- `docs/project/dev_iteration/4.1/PianoDiLavoro.md`

`status.md` deve indicare:

- debito tecnico `Proiezione`/`Prenotazione`: chiuso;
- endpoint `/proiezioni` e `/prenotazioni`: rimossi;
- tabelle legacy: droppate da migration dedicata;
- test backend: numero PASS/SKIP/FAIL aggiornato;
- nessuno skip legacy residuo.

`changelog.md` deve indicare:

- file rimossi;
- frontend migrato da `/proiezioni` a `/shows`/programmazione v2;
- seeder aggiornato;
- migration aggiunta;
- test legacy rimossi o sostituiti;
- verifiche eseguite.

Questo piano deve essere aggiornato:

- nella tabella `Stato Avanzamento Fasi`;
- nelle checklist delle fasi completate;
- nelle note, se una fase subisce scostamenti tecnici.

**Checklist fase**:

- [x] `status.md` aggiornato
- [x] `changelog.md` aggiornato
- [x] Piano 4.1 aggiornato con esiti reali
- [x] Eventuali skip non legacy documentati

---

## 4) File e Aree Impattate

## 4.1 Backend `backend/FilmAPI/`

File da rimuovere o verificare:

- `Endpoints/ProiezioniEndpoints.cs`
- `Endpoints/PrenotazioniEndpoints.cs`
- `Services/IProiezioneService.cs`
- `Services/ProiezioneService.cs`
- `Services/IPrenotazioneService.cs`
- `Services/PrenotazioneService.cs`
- `DTO/ProiezioneDTO.cs`
- `DTO/ProfiloPrenotazioniAdminDTO.cs`
- `Model/Proiezione.cs`
- `Model/Prenotazione.cs`
- `Program.cs`
- `Data/FilmDbContext.cs`
- `Migrations/FilmDbContextModelSnapshot.cs`

File correlati da controllare:

- `Model/Cinema.cs`
- `Model/Film.cs`
- `Model/User.cs`
- `Endpoints/ShowsEndpoints.cs`
- `Services/ShowService.cs`
- `DTO/ProfiloDTO.cs`

## 4.2 Frontend `frontend/CineBase.Web/wwwroot/`

File da controllare:

- `js/api.js`
- `js/pages/home.js`
- `js/pages/shows.js`
- `shows.html`
- `profilo.html`
- `js/pages/profilo.js`
- `components/navbar-landing.html`
- `components/footer-landing.html`
- `js/admin-shell.js`

## 4.3 Seeder

File da controllare:

- `backend/scripts/FilmApiSeeder/Program.cs`
- `backend/scripts/FilmApiSeeder/FilmApiSeeder.csproj`

## 4.4 Test

File probabili:

- `tests/backend/Unit/ProiezioneServiceTests.cs`
- `tests/backend/Integration/ProiezioneCompatIntegrationTests.cs`
- `tests/backend/Integration/PrenotazioneIntegrationTests.cs`
- `tests/backend/Integration/ApiIntegrationTests.cs`
- `tests/backend/Integration/RbacIntegrationTests.cs`
- `tests/backend/Integration/CustomWebApplicationFactory.cs`
- eventuali test `Show`, `Checkout`, `Profilo`, `Credito`, `Ticket` da estendere come copertura sostitutiva.

---

## 5) Rischi e Mitigazioni

| Rischio | Probabilità | Impatto | Mitigazione |
| --- | --- | --- | --- |
| `home.js` continua a chiamare `/proiezioni` dopo la rimozione endpoint | Alta se non gestito | Medio/Alto - home rotta | FASE 1 obbligatoria prima della rimozione backend |
| `FilmApiSeeder` non compila dopo rimozione DbSet legacy | Alta se non gestito | Alto - tooling seed rotto | FASE 2 obbligatoria prima della pulizia `FilmDbContext` |
| `ProfiloPrenotazioniAdminDTO.cs` contiene DTO non legacy ancora usati | Media | Alto - build rossa | Split DTO prima di eliminare il file |
| Migration drop in ordine sbagliato | Media | Alto - FK error | Controllare `Prenotazioni` prima di `Proiezioni` |
| Nuova migration tocca tabelle del dominio nuovo | Bassa/Media | Alto - perdita dati funzionali | Ispezionare migration e script SQL prima di `database update` |
| Test skipped legacy lasciati in suite | Alta | Medio - debito non chiuso | Ricerca finale su `Fact(Skip)`/`Theory(Skip)` e rimozione test storici |
| Riferimenti legacy nascosti in frontend copy/link | Media | Basso/Medio - UX incoerente o anchor rotte | Ricerca su `wwwroot`, smoke test profilo/navbar/home |
| Ricerca finale fallisce per migration storiche | Certa se si usa `rg` globale | Basso - falso positivo | Escludere `Migrations/**` dai controlli runtime |
| Dati storici persi | Certa se tabelle droppate | Variabile | Backup/export se i dati hanno valore reale |
| Rollback non recupera dati droppati | Certa | Alto in ambienti non dev | Eseguire solo su dev/demo o predisporre backup e piano DBA per produzione |

---

## 6) Stima Effort

| Attività | Tempo stimato |
| --- | --- |
| Preflight e mappa dipendenze | 20-30 min |
| Migrazione frontend residua (`home.js`, `api.js`, link/copy) | 45-75 min |
| Aggiornamento `FilmApiSeeder` | 20-40 min |
| Rimozione backend endpoint/service/DTO/model/context | 60-90 min |
| Migration EF + ispezione SQL/snapshot | 30-60 min |
| Pulizia test legacy e copertura sostitutiva | 60-120 min |
| Build/test completi | 30-60 min |
| Smoke test manuali mirati | 30-60 min |
| Aggiornamento documentazione | 20-40 min |
| **Totale realistico** | **mezza giornata / una giornata breve** |

---

## 7) Criteri di Accettazione Definitivi

L'Iterazione 4.1 può essere marcata completata solo se:

1. `GET /proiezioni` non è più disponibile.
2. `/prenotazioni` non è più disponibile.
3. `Program.cs` non registra né mappa service/endpoint legacy.
4. `backend/FilmAPI/Model/Proiezione.cs` è rimosso.
5. `backend/FilmAPI/Model/Prenotazione.cs` è rimosso.
6. `backend/FilmAPI/Services/*Proiezione*` è rimosso.
7. `backend/FilmAPI/Services/*Prenotazione*` è rimosso.
8. `backend/FilmAPI/Endpoints/*Proiezioni*` è rimosso.
9. `backend/FilmAPI/Endpoints/*Prenotazioni*` è rimosso.
10. `backend/FilmAPI/DTO/ProiezioneDTO.cs` è rimosso.
11. I DTO profilo/admin ancora utili sono stati spostati fuori da `ProfiloPrenotazioniAdminDTO.cs`, oppure il file è stato eliminato senza rompere profilo/admin.
12. `FilmDbContext` non contiene `DbSet` o configurazioni legacy.
13. `FilmApiSeeder` compila e non usa `Proiezioni`/`Prenotazioni`.
14. `api.js` non espone `getProiezioni`/`getProiezione`.
15. `home.js` non usa più `/proiezioni` e calcola i film in evidenza da dominio `Show`/programmazione v2.
16. I link frontend non puntano a sezioni legacy inesistenti.
17. La migration `DropLegacyProiezionePrenotazioneTables` esiste.
18. La migration droppa `Prenotazioni` prima di `Proiezioni`.
19. `FilmDbContextModelSnapshot.cs` non contiene più entità legacy.
20. Il database aggiornato non contiene più le tabelle `Prenotazioni` e `Proiezioni`.
21. I test legacy skipped sono rimossi o riscritti.
22. La suite test backend passa.
23. Non ci sono skip legacy.
24. Build backend, frontend, seeder e test passano.
25. Le ricerche finali non trovano riferimenti legacy in runtime backend, frontend, seeder, test e snapshot.
26. `status.md` e `changelog.md` sono aggiornati.

---

## 8) Prompt Operativo Consigliato

```text
Implementa l'Iterazione 4.1 descritta in `docs/project/dev_iteration/4.1/PianoDiLavoro.md`.

Obiettivo: rimuovere completamente il dominio legacy `Proiezione`/`Prenotazione` da runtime backend, frontend, seeder, test e database, consolidando l'app sul dominio `Show`/`Ordine`/`Biglietto`.

Segui rigorosamente le fasi:
1. preflight con `rg` per mappare tutti i riferimenti legacy;
2. migrazione frontend residua (`api.js`, `home.js`, link/copy) da `/proiezioni` a `/shows` o programmazione v2;
3. aggiornamento `backend/scripts/FilmApiSeeder`;
4. rimozione endpoint, DI, service, DTO, model e navigation legacy;
5. pulizia `FilmDbContext`;
6. creazione e ispezione migration `DropLegacyProiezionePrenotazioneTables`;
7. rimozione o riscrittura dei test legacy skipped;
8. build e test completi;
9. smoke test mirati;
10. ricerca finale riferimenti residui;
11. aggiornamento `status.md`, `changelog.md` e piano 4.1.

Non lasciare test skipped per legacy. Non modificare migration storiche già applicate. Non considerare completata la fase finché backend, frontend, seeder e test non compilano, la suite backend non passa, e le ricerche finali non trovano più riferimenti legacy fuori dalle migration storiche e dalla documentazione.
```
