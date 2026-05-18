let profiloData = null;
let creditoData = null;

document.addEventListener('DOMContentLoaded', async () => {
  if (!Auth?.isLoggedIn?.()) {
    window.location.replace('/login.html?redirect=' + encodeURIComponent(window.location.pathname + window.location.search));
    return;
  }

  await Promise.all([
    loadProfilo(),
    loadCredito(),
    loadOrdini(),
    loadBiglietti(),
    loadSecurity(),
    loadNotifiche(),
  ]);

  await loadCinemaSelect();
  setupProfiloForm();
  setupPrivacyActions();
});

async function loadProfilo() {
  try {
    profiloData = await API.getProfilo();
    fillProfiloForm();
  } catch (error) {
    handleApiError(error);
  }
}

function fillProfiloForm() {
  if (!profiloData) return;
  document.getElementById('profilo-email').value = profiloData.email || '';
  document.getElementById('profilo-nome').value = profiloData.nome || '';
  document.getElementById('profilo-cognome').value = profiloData.cognome || '';
  document.getElementById('profilo-telefono').value = profiloData.telefono || '';
}

function setupProfiloForm() {
  const form = document.getElementById('profilo-form');
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    const cinemaSelect = document.getElementById('profilo-cinema');
    const cinemaId = cinemaSelect?.value ? parseInt(cinemaSelect.value, 10) : null;

    const data = {
      nome: document.getElementById('profilo-nome').value.trim(),
      cognome: document.getElementById('profilo-cognome').value.trim(),
      telefono: document.getElementById('profilo-telefono').value.trim() || null
    };

    try {
      profiloData = await API.updateProfilo(data);
      await API.setCinemaPreferito(cinemaId);
      fillProfiloForm();
      const user = Auth.getUser();
      if (user) {
        user.nome = profiloData.nome;
        user.cognome = profiloData.cognome;
        Auth.saveUser(user);
      }
      showToast('Profilo aggiornato con successo');
      if (typeof window.updateAuthUI === 'function') window.updateAuthUI();
      const savedEl = document.getElementById('profilo-saved');
      if (savedEl) {
        savedEl.classList.remove('hidden');
        setTimeout(() => savedEl.classList.add('hidden'), 2000);
      }
    } catch (error) {
      handleApiError(error);
    }
  });
}

async function loadCinemaSelect() {
  const select = document.getElementById('profilo-cinema');
  if (!select) return;

  try {
    const [cinemaPref, cinemas] = await Promise.all([
      API.getCinemaPreferito(),
      API.getMyCinemas()
    ]);

    const cinemaList = Array.isArray(cinemas) ? cinemas
      : Array.isArray(cinemas?.$values) ? cinemas.$values
      : Array.isArray(cinemas?.items) ? cinemas.items
      : [];

    cinemaList.sort((a, b) => (a.nome || '').localeCompare(b.nome || '')).forEach(c => {
      const option = document.createElement('option');
      option.value = c.id;
      option.textContent = c.nome + ' — ' + c.citta;
      if (cinemaPref?.cinemaId && Number(c.id) === Number(cinemaPref.cinemaId)) {
        option.selected = true;
      }
      select.appendChild(option);
    });
  } catch {
    // silent fail, dropdown will just show "Nessuna preferenza"
  }
}

