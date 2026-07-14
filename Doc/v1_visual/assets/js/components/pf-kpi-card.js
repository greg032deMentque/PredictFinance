/* <pf-kpi-card label="..." value="71 %" delta="+4 pts" trend="up|down"
       icon="bi-bullseye" tone="positive" spark="..." availability="KPI_AVAILABLE">
   Disponibilité KPI comme état métier (RM-29). */
import { KPI_AVAIL } from "../core/enums-fr.js";

const TONE_BG = { positive: "bg-soft-success", info: "bg-soft-info", caution: "bg-soft-warning", blocked: "bg-soft-danger", neutral: "bg-soft-neutral", brand: "bg-soft-navy" };

class PfKpiCard extends HTMLElement {
  connectedCallback() {
    const label = this.getAttribute("label") || "";
    const value = this.getAttribute("value") || "";
    const delta = this.getAttribute("delta") || "";
    const trend = this.getAttribute("trend") || "up";
    const icon = this.getAttribute("icon") || "bi-graph-up";
    const tone = this.getAttribute("tone") || "brand";
    const spark = this.getAttribute("spark") || "";
    const availability = this.getAttribute("availability") || "KPI_AVAILABLE";

    if (availability !== "KPI_AVAILABLE") {
      const a = KPI_AVAIL[availability] || KPI_AVAIL.KPI_INSUFFICIENT_DATA;
      this.innerHTML = `
        <div class="metric-card">
          <div class="metric-label">${label}</div>
          <div class="kpi-unavailable">
            <strong><i class="bi bi-dash-circle"></i> ${a.fr}</strong>
            <span class="mini-note" style="color:inherit">Indicateur non calculable pour l'instant — état métier, pas une erreur.</span>
          </div>
        </div>`;
      return;
    }

    const deltaCls = trend === "down" ? "metric-delta-down" : "metric-delta-up";
    const deltaIcon = trend === "down" ? "bi-arrow-down-right" : "bi-arrow-up-right";
    const sparkEl = spark ? `<pf-sparkline values="${spark}" area></pf-sparkline>` : "";

    this.innerHTML = `
      <div class="metric-card">
        <div class="pf-row-between">
          <span class="metric-icon ${TONE_BG[tone] || TONE_BG.brand}"><i class="bi ${icon}"></i></span>
          ${delta ? `<span class="${deltaCls}"><i class="bi ${deltaIcon}"></i> ${delta}</span>` : ""}
        </div>
        <div class="metric-value">${value}</div>
        <div class="metric-label">${label}</div>
        ${sparkEl}
      </div>`;
  }
}
customElements.define("pf-kpi-card", PfKpiCard);
