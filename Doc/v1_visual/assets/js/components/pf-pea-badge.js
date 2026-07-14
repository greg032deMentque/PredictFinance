/* <pf-pea-badge status="eligible|ineligible|unknown">
   RM-15 : Unknown JAMAIS positif (gris neutre, bordure pointillée). */
import { PEA } from "../core/enums-fr.js";

const BY_STATUS = {
  eligible: PEA.ConfirmedEligible,
  ineligible: PEA.ConfirmedIneligible,
  unknown: PEA.Unknown,
};

class PfPeaBadge extends HTMLElement {
  connectedCallback() {
    const status = this.getAttribute("status") || "unknown";
    const e = BY_STATUS[status] || PEA.Unknown;
    this.innerHTML = `<span class="pea-badge" data-status="${e.status}"><i class="bi ${e.icon}"></i>${e.fr}</span>`;
  }
}
customElements.define("pf-pea-badge", PfPeaBadge);