async function loadCredito() {
  const container = document.getElementById('credito-content');
  try {
    creditoData = await API.getCreditoMe();

    const saldo = creditoData.saldoAttuale || 0;
    const movimenti = creditoData.movimenti || [];

    var html = '<div class="flex items-center justify-between mb-4"><div><p class="text-sm text-brand-on-surface-variant">Saldo disponibile</p><p class="text-2xl font-bold text-brand-gold">' + formatCurrency(saldo) + '</p></div><div class="w-12 h-12 rounded-xl bg-brand-gold/15 flex items-center justify-center"><i class="fa-solid fa-wallet text-brand-gold text-xl"></i></div></div>';

    if (movimenti.length > 0) {
      const recentMov = movimenti.slice(0, 5);
      html += '<div class="border-t border-brand-outline-variant/20 pt-3 mt-3"><p class="text-xs font-semibold text-brand-on-surface-variant uppercase tracking-wider mb-2">Ultimi movimenti</p><div class="space-y-2">';

      recentMov.forEach(m => {
        const isPositive = m.tipo === 'TopUp' || m.tipo === 'Refund';
        const icon = isPositive ? 'fa-arrow-down' : 'fa-arrow-up';
        const color = isPositive ? 'text-emerald-500' : 'text-red-400';
        const sign = isPositive ? '+' : '';
        const date = new Date(m.createdAtUtc);

        html += '<div class="flex items-center justify-between text-sm"><div class="flex items-center gap-2"><i class="fa-solid ' + icon + ' ' + color + ' text-xs"></i><span class="text-brand-on-surface">' + getMovimentoLabel(m.tipo) + '</span></div><div class="text-right"><span class="' + color + ' font-semibold">' + sign + formatCurrency(m.importo) + '</span><p class="text-xs text-brand-on-surface-variant">' + date.toLocaleDateString('it-IT', { day: 'numeric', month: 'short' }) + '</p></div></div>';
      });

      html += '</div></div>';
    }

    container.innerHTML = html;
  } catch {
    container.innerHTML = '<p class="text-sm text-brand-on-surface-variant">Errore caricamento credito</p>';
  }
}

