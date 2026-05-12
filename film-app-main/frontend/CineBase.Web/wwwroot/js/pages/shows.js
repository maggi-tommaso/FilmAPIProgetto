let allCinemas = [];
let allFilms = [];
let allSale = [];
let currentPage = 1;
const pageSize = 10;
let totalPages = 1;
let totalShowsCount = 0;
let editingShowId = null;

const TIPO_SALA_LABELS = { 0: '2D', 1: '3D', 2: 'ISENSE', 3: 'XL' };

function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

function normalizePaged(data) {
  if (Array.isArray(data) || Array.isArray(data?.$values)) {
    const items = normalizeCollection(data);
    return { items, page: 1, pageSize: items.length || pageSize, totalCount: items.length, totalPages: 1 };
  }
  const items = normalizeCollection(data?.items ?? data?.Items ?? []);
  const resolvedPage = Number(data?.page ?? data?.Page ?? 1);
  const resolvedPageSize = Number(data?.pageSize ?? data?.PageSize ?? pageSize);
  const resolvedTotalCount = Number(data?.totalCount ?? data?.TotalCount ?? items.length);
  const resolvedTotalPages = Number(data?.totalPages ?? data?.TotalPages ?? 1);
  return {
    items,
    page: Number.isFinite(resolvedPage) && resolvedPage > 0 ? resolvedPage : 1,
    pageSize: Number.isFinite(resolvedPageSize) && resolvedPageSize > 0 ? resolvedPageSize : pageSize,
    totalCount: Number.isFinite(resolvedTotalCount) && resolvedTotalCount >= 0 ? resolvedTotalCount : items.length,
    totalPages: Number.isFinite(resolvedTotalPages) && resolvedTotalPages > 0 ? resolvedTotalPages : 1
  };
}

document.addEventListener('DOMContentLoaded', async () => {
  await Promise.all([loadCinemas(), loadFilms()]);
  populateFilterCinemas();
  populateShowCinemaSelect();
  populateShowFilmSelect();
  setupFilters();
  setupShowForm();
  setupCascadingCinema();
  await loadShows();
});

async function loadCinemas() {
  try { allCinemas = normalizeCollection(await API.getCinemas()); } catch (e) { console.error('Error loading cinemas:', e); }
}

async function loadFilms() {
  try { allFilms = normalizeCollection(await API.getFilms()); } catch (e) { console.error('Error loading films:', e); }
}

function populateFilterCinemas() {
  const select = document.getElementById('filter-cinema');
  if (!select) return;
  select.innerHTML = '<option value="">Tutti i cinema</option>' +
    allCinemas.map(c => `<option value="${c.id}">${c.nome} - ${c.citta}</option>`).join('');
}

function populateShowCinemaSelect() {
  const select = document.getElementById('show-cinema');
  if (!select) return;
  select.innerHTML = '<option value="">Seleziona Cinema</option>' +
    allCinemas.map(c => `<option value="${c.id}">${c.nome} - ${c.citta}</option>`).join('');
}

function populateShowFilmSelect() {
  const select = document.getElementById('show-film');
  if (!select) return;
  select.innerHTML = '<option value="">Seleziona Film</option>' +
    allFilms.map(f => `<option value="${f.id}">${f.titolo}</option>`).join('');
}

function populateShowSalaSelect(sale) {
  const select = document.getElementById('show-sala');
  if (!select) return;
  select.innerHTML = '<option value="">Seleziona Sala</option>' +
    sale.map(s => `<option value="${s.id}">Sala ${s.numeroProgressivo} - ${TIPO_SALA_LABELS[s.tipoSala] || s.tipoSala} (${s.posti ? s.posti.length : 0} posti)</option>`).join('');
}

