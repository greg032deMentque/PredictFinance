/* <pf-confidence level="MEDIUM">
     <script type="application/json">[{"label":"...","state":"met|partial|absent","source":"DETECTION|VALIDATION|INVALIDATION"}]</script>
   </pf-confidence>
   RM-27 : explique le niveau via critères, ne le recalcule pas. */
import { CONFIDENCE, chip } from "../core/enums-fr.js";

const ICON = { met: "bi-check-circle-fill", partial: "bi-exclamation-circle-fill", absent: "bi-x-circle" };
const SRC_FR = { DETECTION: "Détection", VALIDATION: "Validation", INVALIDATION: "Invalidation" };

class PfConfidence extends HTMLElement {
  connectedCallback() {
    const level = this.getAttribute("level") || "MEDIUM";
    let criteria = [];
    const json = this.querySelector('script[type="application/json"]');
    if (json) { try { criteria = JSON.parse(json.textContent); } catch (_) {} }

    const rows = criteria.map((c) => `
      <div class="criterion" data-state="${c.state}">
        <i class="bi ${ICON[c.state] || ICON.absent} criterion-icon"></i>
        <div>
          <div>${c.label}</div>
          <div class="criterion-source">${SRC_FR[c.source] || c.source}</div>
        </div>
      </div>`).join("");

    this.innerHTML = `
      <div class="confidence-head">
        ${chip(CONFIDENCE[level] || CONFIDENCE.MEDIUM)}
        <span class="mini-note">Pourquoi ce niveau ?</span>
      </div>
      <div class="confidence-grid">${rows}</div>`;
  }
}
customElements.define("pf-confidence", PfConfidence);
