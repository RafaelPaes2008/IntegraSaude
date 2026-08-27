const KEY_ACCESS = "is_access";
const KEY_REFRESH = "is_refresh";
const KEY_ROLES = "is_roles";
const KEY_NOME = "is_nome";

export function store() {
  return localStorage.getItem(KEY_ACCESS) ? localStorage : sessionStorage;
}

export function saveSession({ accessToken, refreshToken, roles, nome }, remember) {
  clearSession();
  const s = remember ? localStorage : sessionStorage;
  s.setItem(KEY_ACCESS, accessToken);
  if (refreshToken) s.setItem(KEY_REFRESH, refreshToken);
  s.setItem(KEY_ROLES, JSON.stringify(roles || []));
  s.setItem(KEY_NOME, nome || "");
}

export function clearSession() {
  [localStorage, sessionStorage].forEach((s) => {
    s.removeItem(KEY_ACCESS);
    s.removeItem(KEY_REFRESH);
    s.removeItem(KEY_ROLES);
    s.removeItem(KEY_NOME);
  });
}

export function getAccess() {
  return localStorage.getItem(KEY_ACCESS) || sessionStorage.getItem(KEY_ACCESS);
}

export function getRefresh() {
  return localStorage.getItem(KEY_REFRESH) || sessionStorage.getItem(KEY_REFRESH);
}

export function getRoles() {
  const raw = localStorage.getItem(KEY_ROLES) || sessionStorage.getItem(KEY_ROLES);
  try { return JSON.parse(raw || "[]"); } catch { return []; }
}

export function getNome() {
  return localStorage.getItem(KEY_NOME) || sessionStorage.getItem(KEY_NOME) || "";
}

export function hasRole(...roles) {
  const mine = getRoles();
  return roles.some((r) => mine.includes(r));
}
