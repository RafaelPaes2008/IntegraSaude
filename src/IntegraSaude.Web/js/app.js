import { api, getNome, getRoles, clearSession } from "./api.js";
import { getAccess, hasRole } from "./auth.js";

if (!getAccess()) location.href = "/";

const view = document.getElementById("view");
const nav = document.getElementById("nav");
document.getElementById("user-name").textContent = getNome() || getRoles().join(", ");
document.getElementById("btn-sair").addEventListener("click", async () => {
  try { await api("/api/auth/logout", { method: "POST", body: JSON.stringify({ refreshToken: localStorage.getItem("is_refresh") || sessionStorage.getItem("is_refresh") }) }); } catch { /* ignore */ }
  clearSession();
  location.href = "/";
});

const offline = document.getElementById("offline");
window.addEventListener("online", () => { offline.hidden = true; });
window.addEventListener("offline", () => { offline.hidden = false; });

const links = [];
if (hasRole("Recepcionista", "Admin")) links.push(["#/recepcao", "Recepção"]);
if (hasRole("Enfermagem", "Admin")) links.push(["#/triagem", "Triagem"]);
if (hasRole("Medico", "Admin")) links.push(["#/medico", "Médico"]);
if (hasRole("Admin")) links.push(["#/admin", "Usuários"]);
nav.innerHTML = links.map(([href, label]) => `<a href="${href}">${label}</a>`).join("");

const statusLabel = ["Aguardando", "Em triagem", "Triado", "Em consulta", "Finalizado"];
const manchester = ["Vermelho", "Laranja", "Amarelo", "Verde", "Azul"];
const mClass = ["m-vermelho", "m-laranja", "m-amarelo", "m-verde", "m-azul"];

