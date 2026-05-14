// Home Page JavaScript
let featuredInterval;
let currentFeaturedIndex = 0;
let featuredEntries = [];

document.addEventListener("DOMContentLoaded", async () => {
  const params = new URLSearchParams(window.location.search);
  if (params.get("forbidden") === "true") {
    showToast("Non hai i permessi per accedere all'area admin", "warning");
    params.delete("forbidden");
    const newQuery = params.toString();
    const newUrl = `${window.location.pathname}${newQuery ? `?${newQuery}` : ""}`;
    window.history.replaceState({}, "", newUrl);
  }

  await loadFeaturedFilms();
});

async function loadFeaturedFilms() {
  const featuredGrid = document.getElementById("featured-grid");
  if (!featuredGrid) return;

  try {
    const [filmsResponse, showsResponse] = await Promise.all([
      API.getFilms({ page: 1, pageSize: 100 }),
      API.getShows({ page: 1, pageSize: 100 })
    ]);

    const films = Array.isArray(filmsResponse)
      ? filmsResponse
      : Array.isArray(filmsResponse?.items)
        ? filmsResponse.items
        : Array.isArray(filmsResponse?.$values)
          ? filmsResponse.$values
          : [];

    const shows = Array.isArray(showsResponse)
      ? showsResponse
      : Array.isArray(showsResponse?.items)
        ? showsResponse.items
        : Array.isArray(showsResponse?.$values)
          ? showsResponse.$values
          : [];

    const featured = buildFeaturedSelection(films, shows);
    initFeaturedFilms(featured);
  } catch (error) {
    handleApiError(error);
    featuredGrid.innerHTML =
      '<p class="text-brand-on-surface col-span-full text-center">Errore nel caricamento dei film in evidenza</p>';
  }
}

function buildFeaturedSelection(films, shows) {
  const next7Days = new Date();
  next7Days.setDate(next7Days.getDate() + 7);

  const upcoming = shows.filter((show) => {
    const date = new Date(show.startAtUtc);
    return Number.isFinite(date.getTime()) && date >= new Date() && date <= next7Days;
  });

  const countByFilm = new Map();
  upcoming.forEach((show) => {
    const filmId = Number(show.filmId);
    countByFilm.set(filmId, (countByFilm.get(filmId) || 0) + 1);
  });

  const filmsWithScore = films
    .map((film) => ({
      film,
      score: countByFilm.get(Number(film.id)) || 0,
      releaseDate: new Date(film.dataProduzione || 0)
    }))
    .sort((a, b) => {
      // Priorita' ai film in programmazione rispetto a quelli non programmati
      if (b.score !== a.score) return b.score - a.score;
      // Parita' di programmazione: piu' recenti
      return b.releaseDate - a.releaseDate;
    });

  // Prendi i top 5 per riempire il grid (1 hero + 4 compatti)
  return filmsWithScore.slice(0, 5);
}

function getCoverImage(copertinaPath) {
  if (!copertinaPath) return "/assets/images/defaults/cover-default.jpg";
  if (copertinaPath.startsWith("/media/")) {
    return `http://localhost:5000${copertinaPath}`;
  }
  if (!copertinaPath.includes("/") && !copertinaPath.startsWith("http")) {
    return `http://localhost:5000/media/${copertinaPath}`;
  }
  if (copertinaPath.startsWith("http")) {
    return copertinaPath;
  }
  return "/assets/images/defaults/cover-default.jpg";
}

function getDirectorName(film) {
  const flatName = [film?.registaNome, film?.registaCognome]
    .filter(Boolean)
    .join(" ")
    .trim();
  if (flatName) return flatName;

  const nestedName = [film?.regista?.nome, film?.regista?.cognome]
    .filter(Boolean)
    .join(" ")
    .trim();
  return nestedName || "Regista sconosciuto";
}

function initFeaturedFilms(entries) {
  featuredEntries = entries;
  if (!featuredEntries.length) {
    const featuredGrid = document.getElementById("featured-grid");
    featuredGrid.innerHTML =
      '<p class="text-brand-on-surface col-span-full text-center">Nessun film disponibile</p>';
    return;
  }

  updateFeaturedDisplay(0);

  if (featuredInterval) clearInterval(featuredInterval);
  if (featuredEntries.length > 1) {
    featuredInterval = setInterval(() => {
      currentFeaturedIndex = (currentFeaturedIndex + 1) % featuredEntries.length;
      updateFeaturedDisplay(currentFeaturedIndex);
    }, 6000); // Cambia ogni 6 secondi
  }
}

window.setActiveFeatured = function (index) {
  if (featuredInterval) clearInterval(featuredInterval);
  currentFeaturedIndex = index;
  updateFeaturedDisplay(currentFeaturedIndex);

  // Riavvia l'intervallo
  if (featuredEntries.length > 1) {
    featuredInterval = setInterval(() => {
      currentFeaturedIndex = (currentFeaturedIndex + 1) % featuredEntries.length;
      updateFeaturedDisplay(currentFeaturedIndex);
    }, 6000);
  }
};

window.addEventListener("resize", () => {
  if (!featuredEntries.length) return;
  updateFeaturedDisplay(currentFeaturedIndex);
});

function updateFeaturedDisplay(activeIndex) {
  const featuredGrid = document.getElementById("featured-grid");
  if (!featuredGrid) return;

  const heroEntry = featuredEntries[activeIndex];
  const sideEntries = featuredEntries.filter((_, idx) => idx !== activeIndex);
  featuredGrid.innerHTML = `
    ${renderHeroCard(heroEntry.film, heroEntry.score)}
    <div class="lg:col-span-1 flex flex-col gap-4 lg:gap-[18px] h-full">
      ${sideEntries.map((entry, idx) => {
        // Re-calcoliamo l'indice originale per il click handler
        const originalIndex = featuredEntries.indexOf(entry);
        return renderCompactCard(entry.film, entry.score, originalIndex);
      }).join("")}
    </div>
  `;
}

