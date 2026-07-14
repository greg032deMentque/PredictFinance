/* pf.bundle.js — tous les modules concaténés, compatible file:// (pas de type="module")
   Généré manuellement depuis les sources ES modules de assets/js/.
   Modifier les sources, pas ce fichier. */
(function () {
"use strict";

/* ==================== core/enums-fr.js ==================== */

const TONE_CLASS = {
  positive: "success",
  info: "info",
  caution: "warning",
  blocked: "danger",
  neutral: "neutral",
  brand: "navy",
};

const ANALYSIS_OUTCOME = {
  CrediblePatternFound:       { fr: "Pattern crédible détecté",            tone: "positive", icon: "bi-check-circle-fill", executable: true },
  MultipleCompatiblePatterns: { fr: "Plusieurs patterns compatibles",      tone: "info",     icon: "bi-diagram-3-fill",   executable: true },
  NoCrediblePattern:          { fr: "Aucun pattern crédible retenu",       tone: "caution",  icon: "bi-slash-circle",     executable: false },
  InsufficientData:           { fr: "Données insuffisantes pour analyser", tone: "caution",  icon: "bi-hourglass-split",  executable: false },
  UnsupportedInstrument:      { fr: "Instrument hors périmètre V1",        tone: "blocked",  icon: "bi-x-octagon",        executable: false },
  UnsupportedContext:         { fr: "Contexte non pris en charge",         tone: "blocked",  icon: "bi-x-octagon",        executable: false },
};

const PATTERN_STATUS = {
  Forming:     { fr: "En formation",  tone: "info",     icon: "bi-circle-half" },
  Monitoring:  { fr: "À surveiller",  tone: "caution",  icon: "bi-eye" },
  Confirmed:   { fr: "Confirmé",      tone: "positive", icon: "bi-check-circle" },
  Invalidated: { fr: "Invalidé",      tone: "blocked",  icon: "bi-x-circle" },
  Completed:   { fr: "Terminé",       tone: "neutral",  icon: "bi-flag" },
};

const CONFIDENCE = {
  LOW:    { fr: "Confiance faible",   tone: "caution",  icon: "bi-reception-1" },
  MEDIUM: { fr: "Confiance moyenne",  tone: "info",     icon: "bi-reception-2" },
  HIGH:   { fr: "Confiance élevée",   tone: "positive", icon: "bi-reception-4" },
};

const HOLDING = {
  NOT_HELD: { fr: "Non détenue", icon: "bi-eye" },
  HELD:     { fr: "Détenue",     icon: "bi-wallet2" },
};

const RECO = {
  Monitor:   { fr: "Surveiller", tone: "caution",  icon: "bi-eye",             contexts: ["NOT_HELD"] },
  Wait:      { fr: "Attendre",   tone: "caution",  icon: "bi-hourglass",       contexts: ["NOT_HELD", "HELD"] },
  Buy:       { fr: "Acheter",    tone: "positive", icon: "bi-cart-plus",       contexts: ["NOT_HELD"] },
  Hold:      { fr: "Conserver",  tone: "info",     icon: "bi-shield-check",    contexts: ["HELD"] },
  Reinforce: { fr: "Renforcer",  tone: "positive", icon: "bi-plus-circle",     contexts: ["HELD"] },
  Lighten:   { fr: "Alléger",    tone: "caution",  icon: "bi-dash-circle",     contexts: ["HELD"] },
  Sell:      { fr: "Vendre",     tone: "blocked",  icon: "bi-box-arrow-right", contexts: ["HELD"] },
};
const RECO_ALLOWED = {
  NOT_HELD: ["Monitor", "Wait", "Buy"],
  HELD: ["Hold", "Reinforce", "Lighten", "Sell", "Wait"],
};

const PEA = {
  ConfirmedEligible:   { fr: "Éligibilité PEA confirmée",     status: "eligible",   icon: "bi-check-circle" },
  ConfirmedIneligible: { fr: "Non éligible PEA confirmée",    status: "ineligible", icon: "bi-x-circle" },
  Unknown:             { fr: "Éligibilité PEA non confirmée", status: "unknown",    icon: "bi-question-circle" },
};

const SIGNAL_OUTCOME = {
  TARGET_HIT:       { fr: "Cible atteinte",       tone: "positive", icon: "bi-bullseye" },
  INVALIDATION_HIT: { fr: "Invalidation touchée", tone: "blocked",  icon: "bi-x-circle" },
  STILL_OPEN:       { fr: "En cours",             tone: "info",     icon: "bi-arrow-repeat" },
  NOT_EVALUABLE:    { fr: "Non évaluable",        tone: "neutral",  icon: "bi-dash-circle" },
};

const COMPOSITE_AVAIL = {
  AVAILABLE:                        { fr: "Score composite disponible", tone: "positive", icon: "bi-check-circle" },
  INSUFFICIENT_COVERAGE:            { fr: "Score composite indisponible : couverture de données insuffisante", tone: "caution", icon: "bi-exclamation-circle" },
  PEA_UNKNOWN_BLOCKING:             { fr: "Score composite indisponible : éligibilité PEA non confirmée", tone: "caution", icon: "bi-exclamation-circle" },
  CONFIRMED_INELIGIBLE_IN_UNIVERSE: { fr: "Score composite indisponible : instrument confirmé non éligible PEA dans cet univers", tone: "caution", icon: "bi-exclamation-circle" },
  UNSUPPORTED_UNIVERSE:             { fr: "Score composite indisponible : univers demandé non pris en charge", tone: "blocked", icon: "bi-x-octagon" },
  PROVIDER_DATA_INCOMPLETE:         { fr: "Score composite indisponible : données fournisseur incomplètes ou indisponibles", tone: "caution", icon: "bi-exclamation-circle" },
};

const FRESHNESS = {
  FRESH:   { fr: "Données à jour",       tone: "positive", icon: "bi-check-circle" },
  AGING:   { fr: "Données à surveiller", tone: "caution",  icon: "bi-clock-history" },
  STALE:   { fr: "Données obsolètes",    tone: "blocked",  icon: "bi-exclamation-triangle" },
  MISSING: { fr: "Données indisponibles", tone: "neutral", icon: "bi-dash-circle" },
};

const NOTIF_CATEGORY = {
  Watchlist: { fr: "Watchlist",     icon: "bi-stars" },
  Analysis:  { fr: "Analyse",       icon: "bi-graph-up" },
  Learning:  { fr: "Apprentissage", icon: "bi-mortarboard" },
  Account:   { fr: "Compte",        icon: "bi-person" },
};

const ALERT_TRIGGER = {
  PATTERN_STATE_CHANGE: { fr: "Changement d'état de pattern", tone: "info",    icon: "bi-arrow-left-right", target: "analysis-result",   targetLabel: "Résultat d'analyse" },
  LEVEL_CROSSED:        { fr: "Niveau franchi",               tone: "caution", icon: "bi-graph-up-arrow",   target: "instrument-detail", targetLabel: "Détail instrument" },
  DATA_STALE:           { fr: "Données obsolètes",            tone: "neutral", icon: "bi-clock-history",    target: "instrument-detail", targetLabel: "Détail instrument" },
};

const USER_ROLE = {
  User:       { fr: "Utilisateur",          space: "client" },
  Admin:      { fr: "Administrateur",        space: "admin" },
  SuperAdmin: { fr: "Super-administrateur",  space: "admin" },
};
const USER_STATUS = {
  ACTIVE:   { fr: "Actif",      tone: "positive", icon: "bi-check-circle" },
  PENDING:  { fr: "En attente", tone: "caution",  icon: "bi-hourglass" },
  DISABLED: { fr: "Désactivé",  tone: "blocked",  icon: "bi-x-circle" },
};

const KPI_AVAIL = {
  KPI_AVAILABLE:         { fr: "Indicateur disponible" },
  KPI_INSUFFICIENT_DATA: { fr: "Données insuffisantes sur la période" },
  KPI_WINDOW_TOO_YOUNG:  { fr: "Fenêtre trop récente pour ce calcul" },
};

const FUNDAMENTAL_CATEGORY = {
  Profitability:     "Rentabilité",
  Valuation:         "Valorisation",
  FinancialStrength: "Solidité financière",
  Growth:            "Croissance",
  Income:            "Rendement",
};

const PATTERN = {
  RECTANGLE_CONTINUATION:            { fr: "Rectangle de continuation" },
  SYMMETRICAL_TRIANGLE_CONTINUATION: { fr: "Triangle symétrique de continuation" },
  BULL_FLAG_CONTINUATION:            { fr: "Drapeau haussier" },
  BEAR_FLAG_CONTINUATION:            { fr: "Drapeau baissier" },
};

const ACTION_STEP = {
  NOTE_LEVEL:       { icon: "bi-bookmark-star" },
  REVIEW_AT:        { icon: "bi-calendar-check" },
  SET_ALERT:        { icon: "bi-bell" },
  HOLDING_REMINDER: { icon: "bi-wallet2" },
  WAIT_FOR_DATA:    { icon: "bi-hourglass-split" },
};

function chip(entry, { outline = false } = {}) {
  if (!entry) return "";
  const cls = TONE_CLASS[entry.tone] || "neutral";
  return `<span class="chip chip-${cls}${outline ? " chip-outline" : ""}"><i class="bi ${entry.icon}"></i>${entry.fr}</span>`;
}

/* ==================== core/theme.js ==================== */

const THEME_KEY = "pf-theme";

function applyStoredTheme() {
  let theme = "light";
  try { theme = localStorage.getItem(THEME_KEY) || "light"; } catch (_) {}
  document.documentElement.setAttribute("data-theme", theme);
  return theme;
}

function toggleTheme() {
  const cur = document.documentElement.getAttribute("data-theme") || "light";
  const next = cur === "dark" ? "light" : "dark";
  document.documentElement.setAttribute("data-theme", next);
  try { localStorage.setItem(THEME_KEY, next); } catch (_) {}
  syncThemeIcons(next);
}

function syncThemeIcons(theme) {
  document.querySelectorAll("[data-pf-theme] i").forEach((i) => {
    i.className = theme === "dark" ? "bi bi-sun" : "bi bi-moon-stars";
  });
}

function initThemeToggle() {
  const theme = document.documentElement.getAttribute("data-theme") || "light";
  syncThemeIcons(theme);
  document.addEventListener("click", (e) => {
    const btn = e.target.closest("[data-pf-theme]");
    if (btn) toggleTheme();
  });
}

applyStoredTheme();

/* ==================== shell/nav-model.js ==================== */

const NAV = {
  client: [
    { group: "Mon espace" },
    { id: "dashboard", href: "../client/dashboard.html", icon: "bi-house-door", label: "Accueil user" },
    { id: "watchlist", href: "../client/watchlist.html", icon: "bi-stars", label: "Watchlist" },
    { id: "portfolio", href: "../client/portfolio.html", icon: "bi-briefcase", label: "Portfolio" },
    { id: "analysis-entry", href: "../client/analysis-entry.html", icon: "bi-graph-up-arrow", label: "Analyse" },
    { id: "history", href: "../client/history.html", icon: "bi-clock-history", label: "Historique" },
    { id: "simulation", href: "../client/simulation.html", icon: "bi-bar-chart-steps", label: "Simulation" },
    { id: "notifications", href: "../client/notifications.html", icon: "bi-bell", label: "Notifications" },
    { group: "Compte" },
    { id: "account-profile", href: "../client/account-profile.html", icon: "bi-person-gear", label: "Compte" }, { group: "Cibles spec" }, { id: "target-onboarding", href: "../client/target-onboarding-empty.html", icon: "bi-rocket-takeoff", label: "Onboarding cible" }, { id: "target-learn", href: "../client/target-learn.html", icon: "bi-mortarboard", label: "Learn cible" }, { id: "target-help", href: "../client/target-help-center.html", icon: "bi-life-preserver", label: "Aide cible" }
  ],
  admin: [
    { group: "Pilotage" },
    { id: "dashboard", href: "../admin/dashboard.html", icon: "bi-speedometer2", label: "Overview" },
    { id: "users", href: "../admin/users.html", icon: "bi-people", label: "Utilisateurs" },
    { group: "Gouvernance" },
    { id: "instrument-registry", href: "../admin/instrument-registry.html", icon: "bi-building-gear", label: "Instrument registry" },
    { id: "pea-registry", href: "../admin/pea-registry.html", icon: "bi-patch-check", label: "PEA registry" },
    { id: "scoring-policy", href: "../admin/scoring-policy.html", icon: "bi-sliders", label: "Scoring policy" },
    { id: "parameter-dictionary", href: "../admin/parameter-dictionary.html", icon: "bi-journal-text", label: "Parameter dictionary" },
    { id: "wording-versions", href: "../admin/wording-versions.html", icon: "bi-chat-left-text", label: "Wording versions" },
    { id: "snapshot-audit", href: "../admin/snapshot-audit.html", icon: "bi-clock-history", label: "Snapshot audit" },
    { id: "data-quality", href: "../admin/data-quality.html", icon: "bi-database-check", label: "Data quality" }, { group: "Cibles KPI" }, { id: "target-signal-quality", href: "../admin/target-signal-quality.html", icon: "bi-graph-up-arrow", label: "Signal quality cible" }, { id: "target-engagement", href: "../admin/target-engagement.html", icon: "bi-people", label: "Engagement cible" }
  ]
};

const MOBILE_NAV = {
  client: ["dashboard", "watchlist", "analysis-entry", "notifications", "account-profile"],
  admin: ["dashboard", "users", "instrument-registry", "snapshot-audit", "data-quality"]
};

const SPACE_META = {
  client: { roleLabel: "Espace client", roleIcon: "bi-person-badge", spaceLabel: "Investisseur" },
  admin: { roleLabel: "Espace admin", roleIcon: "bi-shield-lock", spaceLabel: "Gouvernance" }
};

/* ==================== shell/pf-topbar.js ==================== */

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

/* ==================== shell/pf-sidebar.js ==================== */

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

/* ==================== shell/pf-mobile-nav.js ==================== */

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

/* ==================== shell/pf-shell.js ==================== */

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

/* ==================== components/pf-state.js ==================== */

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
      children[0].classList.add("is-active");
    }
  }
}
customElements.define("pf-state", PfState);

