/* pf.js — point d'entrée unique. Importé en <script type="module"> par chaque page.
   Enregistre tous les composants, applique le thème, initialise états + flux. */

/* Thème d'abord (évite le flash) */
import { initThemeToggle } from "./core/theme.js";

/* Shell */
import "./shell/pf-shell.js";

/* Composants transverses & analyse */
import "./components/pf-state.js";
import "./components/pf-outcome-banner.js";
import "./components/pf-reading-section.js";
import "./components/pf-pattern-list.js";
import "./components/pf-confidence.js";
import "./components/pf-recommendation.js";
import "./components/pf-action-plan.js";
import "./components/pf-pea-badge.js";
import "./components/pf-data-freshness.js";
import "./components/pf-glossary-term.js";
import "./components/pf-sparkline.js";

/* Admin / KPI / alertes */
import "./components/pf-kpi-card.js";
import "./components/pf-calibration-table.js";
import "./components/pf-alert-item.js";

/* Core interactif */
import { initStateSwitcher } from "./core/state-switcher.js";
import { initFlowMode } from "./core/flow-mode.js";

function boot() {
  initThemeToggle();
  initFlowMode();
  // Le shell se rend en connectedCallback ; on initialise les états après un tick
  // pour que les <pf-state> internes soient présents dans le DOM.
  requestAnimationFrame(() => initStateSwitcher());
}

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", boot);
} else {
  boot();
}
