/* state-switcher.js — 2 axes de commutation pour valider les flux :
   - ?state=ready|empty|loading|error|non-executable  -> pilote tous les <pf-state>
   - ?ctx=held|not-held                               -> data-ctx sur <pf-shell> + re-render reco
   Affiche une barre dev flottante (désactivable avec ?dev=0). Deep-linkable. */

const STATES = [
  { id: "ready", label: "Prêt" },
  { id: "empty", label: "Vide" },
  { id: "loading", label: "Chargement" },
  { id: "error", label: "Erreur" },
  { id: "non-executable", label: "Non-exécutable" },
];
const CTX = [
  { id: "not-held", label: "Non détenue" },
  { id: "held", label: "Détenue" },
];

function params() { return new URLSearchParams(location.search); }

function currentState() { return params().get("state") || "ready"; }
function currentCtx() { return params().get("ctx") || "not-held"; }

export function applyState(state) {
  document.querySelectorAll("pf-state").forEach((el) => el.setAttribute("state", state));
}

export function applyCtx(ctx) {
  document.querySelectorAll("pf-shell").forEach((el) => el.setAttribute("data-ctx", ctx));
  document.body.setAttribute("data-ctx", ctx);
  // notifie les composants sensibles au contexte (reco, plan d'action…)
  document.dispatchEvent(new CustomEvent("pf-ctx-change", { detail: { ctx } }));
}

function setParam(key, val) {
  const p = params();
  p.set(key, val);
  history.replaceState(null, "", `${location.pathname}?${p.toString()}${location.hash}`);
}

function hasStates() { return document.querySelector("pf-state") != null; }
function hasCtx() { return document.querySelector("[data-pf-ctx-aware]") != null || document.querySelector("pf-recommendation,pf-action-plan") != null; }

function renderDevbar() {
  if (params().get("dev") === "0") return;
  if (!hasStates() && !hasCtx()) return;

  const bar = document.createElement("div");
  bar.className = "pf-devbar";
  let html = "";

  if (hasStates()) {
    html += `<span class="pf-devbar-label">État</span>`;
    html += STATES.map((s) => `<button data-axis="state" data-val="${s.id}">${s.label}</button>`).join("");
  }
  if (hasStates() && hasCtx()) html += `<span class="sep"></span>`;
  if (hasCtx()) {
    html += `<span class="pf-devbar-label">Contexte</span>`;
    html += CTX.map((c) => `<button data-axis="ctx" data-val="${c.id}">${c.label}</button>`).join("");
  }
  bar.innerHTML = html;
  document.body.appendChild(bar);

  bar.addEventListener("click", (e) => {
    const btn = e.target.closest("button");
    if (!btn) return;
    const axis = btn.dataset.axis, val = btn.dataset.val;
    if (axis === "state") { setParam("state", val); applyState(val); }
    else { setParam("ctx", val); applyCtx(val); }
    syncDevbar(bar);
  });
  syncDevbar(bar);
}

function syncDevbar(bar) {
  const st = currentState(), cx = currentCtx();
  bar.querySelectorAll("button").forEach((b) => {
    const on = (b.dataset.axis === "state" && b.dataset.val === st) ||
               (b.dataset.axis === "ctx" && b.dataset.val === cx);
    b.classList.toggle("active", on);
  });
}

export function initStateSwitcher() {
  applyState(currentState());
  applyCtx(currentCtx());
  renderDevbar();
}
