let currentPage = 1;
const pageSize = 15;

document.addEventListener('DOMContentLoaded', () => {
  loadUsers();
  document.getElementById('search-input').addEventListener('input', debounce(loadUsers, 300));
  document.getElementById('role-filter').addEventListener('change', loadUsers);
  document.getElementById('invite-form').addEventListener('submit', handleInvite);
});

function debounce(fn, ms) {
  let t;
  return (...args) => { clearTimeout(t); t = setTimeout(() => fn(...args), ms); };
}

async function loadUsers() {
  const search = document.getElementById('search-input').value;
  const role = document.getElementById('role-filter').value;
  try {
    const data = await API.getUtentiPaged({ search, role, page: currentPage, pageSize });
    renderUsers(data.items || data.$values || []);
    renderPagination(data.totalCount);
  } catch(e) { console.error(e); }
}

function renderUsers(items) {
  const tbody = document.getElementById('users-tbody');
  if (!Array.isArray(items) || !items.length) {
    tbody.innerHTML = '<tr><td colspan="6" class="p-8 text-center text-brand-on-surface-variant">Nessun utente trovato.</td></tr>';
    return;
  }
  tbody.innerHTML = items.map(u => `
    <tr class="border-b border-brand-outline-variant/10 hover:bg-brand-surface-variant/30">
      <td class="p-4">
        <div class="font-medium">${esc(u.nome)} ${esc(u.cognome)}</div>
        <div class="text-brand-on-surface-variant text-xs">${esc(u.email)}</div>
        ${u.isDisabled ? '<span class="text-red-400 text-xs">Disabilitato</span>' : ''}
      </td>
      <td class="p-4">
        <select class="ghost-input text-xs px-2 py-1 rounded-lg" data-id="${u.id}" onchange="changeRole(${u.id}, this.value)">
          <option value="User" ${u.ruolo==='User'?'selected':''}>User</option>
          <option value="PowerUser" ${u.ruolo==='PowerUser'?'selected':''}>PowerUser</option>
          <option value="Admin" ${u.ruolo==='Admin'?'selected':''}>Admin</option>
        </select>
      </td>
      <td class="p-4">
        ${u.hasLocalPassword
          ? '<span class="text-green-400 text-xs"><i class="fa-solid fa-check"></i> Locale</span>'
          : '<span class="text-amber-400 text-xs"><i class="fa-solid fa-xmark"></i> Nessuna</span>'}
      </td>
      <td class="p-4 text-xs text-brand-on-surface-variant">
        ${u.connectedProviders?.join(', ') || '-'}
      </td>
      <td class="p-4 text-xs text-brand-on-surface-variant">
        ${u.lastLoginAtUtc ? new Date(u.lastLoginAtUtc).toLocaleDateString('it-IT') : 'Mai'}
      </td>
      <td class="p-4 text-right">
        ${!u.hasLocalPassword ? `<button onclick="requestPasswordSetup(${u.id})" class="text-brand-gold hover:text-brand-gold-light text-xs mr-3" title="Invia link password">
          <i class="fa-solid fa-key"></i>
        </button>` : ''}
      </td>
    </tr>`).join('');
}

function esc(s) { const d=document.createElement('div'); d.textContent=s; return d.innerHTML; }

function renderPagination(total) {
  const totalPages = Math.ceil(total / pageSize);
  const el = document.getElementById('pagination');
  el.innerHTML = `
    <span class="text-xs text-brand-on-surface-variant">${total} utenti totali</span>
    <div class="flex gap-1">
      <button onclick="goPage(${currentPage-1})" ${currentPage<=1?'disabled':''} class="px-3 py-1 rounded-lg text-xs border border-brand-outline-variant/30 disabled:opacity-30">&laquo;</button>
      <span class="px-3 py-1 text-xs text-brand-on-surface-variant">${currentPage} / ${totalPages||1}</span>
      <button onclick="goPage(${currentPage+1})" ${currentPage>=totalPages?'disabled':''} class="px-3 py-1 rounded-lg text-xs border border-brand-outline-variant/30 disabled:opacity-30">&raquo;</button>
    </div>`;
}

function goPage(p) {
  if (p < 1) return;
  currentPage = p;
  loadUsers();
}

async function changeRole(id, nuovoRuolo) {
  try {
    await API.updateRuolo(id, { nuovoRuolo });
    loadUsers();
  } catch(e) {
    alert(e.message || 'Errore durante il cambio ruolo.');
  }
}

async function requestPasswordSetup(id) {
  try {
    await API.requestPasswordSetup(id);
    alert('Link per impostare la password inviato.');
  } catch(e) {
    alert(e.message || 'Errore.');
  }
}

function showInviteModal() { document.getElementById('invite-modal').classList.remove('hidden'); }
function hideInviteModal() {
  document.getElementById('invite-modal').classList.add('hidden');
  const errEl = document.getElementById('invite-error');
  if (errEl) errEl.classList.add('hidden');
  document.getElementById('invite-form').reset();
}

async function handleInvite(e) {
  e.preventDefault();
  const body = {
    email: document.getElementById('invite-email').value.trim(),
    nome: document.getElementById('invite-nome').value.trim(),
    cognome: document.getElementById('invite-cognome').value.trim(),
    ruolo: document.getElementById('invite-ruolo').value
  };
  const errEl = document.getElementById('invite-error');
  try {
    await API.createAdminInvite(body);
    hideInviteModal();
    loadUsers();
  } catch(ex) {
    errEl.textContent = ex.message || 'Impossibile connettersi al server.';
    errEl.classList.remove('hidden');
  }
}