function setupCascadingCinema() {
  const cinemaSelect = document.getElementById('show-cinema');
  const salaSelect = document.getElementById('show-sala');
  if (!cinemaSelect || !salaSelect) return;

  cinemaSelect.addEventListener('change', async () => {
    const cinemaId = cinemaSelect.value;
    salaSelect.innerHTML = '<option value="">Caricamento sale...</option>';
    salaSelect.disabled = !cinemaId;

    if (!cinemaId) {
      salaSelect.innerHTML = '<option value="">Seleziona prima il cinema</option>';
      return;
    }

    try {
      const sale = normalizeCollection(await API.getSale(Number(cinemaId)));
      populateShowSalaSelect(sale);
      salaSelect.disabled = false;
    } catch (e) {
      salaSelect.innerHTML = '<option value="">Errore caricamento sale</option>';
    }
  });

  // Also handle the filter cascading
  const filterCinema = document.getElementById('filter-cinema');
  const filterSala = document.getElementById('filter-sala');
  if (!filterCinema || !filterSala) return;

  filterCinema.addEventListener('change', async () => {
    const cinemaId = filterCinema.value;
    if (!cinemaId) {
      filterSala.innerHTML = '<option value="">Tutte le sale</option>';
      filterSala.disabled = true;
    } else {
      try {
        const sale = normalizeCollection(await API.getSale(Number(cinemaId)));
        filterSala.innerHTML = '<option value="">Tutte le sale</option>' +
          sale.map(s => `<option value="${s.id}">Sala ${s.numeroProgressivo} - ${TIPO_SALA_LABELS[s.tipoSala] || s.tipoSala}</option>`).join('');
        filterSala.disabled = false;
      } catch (e) {
        filterSala.innerHTML = '<option value="">Errore</option>';
      }
    }
    currentPage = 1;
    await loadShows();
  });
}

function setupFilters() {
  const filterSala = document.getElementById('filter-sala');
  const filterFilm = document.getElementById('filter-film');
  const filterDate = document.getElementById('filter-date');

  const populateFilmFilter = () => {
    if (!filterFilm) return;
    filterFilm.innerHTML = '<option value="">Tutti i film</option>' +
      allFilms.map(f => `<option value="${f.id}">${f.titolo}</option>`).join('');
  };
  populateFilmFilter();

  if (filterSala) {
    filterSala.addEventListener('change', async () => { currentPage = 1; await loadShows(); });
  }
  if (filterFilm) {
    filterFilm.addEventListener('change', async () => { currentPage = 1; await loadShows(); });
  }
  if (filterDate) {
    filterDate.addEventListener('change', async () => { currentPage = 1; await loadShows(); });
  }
}

async function loadShows() {
  const tableBody = document.getElementById('shows-table-body');
  if (!tableBody) return;

  try {
    const params = { page: currentPage, pageSize };
    const cinemaId = document.getElementById('filter-cinema')?.value;
    const salaId = document.getElementById('filter-sala')?.value;
    const filmId = document.getElementById('filter-film')?.value;
    const date = document.getElementById('filter-date')?.value;

    if (cinemaId) params.cinemaId = Number(cinemaId);
    if (salaId) params.cinemaId = params.cinemaId || undefined;
    if (filmId) params.filmId = Number(filmId);
    if (date) params.date = date;

    const response = await API.getShows(params);
    const paged = normalizePaged(response);
    totalPages = paged.totalPages;
    totalShowsCount = paged.totalCount;
    currentPage = paged.page;

    renderShows(paged.items);
    renderPagination(paged.items.length);
  } catch (error) {
    handleApiError(error);
    tableBody.innerHTML = '<tr><td colspan="8" class="px-6 py-4 text-center text-brand-error">Errore nel caricamento degli show</td></tr>';
    renderPagination(0);
    document.getElementById('pagination-info').textContent = 'Nessun risultato';
  }
}

function resolveName(id, collection, nameField) {
  if (!id) return '-';
  const item = collection.find(i => Number(i.id) === Number(id));
  return item ? item[nameField] : `ID ${id}`;
}

