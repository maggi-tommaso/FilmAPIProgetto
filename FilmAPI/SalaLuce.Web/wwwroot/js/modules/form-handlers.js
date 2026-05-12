import { showToast } from "./utils.js";

export function wireCrudModal({
  triggerSelector,
  modalId,
  formId,
  onSubmit,
  resetValues
}) {
  const modal = document.getElementById(modalId);
  const form = document.getElementById(formId);
  const trigger = document.querySelector(triggerSelector);

  if (!modal || !form || !trigger) {
    return;
  }

  trigger.addEventListener("click", () => {
    if (resetValues) {
      resetValues(form);
    }
    modal.classList.remove("hidden");
  });

  modal.querySelectorAll("[data-close-modal]").forEach((button) => {
    button.addEventListener("click", () => {
      modal.classList.add("hidden");
    });
  });

  form.addEventListener("submit", async (event) => {
    event.preventDefault();

    try {
      await onSubmit(new FormData(form));
      modal.classList.add("hidden");
      showToast("Operazione completata", "success");
    } catch (error) {
      showToast(error.message || "Errore durante il salvataggio", "error");
    }
  });
}

export function wireDeleteConfirmation({ modalId, onConfirm }) {
  const modal = document.getElementById(modalId);
  if (!modal) {
    return;
  }

  let selectedId = null;

  modal.querySelectorAll("[data-close-delete]").forEach((button) => {
    button.addEventListener("click", () => {
      modal.classList.add("hidden");
    });
  });

  const confirmButton = modal.querySelector("[data-confirm-delete]");
  confirmButton?.addEventListener("click", async () => {
    if (selectedId === null) {
      return;
    }

    try {
      await onConfirm(selectedId);
      modal.classList.add("hidden");
      showToast("Elemento eliminato", "success");
    } catch (error) {
      showToast(error.message || "Errore in eliminazione", "error");
    }
  });

  return {
    open(id) {
      selectedId = id;
      modal.classList.remove("hidden");
    }
  };
}

export function createCrudHelpers(resource) {
  return {
    create: (payload) => api.create(resource, payload),
    update: (id, payload) => api.update(resource, id, payload),
    remove: (id) => api.remove(resource, id)
  };
}
