/* <pf-pattern-list>
     <script type="application/json">{
       "main": { "id":"BULL_FLAG_CONTINUATION", "status":"Confirmed" },
       "alternatives": [{ "id":"RECTANGLE_CONTINUATION", "status":"Monitoring" }]
     }</script>
   </pf-pattern-list>
   RM-06 : principal + alternatifs TOUJOURS visibles ; le principal n'efface pas les autres. */
import { PATTERN, PATTERN_STATUS, chip } from "../core/enums-fr.js";

class PfPatternList extends HTMLElement {
  connectedCallback() {
    let data = { main: null, alternatives: [] };
    const json = this.querySelector('script[type="application/json"]');
    if (json) { try { data = JSON.parse(json.textContent); } catch (_) {} }

    const main = data.main;
    const mainName = main ? (PATTERN[main.id]?.fr || main.id) : "—";
    const mainStatus = main ? chip(PATTERN_STATUS[main.status]) : "";

    const alts = (data.alternatives || []).map((a) => {
      const name = PATTERN[a.id]?.fr || a.id;
      const st = PATTERN_STATUS[a.status];
      return `<span class="pattern-alt">${name} ${chip(st)}</span>`;
    }).join("");

    this.innerHTML = `
      <div class="pattern-main">
        <div>
          <div class="card-eyebrow">Pattern principal (affichage)</div>
          <div class="pf-fs-lg pf-fw-700 mt-1">${mainName}</div>
        </div>
        <div>${mainStatus}</div>
      </div>
      ${alts ? `<div class="mt-3"><div class="card-eyebrow mb-2">Patterns alternatifs compatibles</div><div class="pattern-alt-list">${alts}</div></div>` : ""}
      <p class="mini-note mt-2 mb-0"><i class="bi bi-info-circle"></i> Le pattern principal est un choix d'affichage : les alternatifs restent visibles.</p>`;
  }
}
customElements.define("pf-pattern-list", PfPatternList);
