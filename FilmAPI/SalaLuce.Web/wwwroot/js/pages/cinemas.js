import { createCrudHelpers, wireCrudModal, wireDeleteConfirmation } from "../modules/form-handlers.js";
import { api } from "../modules/api.js";
import { loadLayout } from "../modules/template-loader.js";
import { initNavbar } from "../modules/navbar.js";
import { filterByQuery, renderState, safeText, showToast } from "../modules/utils.js";

const crud = createCrudHelpers("cinemas");
let cinemas = [];

const refs = {
  content: document.getElementById("cinemas-content"),
  search: document.getElementById("cinema-search")
};

function toPayload(formData, existingId = 0) {
  return {
    id: Number(formData.get("id") || existingId || 0),
    nome: String(formData.get("nome") || "").trim(),
    indirizzo: String(formData.get("indirizzo") || "").trim(),
    citta: String(formData.get("citta") || "").trim()
  };
}

function openEditModal(id) {
  const cinema = cinemas.find((item) => item.id === id);
  if (!cinema) {
    return;
  }

  const modal = document.getElementById("cinema-modal");
  const form = document.getElementById("cinema-form");
  form.elements.id.value = cinema.id;
  form.elements.nome.value = cinema.nome;
  form.elements.indirizzo.value = cinema.indirizzo;
  form.elements.citta.value = cinema.citta;
  modal.classList.remove("hidden");
}

function renderRows() {
  const data = filterByQuery(cinemas, refs.search.value, ["nome", "indirizzo", "citta"]);
  if (!data.length) {
    renderState(refs.content, "empty", "Nessun cinema trovato.");
    return;
  }

  refs.content.innerHTML = `
    <div class="overflow-x-auto rounded-xl border border-amber-700/25 bg-white/75">
      <table class="salaluce-table min-w-full text-sm">
        <thead>
          <tr>
            <th class="px-4 py-3 text-left">Nome</th>
            <th class="px-4 py-3 text-left">Indirizzo</th>
            <th class="px-4 py-3 text-left">Citta</th>
            <th class="px-4 py-3 text-right">Azioni</th>
          </tr>
        </thead>
        <tbody>
          ${data
            .map(
              (cinema) => `<tr>
                <td class="px-4 py-3 font-semibold text-slate-900">${safeText(cinema.nome)}</td>
                <td class="px-4 py-3">${safeText(cinema.indirizzo)}</td>
                <td class="px-4 py-3">${safeText(cinema.citta)}</td>
                <td class="px-4 py-3">
                  <div class="flex justify-end gap-2">
                    <button class="rounded border border-slate-500/40 px-3 py-1 text-xs uppercase" data-edit-id="${cinema.id}">Modifica</button>
                    <button class="rounded border border-red-700/70 px-3 py-1 text-xs uppercase text-red-800" data-delete-id="${cinema.id}">Elimina</button>
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
  renderState(refs.content, "loading", "Caricamento cinema...");
  try {
    cinemas = await api.getList("cinemas");
    renderRows();
  } catch (error) {
    renderState(refs.content, "error", error.message || "Errore nel caricamento cinema.");
  }
}

wireCrudModal({
  triggerSelector: "#open-cinema-modal",
  modalId: "cinema-modal",
  formId: "cinema-form",
  resetValues: (form) => {
    form.reset();
    form.elements.id.value = "";
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
  modalId: "cinema-delete-modal",
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
    navKey: "cinemas"
  });

  initNavbar("cinemas");
  await reload();
  showToast("Rete Cinema pronta", "info");
});
