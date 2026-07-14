/* flow-mode.js — overlay listant les relations entrantes/sortantes de l'écran courant
   (verbatim de Doc/v1/03). Les pages déclarent leurs relations via un JSON island :
   <script type="application/json" id="pf-flow">{ "screen": "...", "in": [...], "out": [...] }</script>
   Chaque relation : { label, href } */

function readFlow() {
  const el = document.getElementById("pf-flow");
  if (!el) return null;
  try { return JSON.parse(el.textContent); } catch (_) { return null; }
}

function group(title, rels) {
  if (!rels || !rels.length) return "";
  const links = rels.map((r) => `<a class="flow-rel-link" href="${r.href}"><i class="bi bi-arrow-right-short"></i>${r.label}</a>`).join("");
  return `<div class="flow-rel-group"><div class="card-eyebrow">${title}</div>${links}</div>`;
}

function buildOverlay(flow) {
  const ov = document.createElement("div");
  ov.className = "flow-overlay";
  ov.id = "pf-flow-overlay";
  ov.innerHTML = `
    <div class="pf-row-between">
      <div class="section-title mb-0"><i class="bi bi-diagram-2"></i> Mode flux</div>
      <button class="pf-iconbtn" data-pf-flow-close aria-label="Fermer"><i class="bi bi-x-lg"></i></button>
    </div>
    <p class="mini-note mt-2">Relations de cet écran (source : spécification écrans). La navigation réelle reste active partout.</p>
    <div class="card-eyebrow mt-3">Écran courant</div>
    <div class="chip chip-navy mt-1"><i class="bi bi-window"></i>${flow.screen || "—"}</div>
    ${group("Relations entrantes", flow.in)}
    ${group("Relations sortantes", flow.out)}`;
  document.body.appendChild(ov);
  return ov;
}

export function initFlowMode() {
  const flow = readFlow();
  document.addEventListener("click", (e) => {
    const toggle = e.target.closest("[data-pf-flow]");
    const close = e.target.closest("[data-pf-flow-close]");
    if (toggle) {
      if (!flow) { alert("Mode flux : aucune relation déclarée pour cet écran."); return; }
      let ov = document.getElementById("pf-flow-overlay") || buildOverlay(flow);
      ov.classList.toggle("open");
    }
    if (close) {
      const ov = document.getElementById("pf-flow-overlay");
      if (ov) ov.classList.remove("open");
    }
  });
}
