import { api } from "../modules/api.js";
import { loadLayout } from "../modules/template-loader.js";
import { initNavbar } from "../modules/navbar.js";
import { formatDateTime } from "../modules/utils.js";

function setField(field, value) {
  const el = document.querySelector(`[data-field="${field}"]`);
  if (el) {
    el.textContent = value || "Qui è vuoto!";
    if (!value || value === "Qui è vuoto!") {
      el.classList.add("italic", "text-slate-400");
    } else {
      el.classList.remove("italic", "text-slate-400");
    }
  }
}

function renderTicketList(containerSelector, items) {
  const container = document.querySelector(`[data-section="${containerSelector}"]`);
  if (!container) return;

  if (!items || !items.length) {
    container.innerHTML = `<p class="rounded border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-500 italic">Qui è vuoto!</p>`;
    return;
  }

  container.innerHTML = `
    <div class="space-y-2">
      ${items
        .map(
          (ticket) => `
            <article class="rounded-lg border border-slate-200 bg-slate-50 p-3 text-sm">
              <p class="font-semibold text-slate-900">Codice: ${ticket.codice || "-"}</p>
              <p class="text-slate-600">Acquisto: ${formatDateTime(ticket.acquistatoIlUtc)}</p>
              <p class="text-slate-600">Convalida: ${ticket.convalidatoIlUtc ? formatDateTime(ticket.convalidatoIlUtc) : "-"}</p>
            </article>
          `
        )
        .join("")}
    </div>
  `;
}

function renderEmailBadge(emailConfermata) {
  const badge = document.querySelector(`[data-field="email-badge"]`);
  if (!badge) return;

  if (emailConfermata) {
    badge.innerHTML = `<span>&#10003;</span> Email confermata`;
    badge.className = "inline-flex items-center gap-1 rounded-full bg-emerald-100 px-3 py-1 text-xs font-semibold text-emerald-800";
  } else {
    badge.innerHTML = `<span>&#9888;</span> Email non confermata`;
    badge.className = "inline-flex items-center gap-1 rounded-full bg-amber-100 px-3 py-1 text-xs font-semibold text-amber-800";
  }
}

function populateProfile(profile) {
  setField("username", profile.username);
  setField("nome", profile.nome);
  setField("cognome", profile.cognome);
  setField("provider", profile.provider);
  setField("creato-il", profile.creatoIlUtc ? formatDateTime(profile.creatoIlUtc) : "");
  setField("ultimo-accesso", profile.ultimoAccessoUtc ? formatDateTime(profile.ultimoAccessoUtc) : "");

  const emailEl = document.querySelector(`[data-field="email-censurata"]`);
  const toggleBtn = document.getElementById("email-toggle-btn");

  if (emailEl && toggleBtn && profile.emailCensurata) {
    emailEl.textContent = profile.emailCensurata;
    toggleBtn.classList.remove("hidden");

    let visible = false;
    toggleBtn.addEventListener("click", () => {
      visible = !visible;
      emailEl.textContent = visible ? profile.email : profile.emailCensurata;
      toggleBtn.textContent = visible ? "Nascondi" : "Mostra";
    });
  }

  renderEmailBadge(profile.emailConfermata);

  const statoEl = document.querySelector(`[data-field="stato-acquisti"]`);
  if (statoEl) {
    statoEl.textContent = profile.emailConfermata
      ? "La tua email è stata confermata. Puoi acquistare biglietti per le proiezioni."
      : "Devi confermare la tua email prima di poter acquistare biglietti. Controlla la tua casella di posta per il link di conferma.";
    statoEl.classList.remove("italic", "text-slate-400");
  }

  renderTicketList("biglietti-validati", profile.bigliettiConvalidati);
  renderTicketList("biglietti-in-attesa", profile.bigliettiNonConvalidati);
}

function showError(msg) {
  const grid = document.querySelector(".grid.lg\\:grid-cols-2");
  if (grid) {
    grid.innerHTML = `<div class="lg:col-span-2 rounded-lg border border-red-200 bg-red-50 px-4 py-6 text-center text-sm text-red-700">${msg}</div>`;
  }
}

function wireProfiloLogoutButton() {
  const logoutBtn = document.getElementById("profilo-logout-btn");
  if (!logoutBtn || logoutBtn.dataset.wired === "1") return;
  logoutBtn.dataset.wired = "1";

  logoutBtn.addEventListener("click", async () => {
    logoutBtn.disabled = true;
    logoutBtn.textContent = "Uscita in corso...";
    try { await api.logout(); } catch {}
    window.location.href = "/index.html";
  });
}

async function loadProfile() {
  try {
    const profile = await api.getMe();
    populateProfile(profile);
  } catch {
    showError("Utente non autenticato. Effettua prima il login.");
  }
}

document.addEventListener("DOMContentLoaded", async () => {
  wireProfiloLogoutButton();

  await loadLayout({
    header: "header-public.html",
    footer: "footer-public.html",
    navKey: ""
  });

  initNavbar("");
  wireProfiloLogoutButton();
  await loadProfile();
});
