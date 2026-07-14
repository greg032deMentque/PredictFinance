/* theme.js — bascule clair/sombre, persistée en localStorage.
   Appliquée tôt (avant render) pour éviter le flash. */

const KEY = "pf-theme";

export function applyStoredTheme() {
  let theme = "light";
  try { theme = localStorage.getItem(KEY) || "light"; } catch (_) {}
  document.documentElement.setAttribute("data-theme", theme);
  return theme;
}

export function toggleTheme() {
  const cur = document.documentElement.getAttribute("data-theme") || "light";
  const next = cur === "dark" ? "light" : "dark";
  document.documentElement.setAttribute("data-theme", next);
  try { localStorage.setItem(KEY, next); } catch (_) {}
  syncIcons(next);
}

function syncIcons(theme) {
  document.querySelectorAll("[data-pf-theme] i").forEach((i) => {
    i.className = theme === "dark" ? "bi bi-sun" : "bi bi-moon-stars";
  });
}

export function initThemeToggle() {
  const theme = document.documentElement.getAttribute("data-theme") || "light";
  syncIcons(theme);
  document.addEventListener("click", (e) => {
    const btn = e.target.closest("[data-pf-theme]");
    if (btn) toggleTheme();
  });
}

// applique immédiatement à l'import
applyStoredTheme();
