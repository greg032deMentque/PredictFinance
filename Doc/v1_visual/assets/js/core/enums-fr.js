/* ============================================================
   enums-fr.js — Source UNIQUE du wording FR canonique.
   Aligné sur Doc/v1/02_glossaire_et_taxonomies.md.
   Règle : les codes internes (anglais) ne fuitent JAMAIS en UI.
   Chaque entrée : { fr, tone, icon } pour piloter texte + couleur + icône
   (jamais de couleur seule).
   tone ∈ positive | info | caution | blocked | neutral | brand
   ============================================================ */

export const TONE_CLASS = {
  positive: "success",
  info: "info",
  caution: "warning",
  blocked: "danger",
  neutral: "neutral",
  brand: "navy",
};

/* ---- AnalysisOutcome (issue métier de 1er rang) ---- */
export const ANALYSIS_OUTCOME = {
  CrediblePatternFound:       { fr: "Pattern crédible détecté",            tone: "positive", icon: "bi-check-circle-fill", executable: true },
  MultipleCompatiblePatterns: { fr: "Plusieurs patterns compatibles",      tone: "info",     icon: "bi-diagram-3-fill",   executable: true },
  NoCrediblePattern:          { fr: "Aucun pattern crédible retenu",       tone: "caution",  icon: "bi-slash-circle",     executable: false },
  InsufficientData:           { fr: "Données insuffisantes pour analyser", tone: "caution",  icon: "bi-hourglass-split",  executable: false },
  UnsupportedInstrument:      { fr: "Instrument hors périmètre V1",        tone: "blocked",  icon: "bi-x-octagon",        executable: false },
  UnsupportedContext:         { fr: "Contexte non pris en charge",         tone: "blocked",  icon: "bi-x-octagon",        executable: false },
};

/* ---- PatternStatus ---- */
export const PATTERN_STATUS = {
  Forming:     { fr: "En formation",  tone: "info",     icon: "bi-circle-half" },
  Monitoring:  { fr: "À surveiller",  tone: "caution",  icon: "bi-eye" },
  Confirmed:   { fr: "Confirmé",      tone: "positive", icon: "bi-check-circle" },
  Invalidated: { fr: "Invalidé",      tone: "blocked",  icon: "bi-x-circle" },
  Completed:   { fr: "Terminé",       tone: "neutral",  icon: "bi-flag" },
};

/* ---- ConfidenceLabel ---- */
export const CONFIDENCE = {
  LOW:    { fr: "Confiance faible", tone: "caution",  icon: "bi-reception-1" },
  MEDIUM: { fr: "Confiance moyenne", tone: "info",    icon: "bi-reception-2" },
  HIGH:   { fr: "Confiance élevée", tone: "positive", icon: "bi-reception-4" },
};

/* ---- HoldingContext ---- */
export const HOLDING = {
  NOT_HELD: { fr: "Non détenue", icon: "bi-eye" },
  HELD:     { fr: "Détenue",     icon: "bi-wallet2" },
};

/* ---- RecommendationKind (verbe + partition détenu/non-détenu) ---- */
export const RECO = {
  Monitor:   { fr: "Surveiller", tone: "caution",  icon: "bi-eye",            contexts: ["NOT_HELD"] },
  Wait:      { fr: "Attendre",   tone: "caution",  icon: "bi-hourglass",      contexts: ["NOT_HELD", "HELD"] },
  Buy:       { fr: "Acheter",    tone: "positive", icon: "bi-cart-plus",      contexts: ["NOT_HELD"] },
  Hold:      { fr: "Conserver",  tone: "info",     icon: "bi-shield-check",   contexts: ["HELD"] },
  Reinforce: { fr: "Renforcer",  tone: "positive", icon: "bi-plus-circle",    contexts: ["HELD"] },
  Lighten:   { fr: "Alléger",    tone: "caution",  icon: "bi-dash-circle",    contexts: ["HELD"] },
  Sell:      { fr: "Vendre",     tone: "blocked",  icon: "bi-box-arrow-right", contexts: ["HELD"] },
};
// Surveiller (Monitor) n'est JAMAIS une reco finale en contexte détenu (RM-10).
export const RECO_ALLOWED = {
  NOT_HELD: ["Monitor", "Wait", "Buy"],
  HELD: ["Hold", "Reinforce", "Lighten", "Sell", "Wait"],
};

/* ---- PeaEligibilityStatus (Unknown JAMAIS positif) ---- */
export const PEA = {
  ConfirmedEligible:   { fr: "Éligibilité PEA confirmée",    status: "eligible",   icon: "bi-check-circle" },
  ConfirmedIneligible: { fr: "Non éligible PEA confirmée",   status: "ineligible", icon: "bi-x-circle" },
  Unknown:             { fr: "Éligibilité PEA non confirmée", status: "unknown",   icon: "bi-question-circle" },
};

/* ---- SignalOutcome (issue réalisée ex post — admin) ---- */
export const SIGNAL_OUTCOME = {
  TARGET_HIT:       { fr: "Cible atteinte",       tone: "positive", icon: "bi-bullseye" },
  INVALIDATION_HIT: { fr: "Invalidation touchée", tone: "blocked",  icon: "bi-x-circle" },
  STILL_OPEN:       { fr: "En cours",             tone: "info",     icon: "bi-arrow-repeat" },
  NOT_EVALUABLE:    { fr: "Non évaluable",        tone: "neutral",  icon: "bi-dash-circle" },
};

