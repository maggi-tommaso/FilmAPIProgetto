import { api } from "../modules/api.js";
import { loadLayout } from "../modules/template-loader.js";

let googleInitialized = false;

function wireMobileToggle() {
  document.querySelectorAll("[data-nav-toggle]").forEach((toggle) => {
    toggle.addEventListener("click", () => {
      document.querySelectorAll("nav[data-nav-menu-mobile]").forEach((menu) => menu.classList.toggle("hidden"));
    });
  });
}

function renderAuthSlots(user) {
  const slots = [
    document.querySelector("[data-auth-desktop]"),
    document.querySelector("[data-auth-mobile]")
  ];

  slots.forEach((slot) => {
    if (!slot) return;
    if (!user) {
      slot.innerHTML = `<button type="button" class="rounded border border-slate-300 px-3 py-2 text-sm font-semibold text-slate-700" data-login-open>Login</button>`;
    } else {
      slot.innerHTML = `
        <div class="flex flex-wrap items-center justify-end gap-2">
          <a href="/profilo.html" class="flex items-center gap-2 rounded border border-amber-300 bg-amber-50 px-3 py-2 text-sm font-semibold text-amber-900">
            ciao, ${user.username}
          </a>
          <button type="button" class="rounded border border-red-200 bg-red-50 px-3 py-2 text-sm font-semibold text-red-800 hover:bg-red-100" data-auth-logout-fast>Esci</button>
        </div>
      `;
    }
  });

  document.querySelectorAll("[data-login-open]").forEach((btn) => {
    btn.addEventListener("click", () => {
      const modal = document.querySelector("[data-auth-modal]");
      modal?.classList.remove("hidden");
      modal?.classList.add("flex");
    });
  });

  document.querySelectorAll("[data-auth-logout-fast]").forEach((btn) => {
    btn.addEventListener("click", async () => {
      btn.disabled = true;
      try { await api.logout(); } catch {}
      window.location.reload();
    });
  });
}

function wireLoginModal() {
  const modal = document.querySelector("[data-auth-modal]");
  if (!modal) return;

  const closeBtn = modal.querySelector("[data-auth-close]");
  const loginForm = modal.querySelector("[data-login-form]");
  const feedback = modal.querySelector("[data-auth-feedback]");

  const closeModal = () => {
    modal.classList.add("hidden");
    modal.classList.remove("flex");
  };

  closeBtn?.addEventListener("click", closeModal);
  modal.addEventListener("click", (e) => {
    if (e.target === modal) closeModal();
  });

  loginForm?.addEventListener("submit", async (e) => {
    e.preventDefault();
    const fd = new FormData(loginForm);
    try {
      await api.login(
        String(fd.get("identifier") || "").trim(),
        String(fd.get("password") || "")
      );
      window.location.reload();
    } catch (err) {
      if (feedback) feedback.textContent = err.message || "Credenziali non valide.";
    }
  });
}

async function loadGoogleScript() {
  if (window.google?.accounts?.id) return;
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

async function initGoogleButton() {
  const root = document.querySelector("[data-google-button]");
  const modalRoot = document.querySelector("[data-auth-modal] [data-google-button]");
  if ((!root && !modalRoot) || googleInitialized || window.__salaluceGoogleInitDone) return;

  try {
    const cfg = await api.getGoogleConfig();
    if (!cfg.enabled || !cfg.clientId) return;

    await loadGoogleScript();

    const handleGoogleResponse = async (googleResponse) => {
      try {
        await api.loginGoogle(googleResponse.credential);
        window.location.href = "/index.html";
      } catch (error) {
        const fb = document.getElementById("register-feedback");
        if (fb) {
          fb.textContent = error.message || "Errore nel login Google.";
          fb.classList.remove("hidden");
        }
      }
    };

    window.google.accounts.id.initialize({
      client_id: cfg.clientId,
      callback: handleGoogleResponse,
      ux_mode: "popup"
    });

    if (root) {
      window.google.accounts.id.renderButton(root, {
        theme: "outline",
        size: "large",
        text: "signup_with",
        shape: "pill",
        width: 280
      });
    }

    if (modalRoot) {
      window.google.accounts.id.renderButton(modalRoot, {
        theme: "outline",
        size: "large",
        text: "continue_with",
        shape: "pill",
        width: 280
      });
    }

    googleInitialized = true;
    window.__salaluceGoogleInitDone = true;
  } catch {}
}

function initRegisterForm() {
  const form = document.getElementById("register-form");
  const feedback = document.getElementById("register-feedback");

  form?.addEventListener("submit", async (event) => {
    event.preventDefault();
    const formData = new FormData(form);
    const payload = {
      username: String(formData.get("username") || "").trim(),
      nome: String(formData.get("nome") || "").trim(),
      cognome: String(formData.get("cognome") || "").trim(),
      email: String(formData.get("email") || "").trim(),
      password: String(formData.get("password") || "")
    };

    try {
      await api.register(payload);
      feedback?.classList.remove("hidden");
      feedback.textContent = "Registrazione completata! Ti abbiamo inviato una email di conferma.";
      feedback.classList.remove("text-red-600");
      feedback.classList.add("text-emerald-700");
      const btn = form.querySelector("button[type=submit]");
      if (btn) {
        btn.textContent = "Reindirizzamento...";
        btn.disabled = true;
      }
      setTimeout(() => {
        window.location.href = "/index.html";
      }, 2000);
    } catch (error) {
      feedback?.classList.remove("hidden");
      feedback.classList.add("text-red-600");
      feedback.classList.remove("text-emerald-700");
      feedback.textContent = error.message || "Errore durante la registrazione.";
    }
  });
}

document.addEventListener("DOMContentLoaded", async () => {
  await loadLayout({
    header: "header-public.html",
    footer: "footer-public.html",
    navKey: ""
  });

  wireMobileToggle();
  renderAuthSlots(api.getStoredUser());
  wireLoginModal();
  initRegisterForm();
  initGoogleButton();

  document.getElementById("go-login-btn")?.addEventListener("click", () => {
    const modal = document.querySelector("[data-auth-modal]");
    if (modal) {
      modal.classList.remove("hidden");
      modal.classList.add("flex");
    }
  });
});
