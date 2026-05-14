(() => {
  const ADMIN_PATHS = new Set([
    '/dashboard.html',
    '/films.html',
    '/registi.html',
    '/cinemas.html',
    '/shows.html',
    '/categorie.html',
    '/sale.html',
    '/ricarica-credito.html',
    '/validazione-biglietti.html',
    '/utenti.html'
  ]);

  const PAGE_TITLES = {
    '/dashboard.html': 'Dashboard',
    '/films.html': 'Film',
    '/registi.html': 'Registi',
    '/cinemas.html': 'Cinema',
    '/shows.html': 'Show',
    '/categorie.html': 'Categorie',
    '/sale.html': 'Sale',
    '/ricarica-credito.html': 'Ricarica Credito',
    '/validazione-biglietti.html': 'Validazione Biglietti',
    '/utenti.html': 'Utenti'
  };

  function getUser() {
    if (typeof Auth === 'undefined' || !Auth || typeof Auth.getUser !== 'function') return null;
    return Auth.getUser();
  }

  function toggleSidebar() {
    const sidebar = document.getElementById('admin-sidebar');
    const backdrop = document.getElementById('admin-sidebar-backdrop');
    if (!sidebar || !backdrop) return;
    sidebar.classList.toggle('-translate-x-full');
    backdrop.classList.toggle('hidden');
  }

  function setActiveLinks() {
    const currentPath = window.location.pathname.toLowerCase();
    document.querySelectorAll('[data-admin-link]').forEach((el) => {
      const href = (el.getAttribute('href') || '').toLowerCase();
      if (href === currentPath) {
        el.classList.add('active');
      } else {
        el.classList.remove('active');
      }
    });
  }

  function updateUserUI() {
    const user = getUser();
    const userNameEl = document.getElementById('admin-user-name');
    const userAvatarEl = document.getElementById('admin-user-avatar');
    const userMenuNameEl = document.getElementById('admin-user-menu-name');

    if (userNameEl) userNameEl.textContent = user?.nome || user?.email || 'Utente';
    if (userMenuNameEl) userMenuNameEl.textContent = user?.email || '';
    if (userAvatarEl) {
      const first = (user?.nome || 'U').charAt(0);
      const second = (user?.cognome || 'N').charAt(0);
      userAvatarEl.textContent = `${first}${second}`.toUpperCase();
    }

  }

  function bindActions() {
    const sidebarToggle = document.getElementById('admin-sidebar-toggle');
    const backdrop = document.getElementById('admin-sidebar-backdrop');
    const userToggle = document.getElementById('admin-user-toggle');
    const userMenu = document.getElementById('admin-user-menu');
    const logoutBtn = document.getElementById('admin-logout-btn');

    if (sidebarToggle) sidebarToggle.addEventListener('click', toggleSidebar);
    if (backdrop) backdrop.addEventListener('click', toggleSidebar);

    if (userToggle && userMenu) {
      userToggle.addEventListener('click', () => {
        userMenu.classList.toggle('hidden');
      });
      document.addEventListener('click', (event) => {
        if (!userToggle.contains(event.target) && !userMenu.contains(event.target)) {
          userMenu.classList.add('hidden');
        }
      });
    }

    const handleLogout = () => {
      if (typeof Auth === 'undefined' || !Auth || typeof Auth.logout !== 'function') {
        window.location.href = '/index.html';
        return;
      }
      Auth.logout().finally(() => {
        window.location.href = '/index.html';
      });
    };

    if (logoutBtn) logoutBtn.addEventListener('click', handleLogout);
  }

  function renderShell(main) {
    const currentPath = window.location.pathname.toLowerCase();
    const pageTitle = PAGE_TITLES[currentPath] || 'Area Admin';

    const shell = document.createElement('div');
    shell.innerHTML = `
      <div id="admin-shell-root">
      <div id="admin-sidebar-backdrop" class="fixed inset-0 bg-black/50 z-40 hidden md:hidden"></div>
      <div class="flex w-full min-h-screen">
        <aside id="admin-sidebar" class="w-64 sidebar-glass flex-shrink-0 flex flex-col fixed md:relative inset-y-0 left-0 z-50 -translate-x-full md:translate-x-0 transition-transform duration-300">
          <div class="p-6">
            <a href="/index.html" class="flex items-center gap-3">
              <img src="/assets/images/logo-icon.png" alt="RedCurtain" class="h-8 w-auto" loading="lazy">
              <span class="text-xl font-bold text-brand-sidebar-text">RedCurtain</span>
            </a>
          </div>
          <nav class="flex-1 px-4 space-y-1">
            <a data-admin-link href="/dashboard.html" class="flex items-center gap-3 px-4 py-3 rounded-xl"><i class="fa-solid fa-gauge-high"></i>Dashboard</a>
            <a data-admin-link href="/films.html" class="flex items-center gap-3 px-4 py-3 rounded-xl"><i class="fa-solid fa-film"></i>Film</a>
            <a data-admin-link href="/registi.html" class="flex items-center gap-3 px-4 py-3 rounded-xl"><i class="fa-solid fa-user"></i>Registi</a>
            <a data-admin-link href="/cinemas.html" class="flex items-center gap-3 px-4 py-3 rounded-xl"><i class="fa-solid fa-building"></i>Cinema</a>
            <a data-admin-link href="/shows.html" class="flex items-center gap-3 px-4 py-3 rounded-xl"><i class="fa-solid fa-clock"></i>Show</a>
            <a data-admin-link href="/categorie.html" class="flex items-center gap-3 px-4 py-3 rounded-xl"><i class="fa-solid fa-tags"></i>Categorie</a>
            <a data-admin-link href="/utenti.html" class="flex items-center gap-3 px-4 py-3 rounded-xl"><i class="fa-solid fa-users"></i>Utenti</a>
            <div class="my-2 border-t border-slate-800/50"></div>
            <a data-admin-link href="/sale.html" class="flex items-center gap-3 px-4 py-3 rounded-xl"><i class="fa-solid fa-chair"></i>Sale</a>
            <a data-admin-link href="/ricarica-credito.html" class="flex items-center gap-3 px-4 py-3 rounded-xl"><i class="fa-solid fa-coins"></i>Ricarica Credito</a>
            <a data-admin-link href="/validazione-biglietti.html" class="flex items-center gap-3 px-4 py-3 rounded-xl"><i class="fa-solid fa-qrcode"></i>Validazione</a>
          </nav>
          <div class="p-4 border-t border-slate-800 space-y-1">
            <button onclick="RedCurtainTheme.toggle()" class="sidebar-theme-toggle w-full px-4 py-3 rounded-xl text-left">
              <i class="fa-solid fa-moon theme-toggle-icon-moon"></i>
              <i class="fa-solid fa-sun theme-toggle-icon-sun hidden"></i>
              <span class="theme-toggle-label">Cambia tema</span>
            </button>
            <a href="#" class="flex items-center gap-3 px-4 py-3 rounded-xl text-brand-sidebar-text hover:text-brand-gold transition-colors">
              <i class="fa-solid fa-gear"></i>
              Settings
            </a>
          </div>
        </aside>

        <div class="flex-1 min-h-screen overflow-x-auto">
          <header class="bg-brand-surface-container-lowest ambient-shadow-sm border-b border-brand-outline-variant/10 sticky top-0 z-30">
            <div class="px-4 sm:px-6 lg:px-8 py-4 flex items-center justify-between gap-4">
              <div class="flex items-center gap-4">
                <button id="admin-sidebar-toggle" class="md:hidden p-2 text-brand-on-surface hover:text-brand-gold" title="Apri menu" aria-label="Apri menu laterale">
                  <i class="fa-solid fa-bars text-xl"></i>
                </button>
                <h1 class="text-xl sm:text-2xl font-bold text-brand-on-surface">${pageTitle}</h1>
              </div>
              <div class="flex items-center gap-3">
                <div class="relative">
                  <button id="admin-user-toggle" class="flex items-center gap-2 text-sm font-medium text-brand-on-surface">
                    <div id="admin-user-avatar" class="w-8 h-8 bg-brand-surface-container-highest rounded-full flex items-center justify-center text-brand-on-surface font-semibold">UN</div>
                    <span id="admin-user-name" class="hidden sm:inline">Utente</span>
                  </button>
                  <div id="admin-user-menu" class="hidden absolute right-0 mt-2 w-56 bg-brand-surface-container rounded-xl shadow-lg border border-brand-outline-variant/10 py-2">
                    <p id="admin-user-menu-name" class="px-4 py-2 text-xs text-brand-on-surface-variant"></p>
                    <a href="/profilo.html" class="block px-4 py-2 text-sm text-brand-on-surface hover:text-brand-gold hover:bg-brand-surface-container-high"><i class="fa-solid fa-user mr-2"></i>Profilo</a>
                    <a href="/profilo.html" class="block px-4 py-2 text-sm text-brand-on-surface hover:text-brand-gold hover:bg-brand-surface-container-high"><i class="fa-solid fa-ticket mr-2"></i>Biglietti</a>
                    <hr class="my-1 border-brand-outline-variant/20">
                    <button id="admin-logout-btn" class="w-full text-left px-4 py-2 text-sm text-red-500 hover:bg-brand-surface-container-high"><i class="fa-solid fa-sign-out-alt mr-2"></i>Logout</button>
                  </div>
                </div>
              </div>
            </div>
          </header>
          <div id="admin-shell-content" class="px-4 sm:px-6 lg:px-8 py-8"></div>
        </div>
      </div>
      </div>
    `;

    document.body.prepend(shell.firstElementChild);
    const target = document.getElementById('admin-shell-content');
    if (!target) return;
    main.className = '';
    target.appendChild(main);
  }

  document.addEventListener('DOMContentLoaded', () => {
    const pathname = window.location.pathname.toLowerCase();
    if (!ADMIN_PATHS.has(pathname)) return;

    const main = document.querySelector('main');
    if (!main) return;

    const navbarContainer = document.getElementById('navbar-container');
    const footerContainer = document.getElementById('footer-container');
    if (navbarContainer) navbarContainer.remove();
    if (footerContainer) footerContainer.remove();

    renderShell(main);
    bindActions();
    setActiveLinks();
    updateUserUI();
  });
})();