function renderShows(shows) {
  const tableBody = document.getElementById('shows-table-body');
  if (!tableBody) return;

  if (!shows.length) {
    tableBody.innerHTML = '<tr><td colspan="8" class="px-6 py-4 text-center text-brand-on-surface-variant">Nessuno show trovato</td></tr>';
    return;
  }

  tableBody.innerHTML = shows.map(show => {
    const filmTitle = show.filmTitolo || resolveName(show.filmId, allFilms, 'titolo');
    const cinemaNome = show.cinemaNome || resolveName(show.cinemaId, allCinemas, 'nome');
    const salaNome = show.salaNome || `Sala (ID ${show.salaId})`;
    const tipoLabel = TIPO_SALA_LABELS[show.salaTipo] || '-';
    const startDate = show.startAtUtc ? formatDate(show.startAtUtc) : '-';
    const startTime = show.startAtUtc ? formatTime(show.startAtUtc) : '-';
    const prezzo = show.prezzoBase != null ? `&euro;${Number(show.prezzoBase).toFixed(2)}` : '-';

    return `
      <tr class="row-hover">
        <td class="px-6 py-4 whitespace-nowrap text-sm text-brand-on-surface-variant">${show.id}</td>
        <td class="px-6 py-4 whitespace-nowrap text-sm text-brand-on-surface">${filmTitle}</td>
        <td class="px-6 py-4 whitespace-nowrap text-sm text-brand-on-surface">${cinemaNome}</td>
        <td class="px-6 py-4 whitespace-nowrap text-sm text-brand-on-surface">${salaNome}</td>
        <td class="px-6 py-4 whitespace-nowrap text-sm text-brand-on-surface-variant">${tipoLabel}</td>
        <td class="px-6 py-4 whitespace-nowrap text-sm text-brand-on-surface-variant">${startDate} ${startTime}</td>
        <td class="px-6 py-4 whitespace-nowrap text-sm text-brand-on-surface-variant">${prezzo}</td>
        <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
          <button onclick="editShow(${show.id})" class="text-brand-gold hover:text-brand-gold-dark mr-3" title="Modifica show">
            <i class="fa-solid fa-pencil"></i>
          </button>
          <button onclick="deleteShow(${show.id}, '${escapeHtml(filmTitle)}')" class="text-red-600 hover:text-red-900" title="Elimina show">
            <i class="fa-solid fa-trash"></i>
          </button>
        </td>
      </tr>
    `;
  }).join('');
}

