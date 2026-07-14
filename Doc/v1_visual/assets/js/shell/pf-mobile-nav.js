/* <pf-mobile-nav space="..." active="..."> — barre du bas (mobile) */
import { NAV, MOBILE_NAV } from "./nav-model.js";

class PfMobileNav extends HTMLElement {
  connectedCallback() {
    const space = this.getAttribute("space") || "client";
    const active = this.getAttribute("active") || "";
    const ids = MOBILE_NAV[space] || [];
    const byId = Object.fromEntries((NAV[space] || []).filter((i) => i.id).map((i) => [i.id, i]));
    const links = ids.map((id) => {
      const it = byId[id];
      if (!it) return "";
      const isActive = id === active ? " active" : "";
      const short = it.label.split(" ")[0];
      return `<a class="${isActive}" href="${it.href}"><i class="bi ${it.icon}"></i><span>${short}</span></a>`;
    }).join("");
    this.innerHTML = `<div class="mobile-nav"><div class="row">${links}</div></div>`;
  }
}
customElements.define("pf-mobile-nav", PfMobileNav);
