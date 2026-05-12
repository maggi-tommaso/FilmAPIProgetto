import { api } from "../modules/api.js";
import { loadLayout } from "../modules/template-loader.js";
import { initNavbar } from "../modules/navbar.js";
import { formatDate, renderState } from "../modules/utils.js";

async function loadHome() {
  const statsRoot = document.getElementById("home-stats");
  const proiezioniRoot = document.getElementById("home-proiezioni");

  renderState(proiezioniRoot, "loading", "Caricamento proiezioni...");

  try {
    const [films, registi, cinemas, proiezioni] = await Promise.all([
      api.getList("films"),
      api.getList("registi"),
      api.getList("cinemas"),
      api.getList("proiezioni")
    ]);

    const values = [films.length, registi.length, cinemas.length, proiezioni.length];
    statsRoot?.querySelectorAll(".font-display").forEach((cell, index) => {
      cell.textContent = String(values[index] ?? "-");
    });

    if (!proiezioni.length) {
      renderState(proiezioniRoot, "empty", "Nessuna proiezione disponibile.");
      return;
    }

    proiezioniRoot.innerHTML = proiezioni
      .slice(0, 5)
      .map(
        (p) =>
          `<div class="rounded-lg border border-slate-200 bg-slate-50 p-4 text-sm">
            <p class="font-semibold text-slate-900">Proiezione #${p.id}</p>
            <p class="mt-1 text-slate-600">${formatDate(p.data)} - ${String(p.ora || "").slice(0, 5)}</p>
          </div>`
      )
      .join("");
  } catch {
    renderState(proiezioniRoot, "error", "Errore nel caricamento dei dati home.");
  }
}

document.addEventListener("DOMContentLoaded", async () => {
  await loadLayout({
    header: "header-public.html",
    footer: "footer-public.html",
    navKey: "index"
  });

  initNavbar("index");
  await loadHome();
});
