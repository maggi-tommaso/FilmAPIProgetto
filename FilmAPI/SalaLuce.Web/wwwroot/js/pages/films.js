import { api } from "../modules/api.js";
import { createCrudHelpers, wireCrudModal, wireDeleteConfirmation } from "../modules/form-handlers.js";
import { loadLayout } from "../modules/template-loader.js";
import { initNavbar } from "../modules/navbar.js";
import { filterByQuery, formatDate, renderState, safeText, showToast } from "../modules/utils.js";

const crud = createCrudHelpers("films");

let films = [];
let registi = [];

const refs = {
  content: document.getElementById("films-content"),
  search: document.getElementById("film-search"),
  registaSelect: document.getElementById("film-regista")
};

function toPayload(formData, existingId = 0) {
  return {
    id: Number(formData.get("id") || existingId || 0),
    titolo: String(formData.get("titolo") || "").trim(),
    dataProduzione: formData.get("dataProduzione"),
    registaId: Number(formData.get("registaId") || 0),
    durata: Number(formData.get("durata") || 0),
    copertinaPath: String(formData.get("copertinaPath") || "").trim() || null,
    filmatoPath: String(formData.get("filmatoPath") || "").trim() || null
  };
}

function fillRegistaSelect() {
  refs.registaSelect.innerHTML = registi
    .map((regista) => `<option value="${regista.id}">${regista.nome} ${regista.cognome}</option>`)
    .join("");
}

function openEditModal(id) {
  const film = films.find((item) => item.id === id);
  if (!film) {
    return;
  }

  const modal = document.getElementById("film-modal");
  const form = document.getElementById("film-form");
  form.elements.id.value = film.id;
  form.elements.titolo.value = film.titolo;
  form.elements.dataProduzione.value = film.dataProduzione;
  form.elements.registaId.value = String(film.registaId);
  form.elements.durata.value = film.durata;
  form.elements.copertinaPath.value = film.copertinaPath || "";
  form.elements.filmatoPath.value = film.filmatoPath || "";
  modal.classList.remove("hidden");
}

function renderRows() {
  const query = refs.search.value;
  const registaMap = new Map(registi.map((regista) => [regista.id, `${regista.nome} ${regista.cognome}`]));

  const data = filterByQuery(
    films.map((film) => ({ ...film, registaNome: registaMap.get(film.registaId) || "-" })),
    query,
    ["titolo", "registaNome"]
  );

  if (!data.length) {
    renderState(refs.content, "empty", "Nessun film trovato.");
    return;
  }

  refs.content.innerHTML = `
    <div class="overflow-x-auto rounded-xl border border-amber-700/25 bg-white/75">
      <table class="salaluce-table min-w-full text-sm">
        <thead>
          <tr>
            <th class="px-4 py-3 text-left">Titolo</th>
            <th class="px-4 py-3 text-left">Regista</th>
            <th class="px-4 py-3 text-left">Produzione</th>
            <th class="px-4 py-3 text-left">Durata</th>
            <th class="px-4 py-3 text-right">Azioni</th>
          </tr>
        </thead>
        <tbody>
          ${data
            .map(
              (film) => `<tr>
                <td class="px-4 py-3 font-semibold text-slate-900">${safeText(film.titolo)}</td>
                <td class="px-4 py-3">${safeText(film.registaNome)}</td>
                <td class="px-4 py-3">${formatDate(film.dataProduzione)}</td>
                <td class="px-4 py-3">${safeText(film.durata)} min</td>
                <td class="px-4 py-3">
                  <div class="flex justify-end gap-2">
                    <button class="rounded border border-slate-500/40 px-3 py-1 text-xs uppercase" data-edit-id="${film.id}">Modifica</button>
                    <button class="rounded border border-red-700/70 px-3 py-1 text-xs uppercase text-red-800" data-delete-id="${film.id}">Elimina</button>
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
  renderState(refs.content, "loading", "Caricamento film...");
  try {
    [films, registi] = await Promise.all([api.getList("films"), api.getList("registi")]);
    fillRegistaSelect();
    renderRows();
  } catch (error) {
    renderState(refs.content, "error", error.message || "Errore nel caricamento film.");
  }
}

wireCrudModal({
  triggerSelector: "#open-film-modal",
  modalId: "film-modal",
  formId: "film-form",
  resetValues: (form) => {
    form.reset();
    form.elements.id.value = "";
    if (registi.length) {
      form.elements.registaId.value = String(registi[0].id);
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
  modalId: "film-delete-modal",
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
    navKey: "films"
  });

  initNavbar("films");
  await reload();
  showToast("Film Vault pronto", "info");
});