function renderHeroCard(film, score) {
  const badge = score > 0 ? "Top della Settimana" : "Nuovo Arrivo";
  const subBadge = score > 0 ? `${score} spettacoli in programma` : "";
  const categorie = film.categorie || [];
  const badgeHtml = categorie.length
    ? categorie.map(c => `<span class="bg-white/10 backdrop-blur-md text-white text-xs px-2.5 py-1 rounded-full font-medium">${c.nome}</span>`).join('')
    : `<span class="bg-brand-red text-xs font-bold px-2.5 py-1 rounded-full">${film.genere || "Film"}</span>`;

  const isLoggedIn = typeof Auth !== 'undefined' && Auth?.isLoggedIn?.() || false;
  const cta = isLoggedIn
    ? `<a href="/programmazione.html" class="btn-gold hero-cta-glow text-base"><i class="fa-solid fa-ticket mr-2"></i>Acquista Biglietti</a>`
    : `<a href="/programmazione.html" class="btn-outline-brand-light backdrop-blur-sm"><i class="fa-solid fa-eye mr-2"></i>Scopri Orari</a>`;

  return `
    <div class="lg:col-span-2 relative w-full max-w-full h-[118vw] min-h-[420px] max-h-[780px] lg:h-[930px] lg:max-h-none rounded-2xl overflow-hidden group animate-fade-in border border-brand-outline-variant/20 hover:border-brand-red/30 transition-all duration-500">
      <div class="absolute inset-0 bg-brand-surface-container">
        <img src="${getCoverImage(film.copertinaPath)}"
             alt="${film.titolo}"
              class="w-full h-full object-contain sm:object-cover object-top transition-transform duration-700 ease-out group-hover:scale-105 opacity-90">
        <div class="absolute inset-0 bg-gradient-to-t from-brand-surface via-brand-surface/60 to-transparent lg:bg-gradient-to-r lg:from-brand-surface lg:via-brand-surface/40 lg:to-transparent"></div>
      </div>

      <div class="absolute top-4 left-4 right-4 flex items-center justify-between z-10">
        <span class="bg-brand-red text-white text-sm font-bold px-4 py-1.5 rounded-full shadow-lg shadow-brand-red/20">${badge}</span>
        ${subBadge ? `<span class="bg-white/10 backdrop-blur-md text-white text-xs px-3 py-1.5 rounded-full border border-white/10">${subBadge}</span>` : ""}
      </div>

      <div class="absolute bottom-0 left-0 right-0 p-6 lg:p-10 z-10 flex flex-col justify-end h-full">
        <div class="flex flex-wrap gap-2 mb-3">${badgeHtml}</div>
        <h3 class="text-white font-extrabold text-3xl lg:text-5xl xl:text-6xl mb-3 drop-shadow-xl leading-[1.1] tracking-tight line-clamp-2">${film.titolo}</h3>
        <p class="text-gray-200 text-base lg:text-xl mb-6 flex items-center gap-4 font-medium drop-shadow-lg">
          <span><i class="fa-solid fa-video mr-2 text-brand-red"></i>${getDirectorName(film)}</span>
          ${film.durata ? `<span><i class="fa-regular fa-clock mr-1 text-brand-red"></i>${film.durata} min</span>` : ""}
        </p>
        <div class="flex">${cta}</div>
      </div>
    </div>
  `;
}

function renderCompactCard(film, score, originalIndex) {
  const badge = score > 0 ? "In Programmazione" : "Novita";

  return `
    <div class="group cursor-pointer animate-fade-in rounded-2xl overflow-hidden border border-brand-outline-variant/20 bg-brand-surface-container-lowest hover:border-brand-red/40 hover:shadow-xl hover:shadow-brand-red/5 transition-all duration-300 flex h-[180px] sm:h-[198px] lg:h-[213px]" onclick="setActiveFeatured(${originalIndex})">
      <div class="w-[30%] lg:w-[34%] bg-brand-surface-container relative overflow-hidden flex-shrink-0">
        <img src="${getCoverImage(film.copertinaPath)}"
             alt="${film.titolo}"
             class="w-full h-full object-cover object-center transition-transform duration-500 group-hover:scale-110">
        <div class="absolute inset-0 bg-gradient-to-r from-brand-surface/20 to-transparent"></div>
      </div>
      <div class="p-3 lg:p-4 flex flex-col justify-center flex-1 overflow-hidden">
        <div class="flex justify-between items-start mb-1 lg:mb-2">
          <span class="text-[10px] lg:text-[11px] uppercase tracking-wider text-brand-red font-bold truncate pr-1">${badge}</span>
          ${score > 0 ? `<span class="text-[10px] lg:text-[11px] text-brand-on-surface-variant font-medium flex-shrink-0"><i class="fa-solid fa-calendar-day mr-1 text-brand-red"></i>${score}</span>` : ""}
        </div>
        <h3 class="text-brand-on-surface font-bold text-sm sm:text-base lg:text-[1.1rem] mb-1 line-clamp-2 group-hover:text-brand-red-light transition-colors leading-tight">${film.titolo}</h3>
        <p class="text-brand-on-surface-variant text-xs lg:text-[13px] font-medium truncate mt-auto"><i class="fa-solid fa-video text-[10px] mr-1 opacity-50"></i>${getDirectorName(film)}</p>
      </div>
    </div>
  `;
}

window.handlePrenotaFilm = function(filmId) {
  window.location.href = `/programmazione.html`;
};
