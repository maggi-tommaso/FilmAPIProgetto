const DEFAULT_API_BASE_URL = "http://localhost:5072";
const AUTH_TOKEN_KEY = "salaluce.authToken";
const AUTH_USER_KEY = "salaluce.authUser";

function getApiBaseUrl() {
  const fromStorage = localStorage.getItem("salaluce.apiBaseUrl");
  return (fromStorage || DEFAULT_API_BASE_URL).replace(/\/$/, "");
}

async function request(path, options = {}) {
  const token = localStorage.getItem(AUTH_TOKEN_KEY);
  const response = await fetch(`${getApiBaseUrl()}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(options.headers || {})
    },
    ...options
  });

  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || `Errore API ${response.status}`);
  }

  if (response.status === 204) {
    return null;
  }

  return response.json();
}

function saveAuth(payload) {
  localStorage.setItem(AUTH_TOKEN_KEY, payload.token);
  localStorage.setItem(
    AUTH_USER_KEY,
    JSON.stringify({
      username: payload.username,
      nome: payload.nome,
      cognome: payload.cognome,
      email: payload.email,
      provider: payload.provider,
      emailConfermata: payload.emailConfermata,
      expiresAtUtc: payload.expiresAtUtc
    })
  );
}

function clearAuth() {
  localStorage.removeItem(AUTH_TOKEN_KEY);
  localStorage.removeItem(AUTH_USER_KEY);
}

function getStoredUser() {
  const raw = localStorage.getItem(AUTH_USER_KEY);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw);
  } catch {
    return null;
  }
}

export const api = {
  getApiBaseUrl,
  getAuthToken: () => localStorage.getItem(AUTH_TOKEN_KEY),
  getStoredUser,
  clearAuth,
  getList: (resource) => request(`/${resource}/`),
  getById: (resource, id) => request(`/${resource}/${id}`),
  create: (resource, payload) =>
    request(`/${resource}/`, {
      method: "POST",
      body: JSON.stringify(payload)
    }),
  update: (resource, id, payload) =>
    request(`/${resource}/${id}`, {
      method: "PUT",
      body: JSON.stringify(payload)
    }),
  remove: (resource, id) =>
    request(`/${resource}/${id}`, {
      method: "DELETE"
    }),
  login: async (identifier, password) => {
    const payload = await request("/auth/login", {
      method: "POST",
      body: JSON.stringify({ identifier, password })
    });
    saveAuth(payload);
    return payload;
  },
  register: async ({ username, nome, cognome, email, password }) => {
    const payload = await request("/auth/register", {
      method: "POST",
      body: JSON.stringify({ username, nome, cognome, email, password })
    });
    saveAuth(payload);
    return payload;
  },
  getGoogleConfig: () => request("/auth/google/config"),
  loginGoogle: async (idToken) => {
    const payload = await request("/auth/login/google", {
      method: "POST",
      body: JSON.stringify({ idToken })
    });
    saveAuth(payload);
    return payload;
  },
  logout: async () => {
    try {
      await request("/auth/logout", { method: "POST" });
    } finally {
      clearAuth();
    }
  },
  getMe: () => request("/auth/me")
};