/* ---- Disponibilité du score composite ---- */
export const COMPOSITE_AVAIL = {
  AVAILABLE:                         { fr: "Score composite disponible", tone: "positive", icon: "bi-check-circle" },
  INSUFFICIENT_COVERAGE:             { fr: "Score composite indisponible : couverture de données insuffisante", tone: "caution", icon: "bi-exclamation-circle" },
  PEA_UNKNOWN_BLOCKING:              { fr: "Score composite indisponible : éligibilité PEA non confirmée", tone: "caution", icon: "bi-exclamation-circle" },
  CONFIRMED_INELIGIBLE_IN_UNIVERSE:  { fr: "Score composite indisponible : instrument confirmé non éligible PEA dans cet univers", tone: "caution", icon: "bi-exclamation-circle" },
  UNSUPPORTED_UNIVERSE:              { fr: "Score composite indisponible : univers demandé non pris en charge", tone: "blocked", icon: "bi-x-octagon" },
  PROVIDER_DATA_INCOMPLETE:          { fr: "Score composite indisponible : données fournisseur incomplètes ou indisponibles", tone: "caution", icon: "bi-exclamation-circle" },
};

/* ---- Fraîcheur des données ---- */
export const FRESHNESS = {
  FRESH:   { fr: "Données à jour",      tone: "positive", icon: "bi-check-circle" },
  AGING:   { fr: "Données à surveiller", tone: "caution", icon: "bi-clock-history" },
  STALE:   { fr: "Données obsolètes",   tone: "blocked",  icon: "bi-exclamation-triangle" },
  MISSING: { fr: "Données indisponibles", tone: "neutral", icon: "bi-dash-circle" },
};

/* ---- Catégories de notification ---- */
export const NOTIF_CATEGORY = {
  Watchlist: { fr: "Watchlist",     icon: "bi-stars" },
  Analysis:  { fr: "Analyse",       icon: "bi-graph-up" },
  Learning:  { fr: "Apprentissage", icon: "bi-mortarboard" },
  Account:   { fr: "Compte",        icon: "bi-person" },
};

/* ---- Déclencheurs d'alerte (AlertTrigger) — route, n'explique pas ---- */
export const ALERT_TRIGGER = {
  PATTERN_STATE_CHANGE: { fr: "Changement d'état de pattern", tone: "info",    icon: "bi-arrow-left-right", target: "analysis-result",   targetLabel: "Résultat d'analyse" },
  LEVEL_CROSSED:        { fr: "Niveau franchi",               tone: "caution", icon: "bi-graph-up-arrow",   target: "instrument-detail", targetLabel: "Détail instrument" },
  DATA_STALE:           { fr: "Données obsolètes",            tone: "neutral", icon: "bi-clock-history",    target: "instrument-detail", targetLabel: "Détail instrument" },
};

/* ---- UserRole / UserStatus ---- */
export const USER_ROLE = {
  User:       { fr: "Utilisateur",         space: "client" },
  Admin:      { fr: "Administrateur",       space: "admin" },
  SuperAdmin: { fr: "Super-administrateur", space: "admin" },
};
export const USER_STATUS = {
  ACTIVE:   { fr: "Actif",      tone: "positive", icon: "bi-check-circle" },
  PENDING:  { fr: "En attente", tone: "caution",  icon: "bi-hourglass" },
  DISABLED: { fr: "Désactivé",  tone: "blocked",  icon: "bi-x-circle" },
};

/* ---- Disponibilité d'un KPI (état métier admin) ---- */
export const KPI_AVAIL = {
  KPI_AVAILABLE:        { fr: "Indicateur disponible" },
  KPI_INSUFFICIENT_DATA: { fr: "Données insuffisantes sur la période" },
  KPI_WINDOW_TOO_YOUNG: { fr: "Fenêtre trop récente pour ce calcul" },
};

/* ---- Catégories fondamentales ---- */
export const FUNDAMENTAL_CATEGORY = {
  Profitability:     "Rentabilité",
  Valuation:         "Valorisation",
  FinancialStrength: "Solidité financière",
  Growth:            "Croissance",
  Income:            "Rendement",
};

/* ---- Patterns V1 (continuation) ---- */
export const PATTERN = {
  RECTANGLE_CONTINUATION:            { fr: "Rectangle de continuation" },
  SYMMETRICAL_TRIANGLE_CONTINUATION: { fr: "Triangle symétrique de continuation" },
  BULL_FLAG_CONTINUATION:            { fr: "Drapeau haussier" },
  BEAR_FLAG_CONTINUATION:            { fr: "Drapeau baissier" },
};

/* ---- Étapes de plan d'action (kind) ---- */
export const ACTION_STEP = {
  NOTE_LEVEL:       { icon: "bi-bookmark-star" },
  REVIEW_AT:        { icon: "bi-calendar-check" },
  SET_ALERT:        { icon: "bi-bell" },
  HOLDING_REMINDER: { icon: "bi-wallet2" },
  WAIT_FOR_DATA:    { icon: "bi-hourglass-split" },
};

/* Helper : rendu d'un chip de statut homogène (couleur + icône + texte FR) */
export function chip(entry, { outline = false } = {}) {
  if (!entry) return "";
  const cls = TONE_CLASS[entry.tone] || "neutral";
  return `<span class="chip chip-${cls}${outline ? " chip-outline" : ""}"><i class="bi ${entry.icon}"></i>${entry.fr}</span>`;
}
