let selectedUserId = null;
let selectedUserEmail = '';
let allCinemas = [];

function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

document.addEventListener('DOMContentLoaded', async () => {
  await loadCinemas();
  populateCinemaSelect();
  setupRicaricaForm();
  await loadRicaricheHistory();
});

async function loadCinemas() {
  try { allCinemas = normalizeCollection(await API.getCinemas()); } catch (e) { console.error('Error loading cinemas:', e); }
}

function populateCinemaSelect() {
  const select = document.getElementById('ricarica-cinema');
  if (!select) return;
  select.innerHTML = '<option value="">Nessuno</option>' +
    allCinemas.map(c => `<option value="${c.id}">${c.nome} - ${c.citta}</option>`).join('');
}

async function searchUser() {
  const input = document.getElementById('user-search-input');
  const email = input?.value?.trim();
  if (!email) {
    showToast('Inserisci un indirizzo email', 'warning');
    return;
  }

  const resultsArea = document.getElementById('search-results-area');
  const results = document.getElementById('search-results');

  try {
    const users = normalizeCollection(await API.getUserByEmail(email));
    if (!users.length) {
      showToast('Nessun utente trovato con questa email', 'warning');
      resultsArea.classList.add('hidden');
      return;
    }

    resultsArea.classList.remove('hidden');
    results.innerHTML = users.map(u => `
      <div class="card-elevated p-4 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 cursor-pointer hover:ring-2 hover:ring-brand-gold/30 transition-all"
        onclick="selectUser(${u.id}, '${escapeHtml(u.email)}', '${escapeHtml(u.nome || '')} ${escapeHtml(u.cognome || '')}', ${(u.creditoResiduo || 0).toFixed(2)})">
        <div>
          <p class="text-sm font-medium text-brand-on-surface">${u.nome || ''} ${u.cognome || ''}</p>
          <p class="text-xs text-brand-on-surface-variant">${u.email}</p>
        </div>
        <div class="text-right">
          <p class="text-xs text-brand-on-surface-variant">Saldo attuale</p>
          <p class="text-lg font-bold text-brand-gold">&euro;${(u.creditoResiduo || 0).toFixed(2)}</p>
        </div>
      </div>
    `).join('');
  } catch (error) {
    handleApiError(error);
  }
}

function escapeHtml(text) {
  if (!text) return '';
  return String(text).replace(/&/g, '&amp;').replace(/'/g, '&#39;').replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

function selectUser(id, email, name, saldo) {
  selectedUserId = id;
  selectedUserEmail = email;

  document.getElementById('selected-user-name').textContent = name || 'Utente';
  document.getElementById('selected-user-email').textContent = email;
  document.getElementById('selected-user-saldo').textContent = `\u20AC${Number(saldo).toFixed(2)}`;
  document.getElementById('selected-user-panel').classList.remove('hidden');

  loadRicaricheHistory(email);
}

function setupRicaricaForm() {
  const form = document.getElementById('ricarica-form');
  if (!form) return;

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    if (!selectedUserId) {
      showToast('Seleziona prima un utente dai risultati di ricerca', 'warning');
      return;
    }

    const importo = Number(form.importo.value);
    if (!importo || importo <= 0) {
      showToast('Inserisci un importo valido', 'warning');
      return;
    }

    const data = {
      userId: selectedUserId,
      importo: importo,
      note: form.note.value || null
    };

    const cinemaId = document.getElementById('ricarica-cinema')?.value;
    if (cinemaId) data.cinemaId = Number(cinemaId);

    try {
      const result = await API.ricaricaCredito(data);
      showToast('Ricarica effettuata con successo');

      const nuovoSaldo = result?.movimento?.saldoPost;
      if (nuovoSaldo != null) {
        document.getElementById('selected-user-saldo').textContent = `\u20AC${Number(nuovoSaldo).toFixed(2)}`;
      }

      form.reset();
      await loadRicaricheHistory(selectedUserEmail);
    } catch (error) {
      handleApiError(error);
    }
  });
}

async function loadRicaricheHistory(email) {
  const tableBody = document.getElementById('history-table-body');
  if (!tableBody) return;

  try {
    const movimenti = normalizeCollection(await API.getRicariche(email || undefined));
    if (!movimenti.length) {
      tableBody.innerHTML = '<tr><td colspan="5" class="px-6 py-4 text-center text-brand-on-surface-variant">Nessuna ricarica trovata</td></tr>';
      return;
    }

    const ricariche = movimenti.filter(m => m.tipo === 'Ricarica' || m.tipo === 'TopUp' || m.importo > 0);

    tableBody.innerHTML = ricariche.slice(0, 20).map(m => {
      const date = m.createdAtUtc ? formatDate(m.createdAtUtc) : '-';
      const operatorName = m.operatoreEmail || '-';
      const note = m.note || '-';
      return `
        <tr class="row-hover">
          <td class="px-6 py-3 whitespace-nowrap text-sm text-brand-on-surface-variant">${date}</td>
          <td class="px-6 py-3 whitespace-nowrap text-sm text-brand-on-surface">${m.userEmail || '-'}</td>
          <td class="px-6 py-3 whitespace-nowrap text-sm font-medium text-brand-emerald">&euro;${(m.importo || 0).toFixed(2)}</td>
          <td class="px-6 py-3 whitespace-nowrap text-sm text-brand-on-surface-variant">${operatorName}</td>
          <td class="px-6 py-3 text-sm text-brand-on-surface-variant max-w-xs truncate">${note}</td>
        </tr>
      `;
    }).join('');
  } catch (error) {
    handleApiError(error);
    tableBody.innerHTML = '<tr><td colspan="5" class="px-6 py-4 text-center text-brand-error">Errore nel caricamento storico</td></tr>';
  }
}
