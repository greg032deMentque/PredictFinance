/* <pf-sparkline values="12,14,13,18,..." [stroke="var(--pf-blue)"] [area]> — SVG inline, sans lib */
class PfSparkline extends HTMLElement {
  connectedCallback() {
    const raw = (this.getAttribute("values") || "").split(",").map(Number).filter((n) => !isNaN(n));
    if (raw.length < 2) { this.innerHTML = ""; return; }
    const stroke = this.getAttribute("stroke") || "var(--pf-blue)";
    const area = this.hasAttribute("area");
    const w = 100, h = 32, pad = 2;
    const min = Math.min(...raw), max = Math.max(...raw), span = max - min || 1;
    const pts = raw.map((v, i) => {
      const x = pad + (i * (w - 2 * pad)) / (raw.length - 1);
      const y = h - pad - ((v - min) / span) * (h - 2 * pad);
      return [x, y];
    });
    const line = pts.map((p, i) => `${i ? "L" : "M"}${p[0].toFixed(1)},${p[1].toFixed(1)}`).join(" ");
    const areaPath = area ? `<path d="${line} L${pts[pts.length - 1][0].toFixed(1)},${h} L${pts[0][0].toFixed(1)},${h} Z" fill="${stroke}" opacity=".12"/>` : "";
    this.innerHTML = `<svg class="pf-sparkline" viewBox="0 0 ${w} ${h}" preserveAspectRatio="none" aria-hidden="true">
      ${areaPath}<path d="${line}" fill="none" stroke="${stroke}" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
    </svg>`;
  }
}
customElements.define("pf-sparkline", PfSparkline);
