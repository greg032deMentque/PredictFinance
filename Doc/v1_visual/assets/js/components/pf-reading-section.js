/* <pf-reading-section kind="market|support|personal|parameter" num="1" title="..." question="...">
     ...contenu libre de la lecture...
   </pf-reading-section>
   Encadre une des 4 lectures avec ordre et identité visuelle. Jamais de méga-score. */

const META = {
  market:    { icon: "bi-graph-up",   defaultTitle: "Lecture marché" },
  support:   { icon: "bi-bank",       defaultTitle: "Lecture support" },
  personal:  { icon: "bi-person-vcard", defaultTitle: "Ce que ça signifie pour moi" },
  parameter: { icon: "bi-rulers",     defaultTitle: "Lecture paramètre" },
};

class PfReadingSection extends HTMLElement {
  connectedCallback() {
    const kind = this.getAttribute("kind") || "market";
    const meta = META[kind] || META.market;
    const num = this.getAttribute("num") || "";
    const title = this.getAttribute("title") || meta.defaultTitle;
    const question = this.getAttribute("question") || "";
    const inner = this.innerHTML;

    this.innerHTML = `
      <section class="reading" data-kind="${kind}">
        <div class="reading-head">
          ${num ? `<span class="reading-num">${num}</span>` : ""}
          <div>
            <div class="reading-title"><i class="bi ${meta.icon}"></i> ${title}</div>
            ${question ? `<div class="reading-q">${question}</div>` : ""}
          </div>
        </div>
        <div class="reading-body">${inner}</div>
      </section>`;
  }
}
customElements.define("pf-reading-section", PfReadingSection);