function renderOrdineCard(o) {
  const startDate = new Date(o.startAtUtc);
  const dateStr = startDate.toLocaleDateString('it-IT', { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  const biglietti = normalizeCollection(o.biglietti || []);
  var activeCount = 0, refundedCount = 0, validatedCount = 0;
  biglietti.forEach(function(b) {
    if (b.stato === 'Refunded') refundedCount++;
    else if (b.stato === 'Validated') validatedCount++;
    else if (b.stato === 'Issued') activeCount++;
  });
  const allRefunded = biglietti.length > 0 && refundedCount === biglietti.length;
  const statoBadge = allRefunded ? getStatoBadge('Refunded') : getStatoBadge(o.stato);

  var ticketListHtml = '';
  if (biglietti.length > 0) {
    ticketListHtml = '<div class="mt-2 pt-2 border-t border-brand-outline-variant/10 text-xs space-y-1">';
    biglietti.forEach(function(b) {
      var tStatoIcon = '', tStatoClass = '';
      if (b.stato === 'Validated') { tStatoIcon = 'fa-circle-check text-blue-400'; tStatoClass = 'text-blue-400'; }
      else if (b.stato === 'Refunded') { tStatoIcon = 'fa-rotate-left text-amber-500'; tStatoClass = 'text-amber-500'; }
      else if (b.stato === 'Cancelled') { tStatoIcon = 'fa-ban text-brand-on-surface-variant'; tStatoClass = 'text-brand-on-surface-variant'; }
      else { tStatoIcon = 'fa-ticket text-emerald-400'; tStatoClass = 'text-emerald-400'; }
      ticketListHtml += '<div class="flex items-center justify-between"><span class="' + tStatoClass + '"><i class="fa-solid ' + tStatoIcon + ' mr-1"></i>' + (b.settore || '') + ' F' + b.fila + ' P' + b.numero + '</span><span class="text-brand-on-surface-variant">' + formatCurrency(b.prezzoTotale) + '</span></div>';
    });
    ticketListHtml += '</div>';
  }

  return '<div class="border border-brand-outline-variant/20 rounded-xl p-4 mb-3 hover:bg-brand-surface-container-high/50 transition-colors">' +
    '<div class="flex justify-between items-start">' +
      '<div class="flex-1 min-w-0">' +
        '<div class="flex items-center gap-2 mb-1">' +
          '<h3 class="font-semibold text-brand-on-surface truncate">' + o.filmTitolo + '</h3>' +
          statoBadge +
        '</div>' +
        '<p class="text-sm text-brand-on-surface-variant"><i class="fa-solid fa-location-dot mr-1"></i>' + o.cinemaNome + ' - ' + o.salaNome + '</p>' +
        '<p class="text-sm text-brand-on-surface-variant"><i class="fa-regular fa-calendar mr-1"></i>' + dateStr + '</p>' +
        '<div class="flex flex-wrap gap-3 mt-2 text-sm">' +
          '<span class="text-brand-on-surface-variant"><i class="fa-solid fa-ticket mr-1"></i>' + o.numeroBiglietti + ' bigliett' + (o.numeroBiglietti === 1 ? 'o' : 'i') + '</span>' +
          '<span class="text-brand-gold font-semibold">' + formatCurrency(o.totaleLordo) + '</span>' +
        '</div>' +
        '<p class="text-xs text-brand-on-surface-variant mt-1 font-mono">' + o.codiceOrdine + '</p>' +
        ticketListHtml +
      '</div>' +
      '<div class="flex flex-col gap-1 ml-2 flex-shrink-0">' +
        (o.stato === 'Paid' ? '<button onclick="downloadPdf(' + o.id + ')" class="btn-ghost text-xs" title="Scarica PDF"><i class="fa-solid fa-file-pdf mr-1"></i>PDF</button>' : '') +
        '<a href="/esito-acquisto.html?orderId=' + o.id + '" class="btn-ghost text-xs" title="Dettagli"><i class="fa-solid fa-eye mr-1"></i>Dettagli</a>' +
      '</div>' +
    '</div>' +
  '</div>';
}

function renderBigliettoCard(b) {
  const startDate = new Date(b.startAtUtc);
  const dateStr = startDate.toLocaleDateString('it-IT', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' });
  const statoClass = b.stato === 'Issued' ? 'text-emerald-500' : b.stato === 'Validated' ? 'text-blue-500' : 'text-brand-on-surface-variant';
  const statoLabel = b.stato === 'Issued' ? 'Emesso' : b.stato === 'Validated' ? 'Validato' : b.stato;

  return '<div class="border border-brand-outline-variant/20 rounded-xl p-4 mb-3 hover:bg-brand-surface-container-high/50 transition-colors">' +
    '<div class="flex justify-between items-start">' +
      '<div class="flex-1 min-w-0">' +
        '<div class="flex items-center gap-2 mb-1">' +
          '<h3 class="font-semibold text-brand-on-surface truncate">' + b.filmTitolo + '</h3>' +
          '<span class="' + statoClass + ' text-xs font-semibold">' + statoLabel + '</span>' +
        '</div>' +
        '<p class="text-sm text-brand-on-surface-variant"><i class="fa-solid fa-location-dot mr-1"></i>' + b.cinemaNome + ' - ' + b.salaNome + '</p>' +
        '<p class="text-sm text-brand-on-surface-variant"><i class="fa-regular fa-calendar mr-1"></i>' + dateStr + '</p>' +
        '<div class="flex flex-wrap gap-3 mt-2 text-sm">' +
          '<span class="text-brand-on-surface-variant"><i class="fa-solid fa-chair mr-1"></i>' + b.settore + ' - Fila ' + b.fila + ', Posto ' + b.numero + '</span>' +
          '<span class="text-brand-gold font-semibold">' + formatCurrency(b.prezzoTotale) + '</span>' +
        '</div>' +
        '<p class="text-xs text-brand-on-surface-variant mt-1 font-mono">' + b.codiceBiglietto + '</p>' +
        (b.validatoAtUtc ? '<p class="text-xs text-blue-500 mt-1"><i class="fa-solid fa-check mr-1"></i>Validato il ' + new Date(b.validatoAtUtc).toLocaleDateString('it-IT', { day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' }) + '</p>' : '') +
      '</div>' +
      '<div class="flex flex-col gap-1 ml-2 flex-shrink-0">' +
        (b.stato === 'Issued' ? '<a href="/validazione-biglietti.html?codice=' + encodeURIComponent(b.codiceBiglietto) + '" class="btn-ghost text-xs text-emerald-400 hover:text-emerald-300" title="Apri pagina validazione"><i class="fa-solid fa-qrcode mr-1"></i>Valida</a>' : '') +
        (b.stato === 'Issued' ? '<button onclick="refundTicket(' + b.id + ')" class="btn-ghost text-xs text-red-400 hover:text-red-300" title="Richiedi rimborso"><i class="fa-solid fa-rotate-left mr-1"></i>Rimborsa</button>' : '') +
      '</div>' +
    '</div>' +
  '</div>';
}

function toggleSection(hiddenId, btn) {
  const hidden = document.getElementById(hiddenId);
  if (!hidden) return;
  const isHidden = hidden.classList.contains('hidden');
  if (isHidden) {
    hidden.classList.remove('hidden');
    btn.innerHTML = '<i class="fa-solid fa-chevron-up mr-2"></i>Comprimi';
  } else {
    hidden.classList.add('hidden');
    btn.innerHTML = '<i class="fa-solid fa-chevron-down mr-2"></i>' + btn.getAttribute('data-original-text');
  }
}

async function loadOrdini() {
  const container = document.getElementById('ordini-list');
  try {
    const data = await API.getOrdini();
    const ordini = normalizeCollection(data);

    if (!ordini.length) {
      container.innerHTML = '<div class="text-center py-8 text-brand-on-surface-variant"><i class="fa-solid fa-receipt text-4xl mb-3 opacity-40"></i><p class="font-medium">Nessun ordine</p><p class="text-sm mt-1">I tuoi ordini appariranno qui</p></div>';
      return;
    }

    const MAX_VISIBLE = 2;
    const hasMore = ordini.length > MAX_VISIBLE;

    var html = ordini.slice(0, MAX_VISIBLE).map(renderOrdineCard).join('');

    if (hasMore) {
      const hiddenCount = ordini.length - MAX_VISIBLE;
      html += '<div id="ordini-hidden" class="hidden">' + ordini.slice(MAX_VISIBLE).map(renderOrdineCard).join('') + '</div>';
      html += '<button onclick="toggleSection(\'ordini-hidden\', this)" data-original-text="Mostra tutti gli ordini (' + ordini.length + ')" class="btn-ghost text-sm w-full text-center py-2 text-brand-gold hover:text-brand-gold-light"><i class="fa-solid fa-chevron-down mr-2"></i>Mostra tutti gli ordini (' + ordini.length + ')</button>';
    }

    container.innerHTML = html;
  } catch {
    container.innerHTML = '<p class="text-sm text-brand-error text-center py-4">Errore caricamento ordini</p>';
  }
}

async function loadBiglietti() {
  const container = document.getElementById('biglietti-list');
  try {
    const data = await API.getBiglietti();
    const biglietti = normalizeCollection(data);

    const activeBiglietti = biglietti.filter(function(b) { return b.stato !== 'Refunded' && b.stato !== 'Cancelled'; });

    if (!activeBiglietti.length) {
      container.innerHTML = '<div class="text-center py-8 text-brand-on-surface-variant"><i class="fa-solid fa-ticket text-4xl mb-3 opacity-40"></i><p class="font-medium">Nessun biglietto attivo</p><p class="text-sm mt-1">I biglietti rimborsati o annullati sono visibili nella sezione ordini</p></div>';
      return;
    }

    const MAX_VISIBLE = 3;
    const hasMore = activeBiglietti.length > MAX_VISIBLE;

    var html = activeBiglietti.slice(0, MAX_VISIBLE).map(renderBigliettoCard).join('');

    if (hasMore) {
      html += '<div id="biglietti-hidden" class="hidden">' + activeBiglietti.slice(MAX_VISIBLE).map(renderBigliettoCard).join('') + '</div>';
      html += '<button onclick="toggleSection(\'biglietti-hidden\', this)" data-original-text="Mostra tutti i biglietti (' + activeBiglietti.length + ')" class="btn-ghost text-sm w-full text-center py-2 text-brand-gold hover:text-brand-gold-light"><i class="fa-solid fa-chevron-down mr-2"></i>Mostra tutti i biglietti (' + activeBiglietti.length + ')</button>';
    }

    container.innerHTML = html;
  } catch {
    container.innerHTML = '<p class="text-sm text-brand-error text-center py-4">Errore caricamento biglietti</p>';
  }
}

async function downloadPdf(orderId) {
  try {
    const blob = await API.getOrdinePdf(orderId);
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'biglietti.pdf';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  } catch {
    showToast('Errore nel download del PDF', 'danger');
  }
}

async function refundTicket(ticketId) {
  if (!confirm('Sei sicuro di voler richiedere il rimborso per questo biglietto? L\'operazione non e reversibile.')) return;

  try {
    await API.refundTicket(ticketId);
    showToast('Biglietto rimborsato con successo');
    await loadBiglietti();
    await loadOrdini();
    await loadCredito();
  } catch (error) {
    handleApiError(error);
  }
}

function normalizeCollection(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data && data.$values)) return data.$values;
  if (Array.isArray(data && data.items)) return data.items;
  return [];
}

function formatCurrency(amount) {
  return new Intl.NumberFormat('it-IT', { style: 'currency', currency: 'EUR' }).format(amount);
}

function getStatoBadge(stato) {
  switch (stato) {
    case 'Paid':
      return '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-emerald-500/15 text-emerald-500"><i class="fa-solid fa-check text-[10px]"></i>Pagato</span>';
    case 'Pending':
      return '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-amber-500/15 text-amber-500"><i class="fa-solid fa-clock text-[10px]"></i>In attesa</span>';
    case 'Failed':
      return '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-red-500/15 text-red-500"><i class="fa-solid fa-xmark text-[10px]"></i>Fallito</span>';
    case 'Cancelled':
      return '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-brand-on-surface-variant/15 text-brand-on-surface-variant"><i class="fa-solid fa-ban text-[10px]"></i>Annullato</span>';
    case 'Expired':
      return '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-brand-on-surface-variant/15 text-brand-on-surface-variant"><i class="fa-solid fa-hourglass-end text-[10px]"></i>Scaduto</span>';
    case 'Refunded':
      return '<span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-amber-500/15 text-amber-500"><i class="fa-solid fa-rotate-left text-[10px]"></i>Rimborsato</span>';
    default:
      return '<span class="text-xs">' + stato + '</span>';
  }
}

function getMovimentoLabel(tipo) {
  switch (tipo) {
    case 'TopUp': return 'Ricarica';
    case 'DebitOrder': return 'Acquisto';
    case 'Refund': return 'Rimborso';
    case 'Adjustment': return 'Rettifica';
    default: return tipo;
  }
}

async function loadSecurity() {
  const container = document.getElementById('security-content');
  try {
    const security = await API.getAccountSecurity();
    const providers = security.connectedProviders || [];

    var html = '';

    if (security.hasLocalPassword) {
      html += '<div class="mb-4"><div class="flex items-center gap-2 mb-1"><i class="fa-solid fa-lock text-emerald-500"></i><span class="text-sm font-medium text-brand-on-surface">Password locale attiva</span></div>' + (security.passwordChangedAtUtc ? '<p class="text-xs text-brand-on-surface-variant">Ultimo cambio: ' + new Date(security.passwordChangedAtUtc).toLocaleDateString('it-IT', { day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' }) + '</p>' : '') + '</div>' +
        '<form id="password-form" class="space-y-3 border-t border-brand-outline-variant/20 pt-4">' +
          '<div><label for="current-password" class="block text-sm font-medium text-brand-on-surface mb-1">Password attuale</label><input type="password" id="current-password" class="ghost-input w-full px-4 py-2" required></div>' +
          '<div><label for="new-password" class="block text-sm font-medium text-brand-on-surface mb-1">Nuova password</label><input type="password" id="new-password" class="ghost-input w-full px-4 py-2" required minlength="8" pattern="^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d).+$" placeholder="Min. 8 caratteri, una maiuscola, una minuscola, un numero"></div>' +
          '<div><label for="confirm-password" class="block text-sm font-medium text-brand-on-surface mb-1">Conferma password</label><input type="password" id="confirm-password" class="ghost-input w-full px-4 py-2" required></div>' +
          '<div id="password-error" class="text-sm text-red-500 hidden"></div>' +
          '<div id="password-success" class="text-sm text-emerald-500 hidden"></div>' +
          '<button type="submit" id="password-btn" class="btn-gold px-6 py-2 text-sm"><span id="password-btn-text"><i class="fa-solid fa-key mr-2"></i>Cambia Password</span><span id="password-btn-loader" class="hidden"><i class="fa-solid fa-spinner fa-spin"></i></span></button>' +
        '</form>';
    } else {
      html += '<div class="mb-4"><div class="flex items-center gap-2 mb-1"><i class="fa-solid fa-triangle-exclamation text-amber-500"></i><span class="text-sm font-medium text-brand-on-surface">Nessuna password locale</span></div><p class="text-xs text-brand-on-surface-variant">Hai effettuato l\'accesso con un provider social. Imposta una password per accedere anche con le credenziali locali.</p></div>' +
        '<button onclick="requestSetPassword()" id="set-password-btn" class="btn-gold px-6 py-2 text-sm"><span id="set-password-btn-text"><i class="fa-solid fa-paper-plane mr-2"></i>Imposta Password</span><span id="set-password-btn-loader" class="hidden"><i class="fa-solid fa-spinner fa-spin"></i></span></button>' +
        '<p id="set-password-message" class="text-sm text-emerald-500 mt-2 hidden"></p>';
    }

    if (providers.length > 0) {
      html += '<div class="border-t border-brand-outline-variant/20 pt-4 mt-4"><p class="text-xs font-semibold text-brand-on-surface-variant uppercase tracking-wider mb-2">Provider collegati</p><div class="flex flex-wrap gap-2">' +
        providers.map(function(p) {
          const icon = p.toLowerCase() === 'google' ? 'fa-google' : 'fa-microsoft';
          return '<span class="inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-medium bg-brand-surface-container-high text-brand-on-surface"><i class="fa-brands ' + icon + '"></i>' + p + '</span>';
        }).join('') +
      '</div></div>';
    }

    if (security.lastLoginAtUtc) {
      html += '<p class="text-xs text-brand-on-surface-variant mt-3">Ultimo accesso: ' + new Date(security.lastLoginAtUtc).toLocaleDateString('it-IT', { day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' }) + (security.lastLoginProvider ? ' via ' + security.lastLoginProvider : '') + '</p>';
    }

    container.innerHTML = html;

    if (security.hasLocalPassword) {
      setupPasswordForm();
    }

  } catch (err) {
    console.error('loadSecurity error:', err);
    const msg = err && err.message ? err.message : 'Errore sconosciuto';
    container.innerHTML = '<p class="text-sm text-brand-error">Errore sicurezza: ' + msg + '</p>';
  }
}

function setupPasswordForm() {
  const form = document.getElementById('password-form');
  if (!form) return;

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    const currentPw = document.getElementById('current-password').value;
    const newPw = document.getElementById('new-password').value;
    const confirmPw = document.getElementById('confirm-password').value;
    const errorEl = document.getElementById('password-error');
    const successEl = document.getElementById('password-success');
    const btnText = document.getElementById('password-btn-text');
    const btnLoader = document.getElementById('password-btn-loader');
    const btn = document.getElementById('password-btn');

    errorEl.classList.add('hidden');
    successEl.classList.add('hidden');

    if (newPw !== confirmPw) {
      errorEl.textContent = 'Le password non corrispondono';
      errorEl.classList.remove('hidden');
      return;
    }

    btn.disabled = true;
    btnText.classList.add('hidden');
    btnLoader.classList.remove('hidden');

    try {
      await API.changePassword({ currentPassword: currentPw, newPassword: newPw });
      form.reset();
      successEl.textContent = 'Password aggiornata con successo. Dovrai effettuare nuovamente il login.';
      successEl.classList.remove('hidden');
      setTimeout(async () => {
        await Auth.logout();
        window.location.href = '/login.html';
      }, 2000);
    } catch (err) {
      errorEl.textContent = err.message || 'Errore durante il cambio password';
      errorEl.classList.remove('hidden');
    } finally {
      btn.disabled = false;
      btnText.classList.remove('hidden');
      btnLoader.classList.add('hidden');
    }
  });
}

async function requestSetPassword() {
  const btn = document.getElementById('set-password-btn');
  const btnText = document.getElementById('set-password-btn-text');
  const btnLoader = document.getElementById('set-password-btn-loader');
  const msgEl = document.getElementById('set-password-message');

  btn.disabled = true;
  btnText.classList.add('hidden');
  btnLoader.classList.remove('hidden');

  try {
    await API.requestSetPassword();
    msgEl.textContent = 'Ti abbiamo inviato una email per impostare la password.';
    msgEl.classList.remove('hidden');
  } catch (err) {
    showToast(err.message || 'Errore nella richiesta', 'danger');
  } finally {
    btn.disabled = false;
    btnText.classList.remove('hidden');
    btnLoader.classList.add('hidden');
  }
}

async function loadNotifiche() {
  console.log('[NOTIFICHE] Caricamento iniziato');
  const container = document.getElementById('notifiche-content');
  const badge = document.getElementById('notifiche-badge');
  console.log('[NOTIFICHE] Container trovato:', !!container);
  try {
    const notifiche = await API.getNotifiche();
    console.log('[NOTIFICHE] Ricevute:', notifiche?.length || 0, 'notifiche');
    if (!notifiche || !notifiche.length) {
      container.innerHTML = '<div class="text-center py-4 text-brand-on-surface-variant"><i class="fa-regular fa-bell text-3xl mb-2 opacity-40"></i><p class="text-sm">Nessuna notifica</p></div>';
      if (badge) badge.classList.add('hidden');
      return;
    }

    if (badge) {
      badge.textContent = notifiche.length;
      badge.classList.remove('hidden');
    }

    var html = '<div class="space-y-3">';
    notifiche.forEach(function(n) {
      if (n.tipo === 'valutazione_film') {
        html += renderValutazioneCard(n);
      } else {
        var priorityColor = n.priorita === 'alta' ? 'border-l-red-500 bg-red-500/5' : 'border-l-brand-gold bg-brand-surface-container-high/50';
        var icon = n.tipo === 'email_verifica' ? 'fa-envelope' : n.tipo === 'validazione_biglietto' ? 'fa-qrcode' : 'fa-bell';
        var iconColor = n.priorita === 'alta' ? 'text-red-400' : 'text-brand-gold';
        html += '<div class="border-l-4 ' + priorityColor + ' rounded-lg p-4">' +
          '<div class="flex items-start gap-3">' +
            '<i class="fa-solid ' + icon + ' ' + iconColor + ' mt-0.5"></i>' +
            '<div class="flex-1 min-w-0">' +
              '<p class="text-sm text-brand-on-surface">' + n.messaggio + '</p>' +
              (n.azioneUrl && n.azioneLabel ? '<a href="' + n.azioneUrl + '" class="inline-block mt-2 text-sm font-medium text-brand-gold hover:text-brand-gold-light">' + n.azioneLabel + ' <i class="fa-solid fa-arrow-right ml-1"></i></a>' : '') +
            '</div>' +
          '</div>' +
        '</div>';
      }
    });
    html += '</div>';
    container.innerHTML = html;
  } catch {
    container.innerHTML = '<p class="text-sm text-brand-error text-center py-4">Errore caricamento notifiche</p>';
  }
}

function renderValutazioneCard(n) {
  var coverHtml = n.filmCopertina
    ? '<img src="' + n.filmCopertina + '" alt="' + n.filmTitolo + '" class="w-12 h-16 rounded object-cover" loading="lazy">'
    : '<div class="w-12 h-16 rounded bg-brand-surface-container flex items-center justify-center text-brand-on-surface-variant"><i class="fa-solid fa-film"></i></div>';

  return '<div class="border-l-4 border-l-amber-500 bg-amber-500/5 rounded-lg p-4">' +
    '<div class="flex items-start gap-3">' +
      coverHtml +
      '<div class="flex-1 min-w-0">' +
        '<p class="text-sm text-brand-on-surface mb-2">' + n.messaggio + '</p>' +
        '<div class="flex items-center gap-1 star-rating" data-film-id="' + n.filmId + '">' +
          renderStars(n.filmId, 0) +
        '</div>' +
        '<p class="text-xs text-emerald-500 mt-2 hidden valutazione-confirm" id="confirm-' + n.filmId + '"><i class="fa-solid fa-check mr-1"></i>Valutazione salvata!</p>' +
      '</div>' +
    '</div>' +
  '</div>';
}

function renderStars(filmId, currentRating) {
  var html = '';
  for (var i = 1; i <= 5; i++) {
    html += '<button onclick="submitRating(' + filmId + ', ' + i + ')" class="star-btn text-xl ' + (i <= currentRating ? 'text-amber-400' : 'text-brand-on-surface-variant/30') + ' hover:text-amber-400 transition-colors" title="' + i + ' stelle"><i class="fa-solid fa-star"></i></button>';
  }
  return html;
}

async function submitRating(filmId, rating) {
  try {
    var result = await API.valutaFilm({ filmId: filmId, rating: rating });
    var starsContainer = document.querySelector('.star-rating[data-film-id="' + filmId + '"]');
    if (starsContainer) {
      starsContainer.innerHTML = renderStars(filmId, rating);
    }
    var confirmEl = document.getElementById('confirm-' + filmId);
    if (confirmEl) confirmEl.classList.remove('hidden');
    showToast(result.messaggio || 'Valutazione salvata!', 'success');
  } catch (error) {
    handleApiError(error);
  }
}

function setupPrivacyActions() {
  var exportBtn = document.getElementById('btn-export-data');
  var deleteBtn = document.getElementById('btn-delete-account');
  var resultEl = document.getElementById('privacy-action-result');

  if (exportBtn) {
    exportBtn.addEventListener('click', async function() {
      exportBtn.disabled = true;
      exportBtn.innerHTML = '<i class="fa-solid fa-spinner fa-spin mr-2"></i>Esportazione...';
      try {
        var response = await fetch(API_BASE_URL + '/profilo/me/export', {
          headers: { 'Authorization': 'Bearer ' + Auth.getAccessToken() }
        });
        if (!response.ok) throw new Error('Errore durante l\'esportazione');
        var data = await response.json();
        var blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
        var url = URL.createObjectURL(blob);
        var a = document.createElement('a');
        a.href = url;
        a.download = 'redcurtain-export-dati-' + new Date().toISOString().slice(0, 10) + '.json';
        a.click();
        URL.revokeObjectURL(url);
        showToast('Dati esportati con successo', 'success');
      } catch (error) {
        if (resultEl) {
          resultEl.textContent = 'Errore durante l\'esportazione: ' + error.message;
          resultEl.className = 'mt-4 text-sm text-brand-error';
          resultEl.classList.remove('hidden');
        }
      } finally {
        exportBtn.disabled = false;
        exportBtn.innerHTML = '<i class="fa-solid fa-download mr-2"></i>Esporta i miei dati (JSON)';
      }
    });
  }

  if (deleteBtn) {
    deleteBtn.addEventListener('click', async function() {
      if (!confirm('Sei sicuro di voler eliminare il tuo account? Questa azione e irreversibile e tutti i tuoi dati personali verranno rimossi permanentemente. Premi OK per confermare.')) {
        return;
      }
      if (!confirm('ULTIMA CONFERMA: tutti i tuoi biglietti, ordini e dati saranno cancellati. Procedere?')) {
        return;
      }
      deleteBtn.disabled = true;
      deleteBtn.innerHTML = '<i class="fa-solid fa-spinner fa-spin mr-2"></i>Eliminazione...';
      try {
        var response = await fetch(API_BASE_URL + '/profilo/me', {
          method: 'DELETE',
          headers: { 'Authorization': 'Bearer ' + Auth.getAccessToken() }
        });
        if (!response.ok) throw new Error('Errore durante l\'eliminazione');
        Auth.clearAuth();
        showToast('Account eliminato con successo', 'success');
        setTimeout(function() {
          window.location.href = '/index.html';
        }, 2000);
      } catch (error) {
        if (resultEl) {
          resultEl.textContent = 'Errore durante l\'eliminazione: ' + error.message;
          resultEl.className = 'mt-4 text-sm text-brand-error';
          resultEl.classList.remove('hidden');
        }
        deleteBtn.disabled = false;
        deleteBtn.innerHTML = '<i class="fa-solid fa-trash-can mr-2"></i>Elimina Account';
      }
    });
  }
}

window.submitRating = submitRating;
