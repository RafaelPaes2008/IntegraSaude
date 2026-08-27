import { getAccess, getRefresh, saveSession, clearSession, getNome, getRoles } from "./auth.js";

const API = "";

async function parse(res) {
  const text = await res.text();
  try { return text ? JSON.parse(text) : null; } catch { return { message: text }; }
}

export async function api(path, options = {}) {
  const headers = { ...(options.headers || {}) };
  const token = getAccess();
  if (token) headers.Authorization = `Bearer ${token}`;
  if (options.body && !(options.body instanceof FormData) && !headers["Content-Type"]) {
    headers["Content-Type"] = "application/json";
  }

  let res;
  try {
    res = await fetch(API + path, { ...options, headers });
  } catch (err) {
    const error = new Error("Falha de rede. Verifique a conexão.");
    error.offline = true;
    throw error;
  }

  if (res.status === 401 && getRefresh() && !path.includes("/api/auth/")) {
    const refreshed = await tryRefresh();
    if (refreshed) return api(path, options);
  }

  const data = await parse(res);
  if (!res.ok) {
    const error = new Error(data?.message || `Erro ${res.status}`);
    error.status = res.status;
    error.data = data;
    throw error;
  }
  return data;
}

async function tryRefresh() {
  const refreshToken = getRefresh();
  if (!refreshToken) return false;
  try {
    const res = await fetch("/api/auth/refresh", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken })
    });
    if (!res.ok) {
      clearSession();
      return false;
    }
    const data = await res.json();
    const remember = Boolean(localStorage.getItem("is_access"));
    saveSession({ accessToken: data.accessToken, refreshToken: data.refreshToken, roles: data.roles, nome: data.nome }, remember);
    return true;
  } catch {
    return false;
  }
}

export { getNome, getRoles, clearSession };
