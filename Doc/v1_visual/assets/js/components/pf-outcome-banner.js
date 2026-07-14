/* <pf-outcome-banner outcome="CrediblePatternFound|...">
   Issue métier de 1er rang. Non-exécutable = info/caution, JAMAIS erreur (RM-24). */
import { ANALYSIS_OUTCOME } from "../core/enums-fr.js";

class PfOutcomeBanner extends HTMLElement {
  connectedCallback() {
    const code = this.getAttribute("outcome") || "CrediblePatternFound";
    const o = ANALYSIS_OUTCOME[code] || ANALYSIS_OUTCOME.CrediblePatternFound;
    const tone = o.tone; // positive | info | caution | blocked
    const hint = o.executable ? "Analyse exécutable" : "État métier — pas une erreur technique";
    this.innerHTML = `
      <div class="outcome-banner" data-tone="${tone}">
        <i class="bi ${o.icon}"></i>
        <div>
          <div>${o.fr}</div>
          <div class="mini-note" style="color:inherit;opacity:.85">${hint}</div>
        </div>
      </div>`;
  }
}
customElements.define("pf-outcome-banner", PfOutcomeBanner);
