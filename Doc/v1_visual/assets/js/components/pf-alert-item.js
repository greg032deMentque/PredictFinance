/* <pf-alert-item trigger="PATTERN_STATE_CHANGE" instrument="TotalEnergies"
       when="il y a 2 h" unread="true" href="...">
   RM-25 : route, n'explique pas. Porte le déclencheur ; l'écran cible détient la vérité. */
import { ALERT_TRIGGER } from "../core/enums-fr.js";

const TONE_BG = { positive: "bg-soft-success", info: "bg-soft-info", caution: "bg-soft-warning", blocked: "bg-soft-danger", neutral: "bg-soft-neutral" };

class PfAlertItem extends HTMLElement {
  connectedCallback() {
    const code = this.getAttribute("trigger") || "PATTERN_STATE_CHANGE";
    const t = ALERT_TRIGGER[code] || ALERT_TRIGGER.PATTERN_STATE_CHANGE;
    const instrument = this.getAttribute("instrument") || "";
    const when = this.getAttribute("when") || "";
    const unread = this.getAttribute("unread") === "true";
    const href = this.getAttribute("href") || `../client/${t.target}.html`;

    this.innerHTML = `
      <div class="alert-item" data-unread="${unread}">
        <span class="alert-icon ${TONE_BG[t.tone] || TONE_BG.info}"><i class="bi ${t.icon}"></i></span>
        <div class="alert-body">
          <div class="pf-row-between">
            <span class="alert-title">${instrument}</span>
            ${when ? `<span class="mini-note">${when}</span>` : ""}
          </div>
          <div class="mt-1"><span class="chip chip-${t.tone === "info" ? "info" : t.tone === "caution" ? "warning" : "neutral"}"><i class="bi ${t.icon}"></i>${t.fr}</span></div>
          <a class="alert-route mt-2 d-inline-flex align-items-center gap-1" href="${href}">
            Ouvrir : ${t.targetLabel} <i class="bi bi-arrow-right-short"></i>
          </a>
        </div>
      </div>`;
  }
}
customElements.define("pf-alert-item", PfAlertItem);
