import { createCrudHelpers, wireCrudModal, wireDeleteConfirmation } from "../modules/form-handlers.js";
import { api } from "../modules/api.js";
import { loadLayout } from "../modules/template-loader.js";
import { initNavbar } from "../modules/navbar.js";
import { filterByQuery, formatDate, renderState, safeText, showToast } from "../modules/utils.js";

const crud = createCrudHelpers("proiezioni");

let proiezioni = [];
let films = [];
let cinemas = [];

const refs = {
  content: document.getElementById("proiezioni-content"),
  search: document.getElementById("proiezione-search"),
  cinemaSelect: document.getElementById("proiezione-cinema"),
  filmSelect: document.getElementById("proiezione-film")
};

function toPayload(formData, existingId = 0) {
  const ora = String(formData.get("ora") || "").trim();
  const formattedOra = ora.length === 5 ? `${ora}:00` : ora;

  return {
    id: Number(formData.get("id") || existingId || 0),
    cinemaId: Number(formData.get("cinemaId") || 0),
    filmId: Number(formData.get("filmId") || 0),
    data: formData.get("data"),
    ora: formattedOra
  };
}

function fillSelects() {
  refs.cinemaSelect.innerHTML = cinemas
    .map((cinema) => `<option value="${cinema.id}">${cinema.nome} - ${cinema.citta}</option>`)
    .join("");

  refs.filmSelect.innerHTML = films
    .map((film) => `<option value="${film.id}">${film.titolo}</option>`)
    .join("");
}

function openEditModal(id) {
  const proiezione = proiezioni.find((item) => item.id === id);
  if (!proiezione) {
    return;
  }

  const modal = document.getElementById("proiezione-modal");
  const form = document.getElementById("proiezione-form");
  form.elements.id.value = proiezione.id;
  form.elements.cinemaId.value = String(proiezione.cinemaId);
  form.elements.filmId.value = String(proiezione.filmId);
  form.elements.data.value = proiezione.data;
  form.elements.ora.value = String(proiezione.ora || "").slice(0, 5);
  modal.classList.remove("hidden");
}

function renderRows() {
  const cinemaMap = new Map(cinemas.map((cinema) => [cinema.id, cinema.nome]));
  const filmMap = new Map(films.map((film) => [film.id, film.titolo]));

  const rows = proiezioni.map((item) => ({
    ...item,
    cinemaNome: cinemaMap.get(item.cinemaId) || "-",
    filmTitolo: filmMap.get(item.filmId) || "-",
    dataLabel: formatDate(item.data),
    oraLabel: String(item.ora || "").slice(0, 5)
  }));

  const data = filterByQuery(rows, refs.search.value, ["id", "cinemaNome", "filmTitolo", "data", "oraLabel"]);
  if (!data.length) {
    renderState(refs.content, "empty", "Nessuna proiezione trovata.");
    return;
  }

  refs.content.innerHTML = `
    <div class="overflow-x-auto rounded-xl border border-amber-700/25 bg-white/75">
      <table class="salaluce-table min-w-full text-sm">
        <thead>
          <tr>
            <th class="px-4 py-3 text-left">ID</th>
            <th class="px-4 py-3 text-left">Cinema</th>
            <th class="px-4 py-3 text-left">Film</th>
            <th class="px-4 py-3 text-left">Data</th>
            <th class="px-4 py-3 text-left">Ora</th>
            <th class="px-4 py-3 text-right">Azioni</th>
          </tr>
        </thead>
        <tbody>
          ${data
            .map(
              (item) => `<tr>
                <td class="px-4 py-3">#${safeText(item.id)}</td>
                <td class="px-4 py-3 font-semibold text-slate-900">${safeText(item.cinemaNome)}</td>
                <td class="px-4 py-3">${safeText(item.filmTitolo)}</td>
                <td class="px-4 py-3">${safeText(item.dataLabel)}</td>
                <td class="px-4 py-3">${safeText(item.oraLabel)}</td>
                <td class="px-4 py-3">
                  <div class="flex justify-end gap-2">
                    <button class="rounded border border-slate-500/40 px-3 py-1 text-xs uppercase" data-edit-id="${item.id}">Modifica</button>
                    <button class="rounded border border-red-700/70 px-3 py-1 text-xs uppercase text-red-800" data-delete-id="${item.id}">Elimina</button>
                  </div>
                </td>
              </tr>`
            )
            .join("")}
        </tbody>
      </table>
    </div>
  `;

  refs.content.querySelectorAll("[data-edit-id]").forEach((button) => {
    button.addEventListener("click", () => openEditModal(Number(button.dataset.editId)));
  });

  refs.content.querySelectorAll("[data-delete-id]").forEach((button) => {
    button.addEventListener("click", () => deleteDialog.open(Number(button.dataset.deleteId)));
  });
}

async function reload() {
  renderState(refs.content, "loading", "Caricamento proiezioni...");
  try {
    [proiezioni, films, cinemas] = await Promise.all([
      api.getList("proiezioni"),
      api.getList("films"),
      api.getList("cinemas")
    ]);
    fillSelects();
    renderRows();
  } catch (error) {
    renderState(refs.content, "error", error.message || "Errore nel caricamento proiezioni.");
  }
}

wireCrudModal({
  triggerSelector: "#open-proiezione-modal",
  modalId: "proiezione-modal",
  formId: "proiezione-form",
  resetValues: (form) => {
    form.reset();
    form.elements.id.value = "";
    if (cinemas.length) {
      form.elements.cinemaId.value = String(cinemas[0].id);
    }
    if (films.length) {
      form.elements.filmId.value = String(films[0].id);
    }
  },
  onSubmit: async (formData) => {
    const id = Number(formData.get("id") || 0);
    const payload = toPayload(formData, id);
    if (id > 0) {
      await crud.update(id, payload);
    } else {
      payload.id = 0;
      await crud.create(payload);
    }
    await reload();
  }
});

const deleteDialog = wireDeleteConfirmation({
  modalId: "proiezione-delete-modal",
  onConfirm: async (id) => {
    await crud.remove(id);
    await reload();
  }
});

refs.search.addEventListener("input", renderRows);

document.addEventListener("DOMContentLoaded", async () => {
  await loadLayout({
    header: "header-admin.html",
    footer: "footer-admin.html",
    navKey: "proiezioni"
  });

  initNavbar("proiezioni");
  await reload();
  showToast("Projection Schedule pronto", "info");
});
