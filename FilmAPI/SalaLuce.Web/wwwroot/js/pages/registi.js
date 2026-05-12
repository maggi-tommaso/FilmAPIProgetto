import { createCrudHelpers, wireCrudModal, wireDeleteConfirmation } from "../modules/form-handlers.js";
import { api } from "../modules/api.js";
import { loadLayout } from "../modules/template-loader.js";
import { initNavbar } from "../modules/navbar.js";
import { filterByQuery, renderState, safeText, showToast } from "../modules/utils.js";

const crud = createCrudHelpers("registi");
let registi = [];

const refs = {
  content: document.getElementById("registi-content"),
  search: document.getElementById("regista-search")
};

function toPayload(formData, existingId = 0) {
  return {
    id: Number(formData.get("id") || existingId || 0),
    nome: String(formData.get("nome") || "").trim(),
    cognome: String(formData.get("cognome") || "").trim(),
    nazionalita: String(formData.get("nazionalita") || "").trim()
  };
}

function openEditModal(id) {
  const regista = registi.find((item) => item.id === id);
  if (!regista) {
    return;
  }

  const modal = document.getElementById("regista-modal");
  const form = document.getElementById("regista-form");
  form.elements.id.value = regista.id;
  form.elements.nome.value = regista.nome;
  form.elements.cognome.value = regista.cognome;
  form.elements.nazionalita.value = regista.nazionalita;
  modal.classList.remove("hidden");
}

function renderRows() {
  const data = filterByQuery(registi, refs.search.value, ["nome", "cognome", "nazionalita"]);
  if (!data.length) {
    renderState(refs.content, "empty", "Nessun regista trovato.");
    return;
  }

  refs.content.innerHTML = `
    <div class="grid gap-3 md:grid-cols-2 lg:grid-cols-3">
      ${data
        .map(
          (regista) => `<article class="rounded-xl border border-amber-700/20 bg-white/75 p-4">
            <p class="font-display text-2xl text-slate-900">${safeText(regista.nome)} ${safeText(regista.cognome)}</p>
            <p class="mt-1 text-sm uppercase tracking-wide text-slate-700">${safeText(regista.nazionalita)}</p>
            <div class="mt-4 flex gap-2">
              <button class="rounded border border-slate-500/40 px-3 py-1 text-xs uppercase" data-edit-id="${regista.id}">Modifica</button>
              <button class="rounded border border-red-700/70 px-3 py-1 text-xs uppercase text-red-800" data-delete-id="${regista.id}">Elimina</button>
            </div>
          </article>`
        )
        .join("")}
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
  renderState(refs.content, "loading", "Caricamento registi...");
  try {
    registi = await api.getList("registi");
    renderRows();
  } catch (error) {
    renderState(refs.content, "error", error.message || "Errore nel caricamento registi.");
  }
}

wireCrudModal({
  triggerSelector: "#open-regista-modal",
  modalId: "regista-modal",
  formId: "regista-form",
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
  modalId: "regista-delete-modal",
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
    navKey: "registi"
  });

  initNavbar("registi");
  await reload();
  showToast("Directors Circle pronto", "info");
});
