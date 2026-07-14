/* <pf-sidebar space="client|admin" active="dashboard"> — nav depuis nav-model.js */
import { NAV } from "./nav-model.js";

class PfSidebar extends HTMLElement {
  connectedCallback() {
    const space = this.getAttribute("space") || "client";
    const active = this.getAttribute("active") || "";
    const items = NAV[space] || [];
    const links = items.map((it) => {
      if (it.group) return `<div class="sidebar-section-label">${it.group}</div>`;
      const isActive = it.id === active ? " active" : "";
      return `<a class="nav-link${isActive}" href="${it.href}"><i class="bi ${it.icon}"></i><span>${it.label}</span></a>`;
    }).join("");

    this.innerHTML = `
      <aside class="sidebar">
        <nav class="nav flex-column">${links}</nav>
      </aside>`;
  }
}
customElements.define("pf-sidebar", PfSidebar);
