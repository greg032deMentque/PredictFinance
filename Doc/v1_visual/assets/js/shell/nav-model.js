export const NAV = {
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
    { id: "account-profile", href: "../client/account-profile.html", icon: "bi-person-gear", label: "Compte" },
    { group: "Cibles spec" },
    { id: "target-onboarding", href: "../client/target-onboarding-empty.html", icon: "bi-rocket-takeoff", label: "Onboarding cible" },
    { id: "target-learn", href: "../client/target-learn.html", icon: "bi-mortarboard", label: "Learn cible" },
    { id: "target-help", href: "../client/target-help-center.html", icon: "bi-life-preserver", label: "Aide cible" }
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
    { id: "data-quality", href: "../admin/data-quality.html", icon: "bi-database-check", label: "Data quality" },
    { group: "Cibles KPI" },
    { id: "target-signal-quality", href: "../admin/target-signal-quality.html", icon: "bi-graph-up-arrow", label: "Signal quality cible" },
    { id: "target-engagement", href: "../admin/target-engagement.html", icon: "bi-people", label: "Engagement cible" }
  ]
};

export const MOBILE_NAV = {
  client: ["dashboard", "watchlist", "analysis-entry", "notifications", "account-profile"],
  admin: ["dashboard", "users", "instrument-registry", "snapshot-audit", "data-quality"]
};

export const SPACE_META = {
  client: { roleLabel: "Espace client", roleIcon: "bi-person-badge", spaceLabel: "Investisseur" },
  admin: { roleLabel: "Espace admin", roleIcon: "bi-shield-lock", spaceLabel: "Gouvernance" }
};
