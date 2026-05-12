import { api } from "../modules/api.js";
import { loadLayout } from "../modules/template-loader.js";
import { initNavbar } from "../modules/navbar.js";
import { renderState } from "../modules/utils.js";

async function loadDashboard() {
  const root = document.getElementById("dashboard-content");
  renderState(root, "loading", "Caricamento metriche...");

  try {
    const [films, registi, cinemas, proiezioni] = await Promise.all([
      api.getList("films"),
      api.getList("registi"),
      api.getList("cinemas"),
      api.getList("proiezioni")
    ]);

    const cards = [
      ["Film in archivio", films.length],
      ["Registi attivi", registi.length],
      ["Cinema in rete", cinemas.length],
      ["Proiezioni pianificate", proiezioni.length]
    ];

    root.innerHTML = cards
      .map(
        ([label, value]) => `<article class="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
          <p class="text-xs font-semibold uppercase tracking-wider text-slate-500">${label}</p>
          <p class="mt-3 text-4xl font-black text-slate-900">${value}</p>
        </article>`
      )
      .join("");
  } catch {
    renderState(root, "error", "Errore nel caricamento dashboard.");
  }
}

document.addEventListener("DOMContentLoaded", async () => {
  await loadLayout({
    header: "header-admin.html",
    footer: "footer-admin.html",
    navKey: "dashboard"
  });

  initNavbar("dashboard");
  await loadDashboard();
});
