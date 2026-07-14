/* <pf-shell space="anonymous|client|admin" active="...">
   Orchestration du chrome. Capture le contenu de page et l'enveloppe.
   space="anonymous" -> shell auth nu (RM-21 : aucun chrome métier avant login). */
import "./pf-topbar.js";
import "./pf-sidebar.js";
import "./pf-mobile-nav.js";

class PfShell extends HTMLElement {
  connectedCallback() {
    const space = this.getAttribute("space") || "client";
    const active = this.getAttribute("active") || "";
    const pageContent = this.innerHTML;

    if (space === "anonymous") {
      this.innerHTML = `<div class="auth-only">${pageContent}</div>`;
      this.dispatchEvent(new CustomEvent("pf-shell-ready", { bubbles: true }));
      return;
    }

    this.innerHTML = `
      <div class="app-root">
        <pf-topbar space="${space}"></pf-topbar>
        <div class="app-shell">
          <pf-sidebar space="${space}" active="${active}"></pf-sidebar>
          <main class="main-col">${pageContent}</main>
        </div>
      </div>
      <pf-mobile-nav space="${space}" active="${active}"></pf-mobile-nav>`;

    this.dispatchEvent(new CustomEvent("pf-shell-ready", { bubbles: true }));
  }
}
customElements.define("pf-shell", PfShell);
