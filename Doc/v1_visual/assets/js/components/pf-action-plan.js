/* <pf-action-plan>
     <script type="application/json">{
       "not-held": [{ "kind":"NOTE_LEVEL","label":"...","source":"riskHints.invalidationPrice","value":"45,20 €" }, ...],
       "held": [...]
     }</script>
   </pf-action-plan>
   RM-26 : reformule des vérités déjà affichées ; aucun nouveau chiffre ; chaque étape traçable.
   Contexte-aware (data-ctx). */
import { ACTION_STEP } from "../core/enums-fr.js";

class PfActionPlan extends HTMLElement {
  connectedCallback() {
    this._data = { "not-held": [], "held": [] };
    const json = this.querySelector('script[type="application/json"]');
    if (json) { try { this._data = JSON.parse(json.textContent); } catch (_) {} }
    this._render();
    this._onCtx = () => this._render();
    document.addEventListener("pf-ctx-change", this._onCtx);
  }
  disconnectedCallback() { document.removeEventListener("pf-ctx-change", this._onCtx); }

  _render() {
    const ctx = document.body.getAttribute("data-ctx") || "not-held";
    const steps = this._data[ctx] || this._data["not-held"] || [];
    const rows = steps.map((s) => {
      const meta = ACTION_STEP[s.kind] || { icon: "bi-dot" };
      const value = s.value ? ` <strong>${s.value}</strong>` : "";
      const src = s.source ? `<div class="action-step-source"><i class="bi bi-link-45deg"></i> dérivé de <span class="code-chip">${s.source}</span></div>` : "";
      return `
        <div class="action-step">
          <div class="action-step-icon"><i class="bi ${meta.icon}"></i></div>
          <div class="action-step-body">
            <div class="action-step-label">${s.label}${value}</div>
            ${src}
          </div>
        </div>`;
    }).join("");

    this.innerHTML = `
      <div class="section-title"><i class="bi bi-list-check"></i> Vos prochaines étapes</div>
      <div class="action-plan">${rows}</div>
      <p class="mini-note mt-3 mb-0"><i class="bi bi-shield-check"></i> Étapes déterministes : reformulent des informations déjà affichées, sans nouvelle donnée.</p>`;
  }
}
customElements.define("pf-action-plan", PfActionPlan);