/* ==================== components/pf-outcome-banner.js ==================== */

class PfOutcomeBanner extends HTMLElement {
  connectedCallback() {
    const code = this.getAttribute("outcome") || "CrediblePatternFound";
    const o = ANALYSIS_OUTCOME[code] || ANALYSIS_OUTCOME.CrediblePatternFound;
    const tone = o.tone;
    const hint = o.executable ? "Analyse exécutable" : "État métier — pas une erreur technique";
    this.innerHTML = `
      <div class="outcome-banner" data-tone="${tone}">
        <i class="bi ${o.icon}"></i>
        <div>
          <div>${o.fr}</div>
          <div class="mini-note" style="color:inherit;opacity:.85">${hint}</div>
        </div>
      </div>`;
  }
}
customElements.define("pf-outcome-banner", PfOutcomeBanner);

/* ==================== components/pf-reading-section.js ==================== */

const READING_META = {
  market:    { icon: "bi-graph-up",     defaultTitle: "Lecture marché" },
  support:   { icon: "bi-bank",         defaultTitle: "Lecture support" },
  personal:  { icon: "bi-person-vcard", defaultTitle: "Ce que ça signifie pour moi" },
  parameter: { icon: "bi-rulers",       defaultTitle: "Lecture paramètre" },
};

class PfReadingSection extends HTMLElement {
  connectedCallback() {
    const kind = this.getAttribute("kind") || "market";
    const meta = READING_META[kind] || READING_META.market;
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

/* ==================== components/pf-pattern-list.js ==================== */

class PfPatternList extends HTMLElement {
  connectedCallback() {
    let data = { main: null, alternatives: [] };
    const json = this.querySelector('script[type="application/json"]');
    if (json) { try { data = JSON.parse(json.textContent); } catch (_) {} }

    const main = data.main;
    const mainName = main ? (PATTERN[main.id]?.fr || main.id) : "—";
    const mainStatus = main ? chip(PATTERN_STATUS[main.status]) : "";

    const alts = (data.alternatives || []).map((a) => {
      const name = PATTERN[a.id]?.fr || a.id;
      const st = PATTERN_STATUS[a.status];
      return `<span class="pattern-alt">${name} ${chip(st)}</span>`;
    }).join("");

    this.innerHTML = `
      <div class="pattern-main">
        <div>
          <div class="card-eyebrow">Pattern principal (affichage)</div>
          <div class="pf-fs-lg pf-fw-700 mt-1">${mainName}</div>
        </div>
        <div>${mainStatus}</div>
      </div>
      ${alts ? `<div class="mt-3"><div class="card-eyebrow mb-2">Patterns alternatifs compatibles</div><div class="pattern-alt-list">${alts}</div></div>` : ""}
      <p class="mini-note mt-2 mb-0"><i class="bi bi-info-circle"></i> Le pattern principal est un choix d'affichage : les alternatifs restent visibles.</p>`;
  }
}
customElements.define("pf-pattern-list", PfPatternList);

/* ==================== components/pf-confidence.js ==================== */

const CONFIDENCE_ICON = { met: "bi-check-circle-fill", partial: "bi-exclamation-circle-fill", absent: "bi-x-circle" };
const CONFIDENCE_SRC_FR = { DETECTION: "Détection", VALIDATION: "Validation", INVALIDATION: "Invalidation" };

class PfConfidence extends HTMLElement {
  connectedCallback() {
    const level = this.getAttribute("level") || "MEDIUM";
    let criteria = [];
    const json = this.querySelector('script[type="application/json"]');
    if (json) { try { criteria = JSON.parse(json.textContent); } catch (_) {} }

    const rows = criteria.map((c) => `
      <div class="criterion" data-state="${c.state}">
        <i class="bi ${CONFIDENCE_ICON[c.state] || CONFIDENCE_ICON.absent} criterion-icon"></i>
        <div>
          <div>${c.label}</div>
          <div class="criterion-source">${CONFIDENCE_SRC_FR[c.source] || c.source}</div>
        </div>
      </div>`).join("");

    this.innerHTML = `
      <div class="confidence-head">
        ${chip(CONFIDENCE[level] || CONFIDENCE.MEDIUM)}
        <span class="mini-note">Pourquoi ce niveau ?</span>
      </div>
      <div class="confidence-grid">${rows}</div>`;
  }
}
customElements.define("pf-confidence", PfConfidence);

/* ==================== components/pf-recommendation.js ==================== */

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

/* ==================== components/pf-action-plan.js ==================== */

class PfActionPlan extends HTMLElement {
  connectedCallback() {
    this._data = { "not-held": [], "held": [] };
    const json = this.querySelector('script[type="application/json"]');
    if (json) { try { this._data = JSON.parse(json.textContent); } catch (_) {} }
    this._render();
    this._onCtx = () => this._render();
    document.addEventListener("pf-ctx-change", this._onCtx);
  }
  disconnectedCallback() { document.removeEventListener("pf-ctx-change", this._onCtx); }

  _render() {
    const ctx = document.body.getAttribute("data-ctx") || "not-held";
    const steps = this._data[ctx] || this._data["not-held"] || [];
    const rows = steps.map((s) => {
      const meta = ACTION_STEP[s.kind] || { icon: "bi-dot" };
      const value = s.value ? ` <strong>${s.value}</strong>` : "";
      const src = s.source ? `<div class="action-step-source"><i class="bi bi-link-45deg"></i> dérivé de <span class="code-chip">${s.source}</span></div>` : "";
      return `
        <div class="action-step">
          <div class="action-step-icon"><i class="bi ${meta.icon}"></i></div>
          <div class="action-step-body">
            <div class="action-step-label">${s.label}${value}</div>
            ${src}
          </div>
        </div>`;
    }).join("");

    this.innerHTML = `
      <div class="section-title"><i class="bi bi-list-check"></i> Vos prochaines étapes</div>
      <div class="action-plan">${rows}</div>
      <p class="mini-note mt-3 mb-0"><i class="bi bi-shield-check"></i> Étapes déterministes : reformulent des informations déjà affichées, sans nouvelle donnée.</p>`;
  }
}
customElements.define("pf-action-plan", PfActionPlan);

/* ==================== components/pf-pea-badge.js ==================== */

const PEA_BY_STATUS = {
  eligible:   PEA.ConfirmedEligible,
  ineligible: PEA.ConfirmedIneligible,
  unknown:    PEA.Unknown,
};

class PfPeaBadge extends HTMLElement {
  connectedCallback() {
    const status = this.getAttribute("status") || "unknown";
    const e = PEA_BY_STATUS[status] || PEA.Unknown;
    this.innerHTML = `<span class="pea-badge" data-status="${e.status}"><i class="bi ${e.icon}"></i>${e.fr}</span>`;
  }
}
customElements.define("pf-pea-badge", PfPeaBadge);

/* ==================== components/pf-data-freshness.js ==================== */

class PfDataFreshness extends HTMLElement {
  connectedCallback() {
    const level = this.getAttribute("level") || "FRESH";
    this.innerHTML = chip(FRESHNESS[level] || FRESHNESS.FRESH);
  }
}
customElements.define("pf-data-freshness", PfDataFreshness);

/* ==================== components/pf-glossary-term.js ==================== */

const GLOSSARY = {
  pru:          { name: "PRU — Prix de revient unitaire",    def: "Coût moyen d'achat par titre, dérivé des lignes ouvertes en FIFO. Jamais stocké comme vérité ; recalculé à partir des transactions." },
  invalidation: { name: "Niveau d'invalidation",            def: "Seuil de prix sous (ou au-dessus) duquel la lecture du pattern n'est plus valide. Sert au risque, pas à la recommandation." },
  percentile:   { name: "Classement par percentile",        def: "Position relative d'une valeur dans son univers (0–100). Méthode non paramétrique, robuste aux valeurs extrêmes." },
  couverture:   { name: "Couverture de données",            def: "Proportion de catégories fondamentales valides. En dessous de 3 catégories, le score composite reste indisponible." },
  composite:    { name: "Score composite",                  def: "Synthèse des catégories fondamentales valides. Indisponible si couverture insuffisante ou éligibilité PEA non confirmée." },
  pattern:      { name: "Pattern de continuation",          def: "Configuration graphique suggérant la reprise du mouvement précédent après une pause (rectangle, triangle, drapeau)." },
  "ratio-rr":   { name: "Ratio risque / rendement",         def: "Rapport entre la perte potentielle jusqu'à l'invalidation et le gain potentiel jusqu'à la cible. Indicatif, jamais une promesse." },
  drawdown:     { name: "Drawdown potentiel",               def: "Baisse maximale envisageable depuis le point d'entrée jusqu'au niveau d'invalidation, selon disponibilité des données." },
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

/* ==================== components/pf-sparkline.js ==================== */

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

/* ==================== components/pf-kpi-card.js ==================== */

const TONE_BG = { positive: "bg-soft-success", info: "bg-soft-info", caution: "bg-soft-warning", blocked: "bg-soft-danger", neutral: "bg-soft-neutral", brand: "bg-soft-navy" };

class PfKpiCard extends HTMLElement {
  connectedCallback() {
    const label = this.getAttribute("label") || "";
    const value = this.getAttribute("value") || "";
    const delta = this.getAttribute("delta") || "";
    const trend = this.getAttribute("trend") || "up";
    const icon = this.getAttribute("icon") || "bi-graph-up";
    const tone = this.getAttribute("tone") || "brand";
    const spark = this.getAttribute("spark") || "";
    const availability = this.getAttribute("availability") || "KPI_AVAILABLE";

    if (availability !== "KPI_AVAILABLE") {
      const a = KPI_AVAIL[availability] || KPI_AVAIL.KPI_INSUFFICIENT_DATA;
      this.innerHTML = `
        <div class="metric-card">
          <div class="metric-label">${label}</div>
          <div class="kpi-unavailable">
            <strong><i class="bi bi-dash-circle"></i> ${a.fr}</strong>
            <span class="mini-note" style="color:inherit">Indicateur non calculable pour l'instant — état métier, pas une erreur.</span>
          </div>
        </div>`;
      return;
    }

    const deltaCls = trend === "down" ? "metric-delta-down" : "metric-delta-up";
    const deltaIcon = trend === "down" ? "bi-arrow-down-right" : "bi-arrow-up-right";
    const sparkEl = spark ? `<pf-sparkline values="${spark}" area></pf-sparkline>` : "";

    this.innerHTML = `
      <div class="metric-card">
        <div class="pf-row-between">
          <span class="metric-icon ${TONE_BG[tone] || TONE_BG.brand}"><i class="bi ${icon}"></i></span>
          ${delta ? `<span class="${deltaCls}"><i class="bi ${deltaIcon}"></i> ${delta}</span>` : ""}
        </div>
        <div class="metric-value">${value}</div>
        <div class="metric-label">${label}</div>
        ${sparkEl}
      </div>`;
  }
}
customElements.define("pf-kpi-card", PfKpiCard);

/* ==================== components/pf-calibration-table.js ==================== */

class PfCalibrationTable extends HTMLElement {
  connectedCallback() {
    let rows = [];
    const json = this.querySelector('script[type="application/json"]');
    if (json) { try { rows = JSON.parse(json.textContent); } catch (_) {} }

    const byLevel = Object.fromEntries(rows.map((r) => [r.level, r]));
    const low = byLevel.LOW?.hitRate ?? 0;
    const high = byLevel.HIGH?.hitRate ?? 0;
    const miscalibrated = high <= low;

    const body = rows.map((r) => {
      const c = CONFIDENCE[r.level] || { fr: r.level };
      return `
        <tr class="calibration-row">
          <td>${c.fr}</td>
          <td>
            <div class="pf-row gap-2">
              <div class="calib-bar" style="flex:1"><span style="width:${r.hitRate}%"></span></div>
              <strong>${r.hitRate} %</strong>
            </div>
          </td>
          <td class="muted">${r.count}</td>
        </tr>`;
    }).join("");

    const verdict = miscalibrated
      ? `<div class="non-exec-panel mt-3"><span class="non-exec-tag">Calibration à revoir</span>
           Le taux d'atteinte « confiance élevée » (${high} %) n'est pas supérieur à « confiance faible » (${low} %).
           <a class="pf-btn pf-btn-sm" href="../admin/scoring-policy.html"><i class="bi bi-ui-checks-grid"></i> Ouvrir la politique de scoring</a></div>`
      : `<div class="why-panel mt-3"><i class="bi bi-check-circle"></i> Calibration cohérente : la confiance élevée atteint la cible plus souvent que la confiance faible.</div>`;

    this.innerHTML = `
      <div class="table-card">
        <table class="pf-table">
          <thead><tr><th>Niveau de confiance</th><th>Taux d'atteinte de cible</th><th>Signaux</th></tr></thead>
          <tbody>${body}</tbody>
        </table>
      </div>
      ${verdict}`;
  }
}
customElements.define("pf-calibration-table", PfCalibrationTable);

/* ==================== components/pf-alert-item.js ==================== */

class PfAlertItem extends HTMLElement {
  connectedCallback() {
    const code = this.getAttribute("trigger") || "PATTERN_STATE_CHANGE";
    const t = ALERT_TRIGGER[code] || ALERT_TRIGGER.PATTERN_STATE_CHANGE;
    const instrument = this.getAttribute("instrument") || "";
    const when = this.getAttribute("when") || "";
    const unread = this.getAttribute("unread") === "true";
    const href = this.getAttribute("href") || `../client/${t.target}.html`;

    this.innerHTML = `
      <div class="alert-item" data-unread="${unread}">
        <span class="alert-icon ${TONE_BG[t.tone] || TONE_BG.info}"><i class="bi ${t.icon}"></i></span>
        <div class="alert-body">
          <div class="pf-row-between">
            <span class="alert-title">${instrument}</span>
            ${when ? `<span class="mini-note">${when}</span>` : ""}
          </div>
          <div class="mt-1"><span class="chip chip-${t.tone === "info" ? "info" : t.tone === "caution" ? "warning" : "neutral"}"><i class="bi ${t.icon}"></i>${t.fr}</span></div>
          <a class="alert-route mt-2 d-inline-flex align-items-center gap-1" href="${href}">
            Ouvrir : ${t.targetLabel} <i class="bi bi-arrow-right-short"></i>
          </a>
        </div>
      </div>`;
  }
}
customElements.define("pf-alert-item", PfAlertItem);

/* ==================== core/state-switcher.js ==================== */

const DEV_STATES = [
  { id: "ready",           label: "Prêt" },
  { id: "empty",           label: "Vide" },
  { id: "loading",         label: "Chargement" },
  { id: "error",           label: "Erreur" },
  { id: "non-executable",  label: "Non-exécutable" },
];
const DEV_CTX = [
  { id: "not-held", label: "Non détenue" },
  { id: "held",     label: "Détenue" },
];

function devParams() { return new URLSearchParams(location.search); }
function currentState() { return devParams().get("state") || "ready"; }
function currentCtx() { return devParams().get("ctx") || "not-held"; }

function applyState(state) {
  document.querySelectorAll("pf-state").forEach((el) => el.setAttribute("state", state));
}

function applyCtx(ctx) {
  document.querySelectorAll("pf-shell").forEach((el) => el.setAttribute("data-ctx", ctx));
  document.body.setAttribute("data-ctx", ctx);
  document.dispatchEvent(new CustomEvent("pf-ctx-change", { detail: { ctx } }));
}

function setParam(key, val) {
  const p = devParams();
  p.set(key, val);
  history.replaceState(null, "", `${location.pathname}?${p.toString()}${location.hash}`);
}

function hasStates() { return document.querySelector("pf-state") != null; }
function hasCtx() { return document.querySelector("[data-pf-ctx-aware]") != null || document.querySelector("pf-recommendation,pf-action-plan") != null; }

function renderDevbar() {
  if (devParams().get("dev") === "0") return;
  if (!hasStates() && !hasCtx()) return;

  const bar = document.createElement("div");
  bar.className = "pf-devbar";
  let html = "";

  if (hasStates()) {
    html += `<span class="pf-devbar-label">État</span>`;
    html += DEV_STATES.map((s) => `<button data-axis="state" data-val="${s.id}">${s.label}</button>`).join("");
  }
  if (hasStates() && hasCtx()) html += `<span class="sep"></span>`;
  if (hasCtx()) {
    html += `<span class="pf-devbar-label">Contexte</span>`;
    html += DEV_CTX.map((c) => `<button data-axis="ctx" data-val="${c.id}">${c.label}</button>`).join("");
  }
  bar.innerHTML = html;
  document.body.appendChild(bar);

  bar.addEventListener("click", (e) => {
    const btn = e.target.closest("button");
    if (!btn) return;
    const axis = btn.dataset.axis, val = btn.dataset.val;
    if (axis === "state") { setParam("state", val); applyState(val); }
    else { setParam("ctx", val); applyCtx(val); }
    syncDevbar(bar);
  });
  syncDevbar(bar);
}

function syncDevbar(bar) {
  const st = currentState(), cx = currentCtx();
  bar.querySelectorAll("button").forEach((b) => {
    const on = (b.dataset.axis === "state" && b.dataset.val === st) ||
               (b.dataset.axis === "ctx" && b.dataset.val === cx);
    b.classList.toggle("active", on);
  });
}

function initStateSwitcher() {
  applyState(currentState());
  applyCtx(currentCtx());
  renderDevbar();
}

/* ==================== core/flow-mode.js ==================== */

function readFlow() {
  const el = document.getElementById("pf-flow");
  if (!el) return null;
  try { return JSON.parse(el.textContent); } catch (_) { return null; }
}

function buildFlowGroup(title, rels) {
  if (!rels || !rels.length) return "";
  const links = rels.map((r) => `<a class="flow-rel-link" href="${r.href}"><i class="bi bi-arrow-right-short"></i>${r.label}</a>`).join("");
  return `<div class="flow-rel-group"><div class="card-eyebrow">${title}</div>${links}</div>`;
}

function buildFlowOverlay(flow) {
  const ov = document.createElement("div");
  ov.className = "flow-overlay";
  ov.id = "pf-flow-overlay";
  ov.innerHTML = `
    <div class="pf-row-between">
      <div class="section-title mb-0"><i class="bi bi-diagram-2"></i> Mode flux</div>
      <button class="pf-iconbtn" data-pf-flow-close aria-label="Fermer"><i class="bi bi-x-lg"></i></button>
    </div>
    <p class="mini-note mt-2">Relations de cet écran (source : spécification écrans). La navigation réelle reste active partout.</p>
    <div class="card-eyebrow mt-3">Écran courant</div>
    <div class="chip chip-navy mt-1"><i class="bi bi-window"></i>${flow.screen || "—"}</div>
    ${buildFlowGroup("Relations entrantes", flow.in)}
    ${buildFlowGroup("Relations sortantes", flow.out)}`;
  document.body.appendChild(ov);
  return ov;
}

function initFlowMode() {
  const flow = readFlow();
  document.addEventListener("click", (e) => {
    const toggle = e.target.closest("[data-pf-flow]");
    const close = e.target.closest("[data-pf-flow-close]");
    if (toggle) {
      if (!flow) { alert("Mode flux : aucune relation déclarée pour cet écran."); return; }
      let ov = document.getElementById("pf-flow-overlay") || buildFlowOverlay(flow);
      ov.classList.toggle("open");
    }
    if (close) {
      const ov = document.getElementById("pf-flow-overlay");
      if (ov) ov.classList.remove("open");
    }
  });
}

/* ==================== boot (pf.js) ==================== */

function boot() {
  initThemeToggle();
  initFlowMode();
  requestAnimationFrame(() => initStateSwitcher());
}

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", boot);
} else {
  boot();
}

/* Expose les utilitaires nécessaires aux scripts inline des pages support */
window.PF = {
  chip,
  PATTERN_STATUS,
  ANALYSIS_OUTCOME,
  CONFIDENCE,
  FRESHNESS,
  RECO,
  HOLDING,
  PEA,
  ALERT_TRIGGER,
  USER_ROLE,
  USER_STATUS,
  KPI_AVAIL,
  initThemeToggle,
  applyStoredTheme,
  toggleTheme,
};

})();
