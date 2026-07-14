/* <pf-recommendation not-held="Buy" held="Hold"
       rationale-not-held="..." rationale-held="...">
   Affiche le verbe selon le contexte courant (data-ctx sur body), bascule au switch.
   RM-10 : partition stricte des verbes ; Surveiller jamais final en détenu.
   RM-09 : visuellement en aval (le composant est placé après les 4 lectures). */
import { RECO, HOLDING, RECO_ALLOWED } from "../core/enums-fr.js";

class PfRecommendation extends HTMLElement {
  connectedCallback() {
    this._notHeld = this.getAttribute("not-held") || "Monitor";
    this._held = this.getAttribute("held") || "Hold";
    this._rNot = this.getAttribute("rationale-not-held") || "";
    this._rHeld = this.getAttribute("rationale-held") || "";
    this._render();
    this._onCtx = () => this._render();
    document.addEventListener("pf-ctx-change", this._onCtx);
  }
  disconnectedCallback() { document.removeEventListener("pf-ctx-change", this._onCtx); }

  _render() {
    const ctx = document.body.getAttribute("data-ctx") || "not-held";
    const isHeld = ctx === "held";
    const ctxKey = isHeld ? "HELD" : "NOT_HELD";
    let verbCode = isHeld ? this._held : this._notHeld;

    // Garde-fou RM-10 : si le verbe n'est pas autorisé dans ce contexte, on neutralise.
    if (!RECO_ALLOWED[ctxKey].includes(verbCode)) verbCode = isHeld ? "Wait" : "Monitor";
    const verb = RECO[verbCode];
    const hold = HOLDING[ctxKey];
    const rationale = isHeld ? this._rHeld : this._rNot;
    const cls = { positive: "success", info: "info", caution: "warning", blocked: "danger", neutral: "neutral", brand: "navy" }[verb.tone] || "navy";

    this.innerHTML = `
      <div class="reco-card">
        <div class="pf-row-between">
          <div class="card-eyebrow">Recommandation pédagogique</div>
          <span class="reco-context"><i class="bi ${hold.icon}"></i>${hold.fr}</span>
        </div>
        <div class="reco-verb"><span class="chip chip-${cls}" style="font-size:1rem"><i class="bi ${verb.icon}"></i>${verb.fr}</span></div>
        <p class="reco-rationale mb-0">${rationale}</p>
        <p class="mini-note mt-2 mb-0"><i class="bi bi-info-circle"></i> Verbe adapté au contexte « ${hold.fr.toLowerCase()} » — la lecture marché ci-dessus reste inchangée.</p>
      </div>`;
  }
}
customElements.define("pf-recommendation", PfRecommendation);