function flash(el, msg, ok = true) {
  el.innerHTML = `<div class="flash ${ok ? "ok" : "err"}">${msg}</div>` + el.innerHTML.replace(/<div class="flash[\s\S]*?<\/div>/, "");
}

function defaultRoute() {
  if (hasRole("Recepcionista", "Admin")) return "#/recepcao";
  if (hasRole("Enfermagem")) return "#/triagem";
  if (hasRole("Medico")) return "#/medico";
  return "#/recepcao";
}

async function render() {
  const hash = location.hash || defaultRoute();
  nav.querySelectorAll("a").forEach((a) => a.classList.toggle("active", a.getAttribute("href") === hash));
  if (hash.startsWith("#/recepcao")) return recepcao();
  if (hash.startsWith("#/triagem")) return triagem();
  if (hash.startsWith("#/medico")) return medico();
  if (hash.startsWith("#/admin")) return admin();
  location.hash = defaultRoute();
}

async function recepcao() {
  view.innerHTML = `
    <h1>Recepção</h1>
    <p class="sub">Cadastro simplificado, senha de fila e agenda do dia.</p>
    <div class="grid-2">
      <section class="panel">
        <h2>Novo paciente</h2>
        <form class="stack" id="form-pac">
          <input name="nome" placeholder="Nome completo *" required />
          <input name="cpf" placeholder="CPF *" required />
          <input name="cartaoSus" placeholder="Cartão SUS" />
          <input name="telefone" placeholder="Telefone" />
          <input name="endereco" placeholder="Endereço" />
          <input name="dataNascimento" type="date" />
          <button class="btn-primary" type="submit">Cadastrar</button>
        </form>
      </section>
      <section class="panel">
        <h2>Emitir senha</h2>
        <form class="stack" id="form-senha">
          <select name="pacienteId" id="sel-pac" required></select>
          <button class="btn-primary" type="submit">Gerar senha</button>
        </form>
        <h2 style="margin-top:1rem">Agendar</h2>
        <form class="stack" id="form-ag">
          <select name="pacienteId" id="sel-pac-ag" required></select>
          <input name="dataHora" type="datetime-local" required />
          <input name="observacao" placeholder="Observação" />
          <button class="btn-primary" type="submit">Agendar</button>
        </form>
      </section>
    </div>
    <section class="panel" style="margin-top:1rem">
      <h2>Fila e agenda de hoje</h2>
      <div id="listas"></div>
    </section>`;

  async function loadSelects() {
    const pacientes = await api("/api/pacientes");
    const opts = pacientes.map((p) => `<option value="${p.id}">${p.nome} — ${p.cpf}</option>`).join("");
    document.getElementById("sel-pac").innerHTML = opts;
    document.getElementById("sel-pac-ag").innerHTML = opts;
  }

  async function loadLists() {
    const [fila, agenda] = await Promise.all([
      api("/api/atendimentos"),
      api("/api/agendamentos")
    ]);
    document.getElementById("listas").innerHTML = `
      <h3>Atendimentos</h3>
      <table class="table"><thead><tr><th>Senha</th><th>Paciente</th><th>Status</th></tr></thead>
      <tbody>${fila.map((a) => `<tr><td>${a.senha}</td><td>${a.pacienteNome}</td><td>${statusLabel[a.status]}</td></tr>`).join("")}</tbody></table>
      <h3>Agenda</h3>
      <table class="table"><thead><tr><th>Horário</th><th>Paciente</th><th>Obs.</th></tr></thead>
      <tbody>${agenda.map((a) => `<tr><td>${new Date(a.dataHora).toLocaleString()}</td><td>${a.pacienteNome}</td><td>${a.observacao || ""}</td></tr>`).join("")}</tbody></table>`;
  }

  document.getElementById("form-pac").addEventListener("submit", async (e) => {
    e.preventDefault();
    const f = e.target;
    try {
      await api("/api/pacientes", {
        method: "POST",
        body: JSON.stringify({
          nome: f.nome.value,
          cpf: f.cpf.value,
          cartaoSus: f.cartaoSus.value || null,
          telefone: f.telefone.value || null,
          endereco: f.endereco.value || null,
          dataNascimento: f.dataNascimento.value || null
        })
      });
      f.reset();
      await loadSelects();
      flash(view.querySelector(".panel"), "Paciente cadastrado.");
    } catch (err) { alert(err.message); }
  });

  document.getElementById("form-senha").addEventListener("submit", async (e) => {
    e.preventDefault();
    try {
      const a = await api("/api/atendimentos/senha", {
        method: "POST",
        body: JSON.stringify({ pacienteId: document.getElementById("sel-pac").value })
      });
      alert("Senha emitida: " + a.senha);
      await loadLists();
    } catch (err) { alert(err.message); }
  });

  document.getElementById("form-ag").addEventListener("submit", async (e) => {
    e.preventDefault();
    const f = e.target;
    try {
      await api("/api/agendamentos", {
        method: "POST",
        body: JSON.stringify({
          pacienteId: document.getElementById("sel-pac-ag").value,
          dataHora: new Date(f.dataHora.value).toISOString(),
          observacao: f.observacao.value || null
        })
      });
      f.reset();
      await loadLists();
    } catch (err) { alert(err.message); }
  });

  await loadSelects();
  await loadLists();
}

async function triagem() {
  view.innerHTML = `
    <h1>Triagem</h1>
    <p class="sub">Sinais vitais e classificação de risco (Protocolo de Manchester — V1 didática).</p>
    <div class="grid-2">
      <section class="panel"><h2>Fila de espera</h2><div id="fila"></div></section>
      <section class="panel">
        <h2>Registrar triagem</h2>
        <form class="stack" id="form-tr">
          <select name="atendimentoId" id="sel-at" required></select>
          <input name="pressaoSistolica" type="number" placeholder="PA sistólica" />
          <input name="pressaoDiastolica" type="number" placeholder="PA diastólica" />
          <input name="temperatura" type="number" step="0.1" placeholder="Temperatura °C" />
          <input name="glicemia" type="number" step="0.1" placeholder="Glicemia" />
          <input name="saturacao" type="number" step="0.1" placeholder="Saturação %" />
          <input name="peso" type="number" step="0.1" placeholder="Peso kg" />
          <select name="classificacao">${manchester.map((n, i) => `<option value="${i}">${n}</option>`).join("")}</select>
          <textarea name="justificativa" rows="3" placeholder="Justificativa"></textarea>
          <button class="btn-primary" type="submit">Classificar e enviar à fila médica</button>
        </form>
      </section>
    </div>`;

  async function load() {
    const fila = await api("/api/atendimentos/fila-espera");
    document.getElementById("sel-at").innerHTML = fila.map((a) => `<option value="${a.id}">${a.senha} — ${a.pacienteNome}</option>`).join("");
    document.getElementById("fila").innerHTML = fila.map((a) => `
      <div class="fila-item">
        <div><strong>${a.senha}</strong> ${a.pacienteNome}<br /><small>${statusLabel[a.status]}</small></div>
      </div>`).join("") || "<p>Ninguém aguardando.</p>";
  }

  document.getElementById("form-tr").addEventListener("submit", async (e) => {
    e.preventDefault();
    const f = e.target;
    const num = (name) => f[name].value === "" ? null : Number(f[name].value);
    try {
      await api("/api/triagem", {
        method: "POST",
        body: JSON.stringify({
          atendimentoId: f.atendimentoId.value,
          pressaoSistolica: num("pressaoSistolica"),
          pressaoDiastolica: num("pressaoDiastolica"),
          temperatura: num("temperatura"),
          glicemia: num("glicemia"),
          saturacao: num("saturacao"),
          peso: num("peso"),
          classificacao: Number(f.classificacao.value),
          justificativa: f.justificativa.value || null
        })
      });
      await load();
    } catch (err) { alert(err.message); }
  });

  await load();
  window.__poll = setInterval(load, 8000);
}

async function medico() {
  view.innerHTML = `
    <h1>Atendimento médico</h1>
    <p class="sub">Fila por prioridade Manchester, checklist e encerramento da consulta.</p>
    <div class="grid-2">
      <section class="panel"><h2>Fila</h2><div id="fila"></div></section>
      <section class="panel" id="consulta-box"><p>Selecione um paciente triado.</p></section>
    </div>`;

  async function loadFila() {
    const fila = await api("/api/medico/fila");
    document.getElementById("fila").innerHTML = fila.map((a) => `
      <div class="fila-item ${a.classificacao === 0 ? "alerta" : ""}">
        <div>
          <span class="badge ${mClass[a.classificacao ?? 4]}">${a.classificacaoNome || manchester[a.classificacao] || "-"}</span>
          <strong> ${a.senha}</strong> ${a.pacienteNome}
        </div>
        <button data-id="${a.id}">Atender</button>
      </div>`).join("") || "<p>Fila vazia.</p>";
    document.getElementById("fila").querySelectorAll("button").forEach((b) => {
      b.onclick = () => abrirConsulta(b.dataset.id);
    });
  }

  async function abrirConsulta(id) {
    let consulta;
    try { consulta = await api(`/api/medico/${id}/consulta`); }
    catch { consulta = await api(`/api/medico/${id}/iniciar`, { method: "POST" }); }

    const box = document.getElementById("consulta-box");
    box.innerHTML = `
      <h2>Checklist e prontuário</h2>
      <form class="stack" id="form-c">
        <div class="check-row">
          <label><input type="checkbox" name="queixaPrincipal" ${consulta.queixaPrincipal ? "checked" : ""} /> Queixa principal</label>
          <label><input type="checkbox" name="historiaDoencaAtual" ${consulta.historiaDoencaAtual ? "checked" : ""} /> HDA</label>
          <label><input type="checkbox" name="exameFisico" ${consulta.exameFisico ? "checked" : ""} /> Exame físico</label>
          <label><input type="checkbox" name="orientacoes" ${consulta.orientacoes ? "checked" : ""} /> Orientações</label>
        </div>
        <textarea name="anamnese" rows="4" placeholder="Anamnese">${consulta.anamnese || ""}</textarea>
        <input name="diagnostico" placeholder="Diagnóstico *" value="${consulta.diagnostico || ""}" />
        <input name="cid" placeholder="CID" value="${consulta.cid || ""}" />
        <textarea name="prescricao" rows="3" placeholder="Prescrição">${consulta.prescricao || ""}</textarea>
        <button class="btn-primary" type="submit">Salvar</button>
        <button class="btn-outline" type="button" id="btn-fim">Finalizar consulta</button>
      </form>`;

    const payload = () => {
      const f = document.getElementById("form-c");
      return {
        queixaPrincipal: f.queixaPrincipal.checked,
        historiaDoencaAtual: f.historiaDoencaAtual.checked,
        exameFisico: f.exameFisico.checked,
        orientacoes: f.orientacoes.checked,
        anamnese: f.anamnese.value,
        diagnostico: f.diagnostico.value,
        cid: f.cid.value,
        prescricao: f.prescricao.value
      };
    };

    document.getElementById("form-c").onsubmit = async (e) => {
      e.preventDefault();
      try {
        await api(`/api/medico/${id}/consulta`, { method: "PUT", body: JSON.stringify(payload()) });
        alert("Salvo.");
      } catch (err) { alert(err.message); }
    };
    document.getElementById("btn-fim").onclick = async () => {
      try {
        await api(`/api/medico/${id}/finalizar`, { method: "POST", body: JSON.stringify(payload()) });
        box.innerHTML = "<p>Consulta finalizada.</p>";
        await loadFila();
      } catch (err) { alert(err.message); }
    };
  }

  await loadFila();
  window.__poll = setInterval(loadFila, 8000);
}

async function admin() {
  view.innerHTML = `
    <h1>Usuários</h1>
    <div class="grid-2">
      <section class="panel">
        <h2>Novo usuário</h2>
        <form class="stack" id="form-u">
          <input name="usuario" placeholder="Usuário" required />
          <input name="senha" type="password" placeholder="Senha (mín. 8)" required />
          <input name="nomeCompleto" placeholder="Nome completo" required />
          <select name="papel">
            <option>Recepcionista</option><option>Enfermagem</option><option>Medico</option><option>Admin</option>
          </select>
          <button class="btn-primary" type="submit">Criar</button>
        </form>
      </section>
      <section class="panel"><h2>Lista</h2><div id="users"></div></section>
    </div>`;

  async function load() {
    const users = await api("/api/admin/usuarios");
    document.getElementById("users").innerHTML = `<table class="table"><thead><tr><th>Usuário</th><th>Nome</th><th>Papéis</th></tr></thead>
      <tbody>${users.map((u) => `<tr><td>${u.usuario}</td><td>${u.nomeCompleto}</td><td>${u.roles.join(", ")}</td></tr>`).join("")}</tbody></table>`;
  }

  document.getElementById("form-u").onsubmit = async (e) => {
    e.preventDefault();
    const f = e.target;
    try {
      await api("/api/admin/usuarios", {
        method: "POST",
        body: JSON.stringify({
          usuario: f.usuario.value,
          senha: f.senha.value,
          nomeCompleto: f.nomeCompleto.value,
          papel: f.papel.value
        })
      });
      f.reset();
      await load();
    } catch (err) { alert(err.message); }
  };
  await load();
}

window.addEventListener("hashchange", () => {
  if (window.__poll) clearInterval(window.__poll);
  render().catch((e) => { view.innerHTML = `<p class="flash err">${e.message}</p>`; });
});
render().catch((e) => { view.innerHTML = `<p class="flash err">${e.message}</p>`; });
