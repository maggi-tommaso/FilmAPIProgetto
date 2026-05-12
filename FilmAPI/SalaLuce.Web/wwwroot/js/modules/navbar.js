import { api } from "./api.js";

let googleInitialized = false;

function markGoogleInitDone() {
  window.__salaluceGoogleInitDone = true;
  googleInitialized = true;
}

function isGoogleInitDone() {
  return googleInitialized || window.__salaluceGoogleInitDone;
}

function setActiveNav(navKey) {
  if (!navKey) {
    return;
  }

  document.querySelectorAll("[data-nav]").forEach((link) => {
    if (link.dataset.nav === navKey) {
      link.classList.add("salaluce-link-active");
    }
  });
}

function wireToggle() {
  const toggles = document.querySelectorAll("[data-nav-toggle]");
  const menus = document.querySelectorAll("nav[data-nav-menu-mobile]");

  toggles.forEach((toggle) => {
    toggle.addEventListener("click", () => {
      menus.forEach((menu) => menu.classList.toggle("hidden"));
    });
  });
}

function wireAdminLogout() {
  document.querySelectorAll("[data-admin-logout]").forEach((btn) => {
    btn.addEventListener("click", async () => {
      btn.disabled = true;
      btn.textContent = "Uscita...";
      try {
        await api.logout();
      } catch {
        // La sessione locale viene comunque rimossa in api.logout (finally).
      }
      window.location.href = "/index.html";
    });
  });
}

function renderAuthSlot(slot, user) {
  if (!slot) {
    return;
  }

  if (!user) {
    slot.innerHTML = `
      <button type="button" class="rounded border border-slate-300 px-3 py-2 text-sm font-semibold text-slate-700" data-login-open>
        Login
      </button>
    `;
    return;
  }

  slot.innerHTML = `
    <div class="flex flex-wrap items-center justify-end gap-2">
      <button type="button" class="flex items-center gap-2 rounded border border-amber-300 bg-amber-50 px-3 py-2 text-sm font-semibold text-amber-900" data-profile-open>
        <span aria-hidden="true">*</span>
        <span>ciao, ${user.username}</span>
      </button>
      <button type="button" class="rounded border border-red-200 bg-red-50 px-3 py-2 text-sm font-semibold text-red-800 hover:bg-red-100" data-auth-logout>
        Esci
      </button>
    </div>
  `;
}

function bindProfileActions(user) {
  document.querySelectorAll("[data-profile-open]").forEach((btn) => {
    btn.addEventListener("click", () => {
      window.location.href = `/profilo.html?u=${encodeURIComponent(user.username)}`;
    });
  });

  document.querySelectorAll("[data-auth-logout]").forEach((btn) => {
    btn.addEventListener("click", async () => {
      btn.disabled = true;
      btn.textContent = "Uscita...";
      try {
        await api.logout();
      } catch {
        // La sessione locale viene comunque rimossa in api.logout (finally).
      }
      window.location.href = "/index.html";
    });
  });
}

function resetAuthModalLoginView(modal) {
  if (!modal) {
    return;
  }
}

function wireRegisterPanelToggle(modal) {
  // La registrazione ora è in una pagina separata; non serve più il toggle.
}

function updateSlots(desktopSlot, mobileSlot, user) {
  renderAuthSlot(desktopSlot, user);
  renderAuthSlot(mobileSlot, user);

  document.querySelectorAll("[data-login-open]").forEach((btn) => {
    btn.addEventListener("click", () => {
      const modal = document.querySelector("[data-auth-modal]");
      resetAuthModalLoginView(modal);
      modal?.classList.remove("hidden");
      modal?.classList.add("flex");
    });
  });

  if (user) {
    bindProfileActions(user);
  }
}

async function loadGoogleScript() {
  if (window.google?.accounts?.id) {
    return;
  }

  await new Promise((resolve, reject) => {
    const existing = document.querySelector("script[data-google-identity]");
    if (existing) {
      existing.addEventListener("load", resolve, { once: true });
      existing.addEventListener("error", reject, { once: true });
      return;
    }

    const script = document.createElement("script");
    script.src = "https://accounts.google.com/gsi/client";
    script.async = true;
    script.defer = true;
    script.dataset.googleIdentity = "1";
    script.addEventListener("load", resolve, { once: true });
    script.addEventListener("error", reject, { once: true });
    document.head.appendChild(script);
  });
}

async function initGoogleButton(modal, feedback, desktopSlot, mobileSlot) {
  const root = modal.querySelector("[data-google-button]");
  const status = modal.querySelector("[data-google-status]");
  if (!root || isGoogleInitDone()) {
    return;
  }

  try {
    const cfg = await api.getGoogleConfig();
    if (!cfg.enabled || !cfg.clientId) {
      if (status) {
        status.textContent = "Google OAuth non configurato sul server.";
      }
      return;
    }

    await loadGoogleScript();

    window.google.accounts.id.initialize({
      client_id: cfg.clientId,
      callback: async (googleResponse) => {
        try {
          const result = await api.loginGoogle(googleResponse.credential);
          updateSlots(desktopSlot, mobileSlot, result);
          if (feedback) {
            feedback.textContent = "Accesso Google completato.";
          }
          modal.classList.add("hidden");
          modal.classList.remove("flex");
        } catch (error) {
          if (feedback) {
            feedback.textContent = error.message || "Errore nel login Google.";
          }
        }
      }
    });

    window.google.accounts.id.renderButton(root, {
      theme: "outline",
      size: "large",
      text: "continue_with",
      shape: "pill",
      width: 280
    });

    googleInitialized = true;
    markGoogleInitDone();
    if (status) {
      status.textContent = "Usa il pulsante Google per autenticarti.";
    }
  } catch {
    if (status) {
      status.textContent = "Impossibile caricare Google OAuth.";
    }
  }
}

function wireAuthUi() {
  const modal = document.querySelector("[data-auth-modal]");
  if (!modal) {
    return;
  }

  const desktopSlot = document.querySelector("[data-auth-desktop]");
  const mobileSlot = document.querySelector("[data-auth-mobile]");
  const closeBtn = modal.querySelector("[data-auth-close]");
  const loginForm = modal.querySelector("[data-login-form]");
  const feedback = modal.querySelector("[data-auth-feedback]");

  updateSlots(desktopSlot, mobileSlot, api.getStoredUser());
  wireRegisterPanelToggle(modal);
  initGoogleButton(modal, feedback, desktopSlot, mobileSlot);

  const closeModal = () => {
    resetAuthModalLoginView(modal);
    modal.classList.add("hidden");
    modal.classList.remove("flex");
  };

  closeBtn?.addEventListener("click", closeModal);
  modal.addEventListener("click", (event) => {
    if (event.target === modal) {
      closeModal();
    }
  });

  loginForm?.addEventListener("submit", async (event) => {
    event.preventDefault();
    const formData = new FormData(loginForm);
    const identifier = String(formData.get("identifier") || "").trim();
    const password = String(formData.get("password") || "");

    try {
      const result = await api.login(identifier, password);
      updateSlots(desktopSlot, mobileSlot, result);
      if (feedback) {
        feedback.textContent = "Login eseguito correttamente.";
      }
      closeModal();
    } catch (error) {
      if (feedback) {
        feedback.textContent = error.message || "Credenziali non valide.";
      }
    }
  });
}

export function initNavbar(navKey) {
  setActiveNav(navKey);
  wireToggle();
  wireAdminLogout();
  wireAuthUi();
}
