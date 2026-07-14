/* <pf-term key="pru">PRU</pf-term> — glossaire inline (RM-28).
   Définitions de démonstration alignées sur le dictionnaire gouverné cible (Doc/v1 D.6). */

const GLOSSARY = {
  pru: { name: "PRU — Prix de revient unitaire", def: "Coût moyen d'achat par titre, dérivé des lignes ouvertes en FIFO. Jamais stocké comme vérité ; recalculé à partir des transactions." },
  invalidation: { name: "Niveau d'invalidation", def: "Seuil de prix sous (ou au-dessus) duquel la lecture du pattern n'est plus valide. Sert au risque, pas à la recommandation." },
  percentile: { name: "Classement par percentile", def: "Position relative d'une valeur dans son univers (0–100). Méthode non paramétrique, robuste aux valeurs extrêmes." },
  couverture: { name: "Couverture de données", def: "Proportion de catégories fondamentales valides. En dessous de 3 catégories, le score composite reste indisponible." },
  composite: { name: "Score composite", def: "Synthèse des catégories fondamentales valides. Indisponible si couverture insuffisante ou éligibilité PEA non confirmée." },
  pattern: { name: "Pattern de continuation", def: "Configuration graphique suggérant la reprise du mouvement précédent après une pause (rectangle, triangle, drapeau)." },
  "ratio-rr": { name: "Ratio risque / rendement", def: "Rapport entre la perte potentielle jusqu'à l'invalidation et le gain potentiel jusqu'à la cible. Indicatif, jamais une promesse." },
  drawdown: { name: "Drawdown potentiel", def: "Baisse maximale envisageable depuis le point d'entrée jusqu'au niveau d'invalidation, selon disponibilité des données." },
};

class PfTerm extends HTMLElement {
  connectedCallback() {
    const key = this.getAttribute("key") || "";
    const g = GLOSSARY[key];
    const label = this.textContent.trim();
    if (!g) { this.innerHTML = label; return; }
    this.classList.add("pf-term");
    this.setAttribute("tabindex", "0");
    this.innerHTML = `${label}<span class="pf-term-q" aria-hidden="true">?</span>
      <span class="pf-term-pop" role="tooltip">
        <span class="pf-term-name">${g.name}</span>${g.def}
      </span>`;
  }
}
customElements.define("pf-term", PfTerm);
