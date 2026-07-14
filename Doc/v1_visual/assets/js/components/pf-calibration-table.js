/* <pf-calibration-table>
     <script type="application/json">[
       { "level":"LOW","hitRate":45,"count":120 },
       { "level":"MEDIUM","hitRate":62,"count":210 },
       { "level":"HIGH","hitRate":71,"count":95 }
     ]</script>
   </pf-calibration-table>
   Calibration de la confiance : taux d'atteinte de cible par bucket.
   Détecte une mauvaise calibration (HIGH ≤ LOW). */
import { CONFIDENCE } from "../core/enums-fr.js";

class PfCalibrationTable extends HTMLElement {
  connectedCallback() {
    let rows = [];
    const json = this.querySelector('script[type="application/json"]');
    if (json) { try { rows = JSON.parse(json.textContent); } catch (_) {} }

    const byLevel = Object.fromEntries(rows.map((r) => [r.level, r]));
    const low = byLevel.LOW?.hitRate ?? 0;
    const high = byLevel.HIGH?.hitRate ?? 0;
    const miscalibrated = high <= low;

    const body = rows.map((r) => {
      const c = CONFIDENCE[r.level] || { fr: r.level };
      return `
        <tr class="calibration-row">
          <td>${c.fr}</td>
          <td>
            <div class="pf-row gap-2">
              <div class="calib-bar" style="flex:1"><span style="width:${r.hitRate}%"></span></div>
              <strong>${r.hitRate} %</strong>
            </div>
          </td>
          <td class="muted">${r.count}</td>
        </tr>`;
    }).join("");

    const verdict = miscalibrated
      ? `<div class="non-exec-panel mt-3"><span class="non-exec-tag">Calibration à revoir</span>
           Le taux d'atteinte « confiance élevée » (${high} %) n'est pas supérieur à « confiance faible » (${low} %).
           <a class="pf-btn pf-btn-sm" href="../admin/scoring-policy.html"><i class="bi bi-ui-checks-grid"></i> Ouvrir la politique de scoring</a></div>`
      : `<div class="why-panel mt-3"><i class="bi bi-check-circle"></i> Calibration cohérente : la confiance élevée atteint la cible plus souvent que la confiance faible.</div>`;

    this.innerHTML = `
      <div class="table-card">
        <table class="pf-table">
          <thead><tr><th>Niveau de confiance</th><th>Taux d'atteinte de cible</th><th>Signaux</th></tr></thead>
          <tbody>${body}</tbody>
        </table>
      </div>
      ${verdict}`;
  }
}
customElements.define("pf-calibration-table", PfCalibrationTable);
