import { saveSession } from "./auth.js";
import { api } from "./api.js";

document.getElementById("govbr-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  const errorEl = document.getElementById("govbr-error");
  errorEl.hidden = true;
  const cpf = document.getElementById("cpf").value;
  try {
    const data = await api("/api/auth/govbr", {
      method: "POST",
      body: JSON.stringify({ cpf })
    });
    saveSession(data, true);
    location.href = "/app.html";
  } catch (err) {
    errorEl.textContent = err.message;
    errorEl.hidden = false;
  }
});