function escapeHtml(text) {
  if (!text) return '';
  return String(text).replace(/&/g, '&amp;').replace(/'/g, '&#39;').replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

function renderPagination(serverItemsCount) {
  const paginationInfo = document.getElementById('pagination-info');
  const pageIndicator = document.getElementById('page-indicator');
  const firstBtn = document.getElementById('pagination-first');
  const prevBtn = document.getElementById('pagination-prev');
  const nextBtn = document.getElementById('pagination-next');
  const lastBtn = document.getElementById('pagination-last');

  if (!paginationInfo || !pageIndicator || !firstBtn || !prevBtn || !nextBtn || !lastBtn) return;

  if (totalShowsCount === 0 || serverItemsCount === 0) {
    paginationInfo.textContent = 'Nessun risultato';
    pageIndicator.textContent = 'Pagina 1 di 1';
    firstBtn.disabled = true;
    prevBtn.disabled = true;
    nextBtn.disabled = true;
    lastBtn.disabled = true;
    return;
  }

  const startItem = ((currentPage - 1) * pageSize) + 1;
  const endItem = Math.min(currentPage * pageSize, totalShowsCount);
  paginationInfo.textContent = `Mostrando ${startItem}-${endItem} di ${totalShowsCount} show`;
  pageIndicator.textContent = `Pagina ${currentPage} di ${totalPages}`;
  firstBtn.disabled = currentPage <= 1;
  prevBtn.disabled = currentPage <= 1;
  nextBtn.disabled = currentPage >= totalPages;
  lastBtn.disabled = currentPage >= totalPages;
}

async function goToPage(page) {
  if (page < 1 || page > totalPages || page === currentPage) return;
  currentPage = page;
  await loadShows();
}

function goToFirstPage() { goToPage(1); }
function goToPrevPage() { goToPage(currentPage - 1); }
function goToNextPage() { goToPage(currentPage + 1); }
function goToLastPage() { goToPage(totalPages); }

function openShowModal() {
  const form = document.getElementById('show-form');
  if (!form) return;
  form.reset();
  editingShowId = null;
  document.getElementById('modal-title').textContent = 'Aggiungi Show';
  document.getElementById('show-sala').disabled = true;
  document.getElementById('show-sala').innerHTML = '<option value="">Seleziona prima il cinema</option>';
  document.getElementById('show-modal').classList.remove('hidden');
}

async function editShow(id) {
  try {
    const show = await API.getShow(id);
    if (!show) { showToast('Show non trovato', 'danger'); return; }

    const form = document.getElementById('show-form');
    if (!form) return;
    editingShowId = id;

    document.getElementById('modal-title').textContent = 'Modifica Show';
    document.getElementById('show-cinema').value = show.cinemaId || '';

    const salaSelect = document.getElementById('show-sala');
    const cinemaId = show.cinemaId;
    if (cinemaId) {
      const sale = normalizeCollection(await API.getSale(Number(cinemaId)));
      populateShowSalaSelect(sale);
      salaSelect.disabled = false;
      salaSelect.value = show.salaId || '';
    }

    document.getElementById('show-film').value = show.filmId || '';

    if (show.startAtUtc) {
      const d = new Date(show.startAtUtc);
      const pad = (n) => String(n).padStart(2, '0');
      document.getElementById('show-start').value =
        `${d.getFullYear()}-${pad(d.getMonth()+1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
    }

    document.getElementById('show-durata').value = show.durataMinutiSnapshot || '';
    document.getElementById('show-prezzo').value = show.prezzoBase != null ? show.prezzoBase : '';

    document.getElementById('show-modal').classList.remove('hidden');
  } catch (error) {
    handleApiError(error);
  }
}

function setupShowForm() {
  const form = document.getElementById('show-form');
  if (!form) return;

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    const data = {
      cinemaId: Number(form.cinemaId.value),
      salaId: Number(form.salaId.value),
      filmId: Number(form.filmId.value),
      startAtUtc: form.startAtUtc.value ? new Date(form.startAtUtc.value).toISOString() : null
    };

    const durata = form.durataMinutiSnapshot.value;
    if (durata) data.durataMinutiSnapshot = Number(durata);

    const prezzo = form.prezzoBase.value;
    if (prezzo) data.prezzoBase = Number(prezzo);

    try {
      if (editingShowId) {
        await API.updateShow(editingShowId, {
          cinemaId: data.cinemaId,
          salaId: data.salaId,
          filmId: data.filmId,
          startAtUtc: data.startAtUtc,
          durataMinutiSnapshot: data.durataMinutiSnapshot,
          prezzoBase: data.prezzoBase
        });
        showToast('Show aggiornato con successo');
      } else {
        await API.createShow(data);
        showToast('Show creato con successo');
      }
      closeShowModal();
      await loadShows();
    } catch (error) {
      handleApiError(error);
    }
  });
}

async function deleteShow(id, title) {
  document.getElementById('delete-message').textContent = `Sei sicuro di voler eliminare lo show "${title}"?`;
  document.getElementById('delete-modal').classList.remove('hidden');
  document.getElementById('confirm-delete-btn').onclick = async () => {
    try {
      await API.deleteShow(id);
      showToast('Show eliminato con successo');
      closeDeleteModal();
      await loadShows();
    } catch (error) {
      handleApiError(error);
      closeDeleteModal();
    }
  };
}

window.goToPage = goToPage;
window.goToFirstPage = goToFirstPage;
window.goToPrevPage = goToPrevPage;
window.goToNextPage = goToNextPage;
window.goToLastPage = goToLastPage;
