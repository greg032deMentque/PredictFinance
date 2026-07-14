/* <pf-state default="ready"> contient des <div data-state="ready|empty|loading|error|non-executable">
   N'affiche qu'un seul état. Piloté par state-switcher (URL ?state=). */
class PfState extends HTMLElement {
  connectedCallback() {
    this._apply(this.getAttribute("state") || this.getAttribute("default") || "ready");
  }
  static get observedAttributes() { return ["state"]; }
  attributeChangedCallback(name, _o, v) { if (name === "state") this._apply(v); }
  _apply(state) {
    const children = this.querySelectorAll(":scope > [data-state]");
    let matched = false;
    children.forEach((c) => {
      const on = c.getAttribute("data-state") === state;
      c.classList.toggle("is-active", on);
      if (on) matched = true;
    });
    if (!matched && children.length) {
      // fallback : premier état dispo
      children[0].classList.add("is-active");
    }
  }
}
customElements.define("pf-state", PfState);
