import { saveSession, getAccess } from "./auth.js";
import { api } from "./api.js";

if (getAccess()) location.href = "/app.html";

const form = document.getElementById("login-form");
const errorEl = document.getElementById("login-error");
const toggle = document.getElementById("toggle-senha");
const senha = document.getElementById("senha");

toggle.addEventListener("click", () => {
  senha.type = senha.type === "password" ? "text" : "password";
});

form.addEventListener("submit", async (e) => {
  e.preventDefault();
  errorEl.hidden = true;
  const usuario = document.getElementById("usuario").value.trim();
  const senhaVal = senha.value;
  const lembrarMe = document.getElementById("lembrar").checked;
  if (!usuario || !senhaVal) {
    errorEl.textContent = "Preencha usuário e senha.";
    errorEl.hidden = false;
    return;
  }
  const btn = document.getElementById("btn-entrar");
  btn.disabled = true;
  try {
    const data = await api("/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ usuario, senha: senhaVal, lembrarMe })
    });
    saveSession(data, lembrarMe);
    location.href = "/app.html";
  } catch (err) {
    errorEl.textContent = err.message;
    errorEl.hidden = false;
  } finally {
    btn.disabled = false;
  }
});
