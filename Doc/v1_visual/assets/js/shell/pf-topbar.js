/* <pf-topbar space="client|admin"> — marque, chip rôle, notifs, thème, flux, logout */
import { SPACE_META } from "./nav-model.js";

class PfTopbar extends HTMLElement {
  connectedCallback() {
    const space = this.getAttribute("space") || "client";
    const meta = SPACE_META[space] || SPACE_META.client;
    const notifHref = space === "client" ? "../client/notifications.html" : "../admin/dashboard.html";

    this.innerHTML = `
      <header class="topbar">
        <div class="d-flex align-items-center gap-3">
          <a class="brand-mark" href="../index.html" title="Galerie"><i class="bi bi-bar-chart-line-fill"></i></a>
          <div>
            <div class="brand-name">PredictFinance</div>
            <div class="brand-sub">${meta.roleLabel}</div>
          </div>
        </div>
        <div class="top-actions">
          <span class="chip chip-navy"><i class="bi ${meta.roleIcon}"></i>${meta.spaceLabel}</span>
          <button class="pf-iconbtn" data-pf-flow title="Mode flux" aria-label="Mode flux"><i class="bi bi-diagram-2"></i></button>
          <button class="pf-iconbtn" data-pf-theme title="Thème clair/sombre" aria-label="Basculer le thème"><i class="bi bi-moon-stars"></i></button>
          <a class="pf-iconbtn" href="${notifHref}" title="Notifications" aria-label="Notifications"><i class="bi bi-bell"></i></a>
          <a class="pf-btn pf-btn-sm" href="../anonymous/login.html"><i class="bi bi-box-arrow-left"></i><span class="d-none d-lg-inline">Déconnexion</span></a>
        </div>
      </header>`;
  }
}
customElements.define("pf-topbar", PfTopbar);
