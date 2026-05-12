let allCinemas = [];
let selectedCinemaId = null;
let allSale = [];
let editingSalaId = null;
let editingSalaCinemaId = null;
let seatLayoutSeats = [];
let editorSalaId = null;
let editorSalaName = '';
let wheelchairMode = false;
let toggleActiveMode = false;

const TIPO_SALA_LABELS = { 0: '2D', 1: '3D', 2: 'ISENSE', 3: 'XL' };

function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

document.addEventListener('DOMContentLoaded', async () => {
  await loadCinemas();
  setupCinemaFilter();
  setupSalaForm();
});

async function loadCinemas() {
  try {
    allCinemas = normalizeCollection(await API.getCinemas());
    populateCinemaSelect();
  } catch (error) {
    console.error('Error loading cinemas:', error);
  }
}

function populateCinemaSelect() {
  const select = document.getElementById('cinema-filter-select');
  if (!select) return;
  select.innerHTML = '<option value="">Seleziona un cinema...</option>' +
    allCinemas.map(c => `<option value="${c.id}">${c.nome} - ${c.citta}</option>`).join('');
}

function setupCinemaFilter() {
  const select = document.getElementById('cinema-filter-select');
  const addBtn = document.getElementById('btn-add-sala');
  if (!select) return;

  select.addEventListener('change', async () => {
    selectedCinemaId = select.value ? Number(select.value) : null;
    if (addBtn) addBtn.disabled = !selectedCinemaId;
    if (selectedCinemaId) {
      await loadSale();
    } else {
      const tableBody = document.getElementById('sale-table-body');
      if (tableBody) tableBody.innerHTML = '<tr><td colspan="7" class="px-6 py-4 text-center text-brand-on-surface-variant">Seleziona un cinema per visualizzare le sale</td></tr>';
    }
  });
}

async function loadSale() {
  const tableBody = document.getElementById('sale-table-body');
  if (!tableBody || !selectedCinemaId) return;

  try {
    allSale = normalizeCollection(await API.getSale(selectedCinemaId));
    renderSale(allSale);
  } catch (error) {
    handleApiError(error);
    if (tableBody) tableBody.innerHTML = '<tr><td colspan="7" class="px-6 py-4 text-center text-brand-error">Errore nel caricamento delle sale</td></tr>';
  }
}

