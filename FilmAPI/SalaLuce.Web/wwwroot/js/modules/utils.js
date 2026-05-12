const toastContainerId = "toast-container";

function ensureToastContainer() {
  let container = document.getElementById(toastContainerId);
  if (container) {
    return container;
  }

  container = document.createElement("div");
  container.id = toastContainerId;
  container.className = "fixed right-4 top-4 z-50 flex w-[calc(100%-2rem)] max-w-sm flex-col gap-2";
  document.body.appendChild(container);
  return container;
}

export function showToast(message, type = "info") {
  const container = ensureToastContainer();
  const tone =
    type === "success"
      ? "bg-emerald-700"
      : type === "error"
        ? "bg-red-700"
        : "bg-slate-800";

  const toast = document.createElement("div");
  toast.className = `${tone} fade-in rounded border border-amber-200/20 px-4 py-3 text-sm text-amber-50 shadow-lg`;
  toast.textContent = message;
  container.appendChild(toast);

  window.setTimeout(() => {
    toast.remove();
  }, 3200);
}

export function formatDate(isoDate) {
  if (!isoDate) {
    return "-";
  }

  return new Date(isoDate).toLocaleDateString("it-IT", {
    day: "2-digit",
    month: "short",
    year: "numeric"
  });
}

export function formatDateTime(isoDateTime) {
  if (!isoDateTime) {
    return "-";
  }

  return new Date(isoDateTime).toLocaleString("it-IT", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit"
  });
}

export function safeText(value) {
  return value === null || value === undefined || value === "" ? "-" : String(value);
}

export function renderState(container, state, message = "") {
  if (!container) {
    return;
  }

  const palette = {
    loading: "text-slate-700",
    empty: "text-slate-600",
    error: "text-red-700"
  };

  container.innerHTML = `<div class="rounded-lg border border-amber-700/25 bg-white/70 px-4 py-6 text-center text-sm ${palette[state] || "text-slate-700"}">${message}</div>`;
}

export function filterByQuery(items, query, fields) {
  const normalized = query.trim().toLowerCase();
  if (!normalized) {
    return items;
  }

  return items.filter((item) =>
    fields.some((field) => String(item[field] ?? "").toLowerCase().includes(normalized))
  );
}
