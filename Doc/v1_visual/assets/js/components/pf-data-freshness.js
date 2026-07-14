/* <pf-data-freshness level="FRESH|AGING|STALE|MISSING"> — couleur + icône + texte */
import { FRESHNESS, chip } from "../core/enums-fr.js";

class PfDataFreshness extends HTMLElement {
  connectedCallback() {
    const level = this.getAttribute("level") || "FRESH";
    this.innerHTML = chip(FRESHNESS[level] || FRESHNESS.FRESH);
  }
}
customElements.define("pf-data-freshness", PfDataFreshness);