function renderSale(sale) {
  const tableBody = document.getElementById('sale-table-body');
  if (!tableBody) return;

  if (!sale.length) {
    tableBody.innerHTML = '<tr><td colspan="7" class="px-6 py-4 text-center text-brand-on-surface-variant">Nessuna sala configurata per questo cinema</td></tr>';
    return;
  }

  tableBody.innerHTML = sale.map(s => {
    const tipoLabel = TIPO_SALA_LABELS[s.tipoSala] || s.tipoSala;
    const postoCount = s.posti ? s.posti.length : 0;
    const statusBadge = s.isAttiva
      ? '<span class="chip-active inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium">Attiva</span>'
      : '<span class="chip-past inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium">Non attiva</span>';

    return `
      <tr class="row-hover">
        <td class="px-6 py-4 whitespace-nowrap text-sm text-brand-on-surface font-medium">${s.numeroProgressivo}</td>
        <td class="px-6 py-4 whitespace-nowrap text-sm text-brand-on-surface">${s.nome || `Sala ${s.numeroProgressivo}`}</td>
        <td class="px-6 py-4 whitespace-nowrap text-sm text-brand-on-surface">${tipoLabel}</td>
        <td class="px-6 py-4 whitespace-nowrap text-sm text-brand-on-surface">&euro;${(s.supplemento || 0).toFixed(2)}</td>
        <td class="px-6 py-4 whitespace-nowrap">${statusBadge}</td>
        <td class="px-6 py-4 whitespace-nowrap text-sm text-brand-on-surface-variant">${postoCount}</td>
        <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
          <button onclick="editSala(${s.id})" class="text-brand-gold hover:text-brand-gold-dark mr-3" title="Modifica sala">
            <i class="fa-solid fa-pencil"></i>
          </button>
          <button onclick="openSeatEditor(${s.id}, '${escapeHtml(s.nome || ('Sala ' + s.numeroProgressivo))}')" class="text-brand-cyan hover:text-brand-cyan-light mr-3" title="Editor piantina">
            <i class="fa-solid fa-chair"></i>
          </button>
          <button onclick="deleteSala(${s.id}, '${escapeHtml(s.nome || ('Sala ' + s.numeroProgressivo))}')" class="text-red-600 hover:text-red-900" title="Elimina sala">
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

function openCreateSalaModal() {
  if (!selectedCinemaId) return;
  const form = document.getElementById('sala-form');
  if (!form) return;
  form.reset();
  editingSalaId = null;
  editingSalaCinemaId = selectedCinemaId;
  document.getElementById('modal-title').textContent = 'Aggiungi Sala';
  document.getElementById('sala-attiva').checked = true;
  document.getElementById('sala-modal').classList.remove('hidden');
}

async function editSala(salaId) {
  try {
    const sala = await API.getSala(salaId);
    if (!sala) { showToast('Sala non trovata', 'danger'); return; }
    const form = document.getElementById('sala-form');
    if (!form) return;
    editingSalaId = salaId;
    editingSalaCinemaId = sala.cinemaId;
    document.getElementById('modal-title').textContent = 'Modifica Sala';
    document.getElementById('sala-numero').value = sala.numeroProgressivo || '';
    document.getElementById('sala-tipo').value = sala.tipoSala != null ? sala.tipoSala : 0;
    document.getElementById('sala-nome').value = sala.nome || '';
    document.getElementById('sala-supplemento').value = sala.supplemento || 0;
    document.getElementById('sala-attiva').checked = sala.isAttiva !== false;
    document.getElementById('sala-modal').classList.remove('hidden');
  } catch (error) {
    handleApiError(error);
  }
}

function setupSalaForm() {
  const form = document.getElementById('sala-form');
  if (!form) return;

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    const data = {
      cinemaId: editingSalaCinemaId,
      numeroProgressivo: Number(form.numeroProgressivo.value),
      tipoSala: Number(form.tipoSala.value),
      nome: form.nome.value || null,
      supplemento: Number(form.supplemento.value) || 0,
      isAttiva: form.isAttiva.checked
    };

    try {
      if (editingSalaId) {
        await API.updateSala(editingSalaId, {
          tipoSala: data.tipoSala,
          nome: data.nome,
          supplemento: data.supplemento,
          isAttiva: data.isAttiva
        });
        showToast('Sala aggiornata con successo');
      } else {
        await API.createSala(editingSalaCinemaId, data);
        showToast('Sala creata con successo');
      }
      closeSalaModal();
      await loadSale();
    } catch (error) {
      handleApiError(error);
    }
  });
}

async function deleteSala(salaId, name) {
  document.getElementById('delete-message').textContent = `Sei sicuro di voler eliminare "${name}"?`;
  document.getElementById('delete-modal').classList.remove('hidden');
  document.getElementById('confirm-delete-btn').onclick = async () => {
    try {
      await API.deleteSala(salaId);
      showToast('Sala eliminata con successo');
      closeDeleteModal();
      await loadSale();
    } catch (error) {
      handleApiError(error);
      closeDeleteModal();
    }
  };
}

// ---- Seat Layout Editor ----

async function openSeatEditor(salaId, salaName) {
  editorSalaId = salaId;
  editorSalaName = salaName;
  document.getElementById('seat-editor-title').textContent = `Editor Piantina - ${salaName}`;
  document.getElementById('seat-editor-subtitle').textContent = 'Usa i controlli per generare la piantina. Clicca sui posti per selezionarli, poi usa i pulsanti per modificarne lo stato.';

  try {
    const posti = normalizeCollection(await API.getSalaPosti(salaId));
    seatLayoutSeats = posti.map(p => ({
      id: p.id,
      salaId: p.salaId,
      settore: p.settore || 'PLATEA',
      fila: p.fila,
      numero: p.numero,
      posX: p.posX || 0,
      posY: p.posY || 0,
      isWheelchair: p.isWheelchair || false,
      isAttivo: p.isAttivo !== false
    }));
    renderSeatGrid();
  } catch (error) {
    handleApiError(error);
  }

  document.getElementById('seat-editor-modal').classList.remove('hidden');
}

function generateSeatGrid() {
  const sectorName = document.getElementById('seat-sector').value || 'PLATEA';
  const rows = parseInt(document.getElementById('seat-rows').value) || 5;
  const cols = parseInt(document.getElementById('seat-cols').value) || 10;

  const existingIds = new Set(seatLayoutSeats.map(s => s.id));
  const maxId = seatLayoutSeats.reduce((max, s) => Math.max(max, s.id || 0), 0);

  let nextId = maxId + 1;
  const newSeats = [];

  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      const fila = r + 1;
      const numero = c + 1;
      const exists = seatLayoutSeats.find(s => s.settore === sectorName && s.fila === fila && s.numero === numero);
      if (!exists) {
        newSeats.push({
          id: nextId++,
          salaId: editorSalaId || 0,
          settore: sectorName,
          fila: fila,
          numero: numero,
          posX: c,
          posY: r,
          isWheelchair: false,
          isAttivo: true
        });
      }
    }
  }

  seatLayoutSeats = [...seatLayoutSeats, ...newSeats];
  renderSeatGrid();
}

function clearSeatGrid() {
  seatLayoutSeats = [];
  renderSeatGrid();
}

function toggleWheelchairSeat() {
  wheelchairMode = !wheelchairMode;
  toggleActiveMode = false;
  updateToolButtons();
}

function toggleSeatActive() {
  toggleActiveMode = !toggleActiveMode;
  wheelchairMode = false;
  updateToolButtons();
}

function updateToolButtons() {
  const whBtn = document.querySelector('#seat-editor-modal button[title*="disabile"]');
  const actBtn = document.querySelector('#seat-editor-modal button[title*="Attiva"]');
  if (whBtn) {
    whBtn.classList.toggle('btn-gold', wheelchairMode);
    whBtn.classList.toggle('btn-outline-brand', !wheelchairMode);
  }
  if (actBtn) {
    actBtn.classList.toggle('btn-gold', toggleActiveMode);
    actBtn.classList.toggle('btn-outline-brand', !toggleActiveMode);
  }
}

function onSeatClick(seatIndex) {
  if (wheelchairMode) {
    seatLayoutSeats[seatIndex].isWheelchair = !seatLayoutSeats[seatIndex].isWheelchair;
    wheelchairMode = false;
    updateToolButtons();
  } else if (toggleActiveMode) {
    seatLayoutSeats[seatIndex].isAttivo = !seatLayoutSeats[seatIndex].isAttivo;
    toggleActiveMode = false;
    updateToolButtons();
  }
  renderSeatGrid();
}

function renderSeatGrid() {
  const container = document.getElementById('seat-grid-container');
  const countLabel = document.getElementById('seat-count-label');
  if (!container) return;

  if (!seatLayoutSeats.length) {
    container.innerHTML = '<p class="text-sm text-brand-on-surface-variant text-center">Usa "Genera" per creare la piantina</p>';
    if (countLabel) countLabel.textContent = '';
    return;
  }

  const groups = {};
  seatLayoutSeats.forEach((seat, idx) => {
    const key = seat.settore;
    if (!groups[key]) groups[key] = [];
    groups[key].push({ ...seat, _idx: idx });
  });

  const activeCount = seatLayoutSeats.filter(s => s.isAttivo).length;

  let html = '';
  for (const [settore, seats] of Object.entries(groups)) {
    const maxFila = Math.max(...seats.map(s => s.fila));
    const maxNumero = Math.max(...seats.map(s => s.numero));

    html += `<div class="mb-4"><p class="text-xs font-semibold text-brand-on-surface-variant mb-2 uppercase tracking-wider">${settore}</p>`;
    html += '<div class="flex flex-col items-center gap-2">';

    for (let r = 1; r <= maxFila; r++) {
      html += '<div class="flex items-center gap-2">';
      html += `<span class="text-xs text-brand-on-surface-variant w-6 text-right">${String.fromCharCode(64 + r)}</span>`;
      for (let c = 1; c <= maxNumero; c++) {
        const seat = seats.find(s => s.fila === r && s.numero === c);
        if (seat) {
          let seatClass = 'seat-available';
          if (!seat.isAttivo) seatClass = 'seat-inactive';
          else if (seat.isWheelchair) seatClass = 'seat-wheelchair';

          html += `<button onclick="onSeatClick(${seat._idx})" 
            class="seat-btn ${seatClass} w-7 h-7 rounded text-[10px] font-medium transition-colors"
            title="Fila ${seat.fila} Posto ${seat.numero}${seat.isWheelchair ? ' ♿' : ''}${!seat.isAttivo ? ' (disattivato)' : ''}">${seat.numero}</button>`;
        } else {
          html += '<span class="w-7 h-7"></span>';
        }
      }
      html += '</div>';
    }
    html += '</div></div>';
  }

  container.innerHTML = html;
  if (countLabel) countLabel.textContent = `Totale posti: ${seatLayoutSeats.length} (attivi: ${activeCount})`;
}

async function saveSeatLayout() {
  if (!editorSalaId) return;
  const posti = seatLayoutSeats.map(s => ({
    settore: s.settore || 'PLATEA',
    fila: s.fila,
    numero: s.numero,
    posX: s.posX || null,
    posY: s.posY || null,
    isWheelchair: s.isWheelchair || false,
    isAttivo: s.isAttivo !== false
  }));

  try {
    await API.saveSalaPosti(editorSalaId, { posti });
    showToast('Piantina salvata con successo');
    closeSeatEditor();
    await loadSale();
  } catch (error) {
    handleApiError(error);
  }
}
