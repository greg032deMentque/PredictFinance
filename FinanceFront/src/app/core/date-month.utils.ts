/*
 * date-month.utils.ts
 * Utilitaires de parsing/formatage des dates orientÃ©s "sÃ©lection de mois" (YYYY-MM)
 * ConÃ§us pour Ãªtre agnostiques et rÃ©utilisables dans des formulaires Angular.
 */

import { AbstractControl, ValidationErrors, ValidatorFn } from "@angular/forms";

/** Parse "dd/MM/yyyy" -> Date (null si invalide) */
export function parseDateFr(s?: string | null): Date | null {
    if (!s) return null;
    const m = s.trim().match(/^(\d{1,2})[/-](\d{1,2})[/-](\d{4})$/);
    if (!m) return null;
    const day = Number(m[1]), month = Number(m[2]), year = Number(m[3]);
    const d = new Date(year, month - 1, day);
    return (d.getFullYear() === year && d.getMonth() === month - 1 && d.getDate() === day) ? d : null;
}

/** Parse "yyyy-MM-dd" -> Date (null si invalide) */
export function parseDateInput(s?: string | null): Date | null {
    if (!s) return null;
    const [yStr, mStr, dStr] = s.split("-");
    const y = Number(yStr), m = Number(mStr), d = Number(dStr);
    if (!y || !m || !d) return null;
    const dt = new Date(y, m - 1, d);
    return (dt.getFullYear() === y && dt.getMonth() === m - 1 && dt.getDate() === d) ? dt : null;
}

/** Padding sur 2 chiffres */
export function pad2(n: number): string { return n.toString().padStart(2, "0"); }

/** Pattern d'un input mois HTML (YYYY-MM) */
export const MONTH_INPUT_PATTERN = /^(\d{4})-(0[1-9]|1[0-2])$/;

/**
 * Normalise une valeur en chaÃ®ne d'input mois ("YYYY-MM").
 * Accepte: Date | "YYYY-MM" | "YYYY-MM-DD" | "dd/MM/yyyy".
 */
export function toInputMonth(value?: string | Date | null): string | null {
  if (!value) return null;

  if (typeof value === "string") {
    if (MONTH_INPUT_PATTERN.test(value)) return value; // deja au format YYYY-MM
    const d =
      parseDateInput(value) ??
      parseDateFr(value) ??
      new Date(value); // <= fallback pour "2024-01-01T00:00:00"

    return isNaN(d.getTime()) ? null : `${d.getFullYear()}-${pad2(d.getMonth() + 1)}`;
  }

  const d = value instanceof Date ? value : new Date(value);
  return isNaN(d.getTime()) ? null : `${d.getFullYear()}-${pad2(d.getMonth() + 1)}`;
}

/** Formate un Date -> "YYYY-MM" (mois de l'objet en timezone locale) */
export function formatMonthInput(d: Date): string {
    return `${d.getFullYear()}-${pad2(d.getMonth() + 1)}`;
}

/** Parse "YYYY-MM" -> { y, m } (1..12) */
export function parseMonthInput(s?: string | null): { y: number; m: number } | null {
    if (!s) return null;
    const m = s.match(MONTH_INPUT_PATTERN);
    if (!m) return null;
    const y = Number(m[1]), mo = Number(m[2]);
    return { y, m: mo };
}

/** ClÃ© d'ordonnancement pour comparer des mois */
export function monthKey(y: number, m: number): number {
    return y * 12 + (m - 1);
}

/** 1er jour UTC du mois (date-only) */
export function monthToUtcStart(y: number, m: number): Date {
    return new Date(Date.UTC(y, m - 1, 1));
}

/** Dernier jour UTC du mois (date-only) */
export function monthToUtcEnd(y: number, m: number): Date {
    // Jour 0 du mois suivant = dernier jour du mois courant
    return new Date(Date.UTC(y, m, 0));
}

/**
 * Validator cross-field (form group): impose End >= Start au niveau mois.
 * @example
 * this.form = this.fb.group({ StartDate: [null, Validators.required], EndDate: [null, Validators.required] }, {
 *   validators: monthRangeValidator({ startKey: 'StartDate', endKey: 'EndDate' })
 * });
 */
export function monthRangeValidator(opts?: { startKey?: string; endKey?: string; }): ValidatorFn {
    const startKey = opts?.startKey ?? 'start';
    const endKey = opts?.endKey ?? 'end';

    return (group: AbstractControl): ValidationErrors | null => {
        const start = group.get(startKey)?.value as string | null | undefined;
        const end = group.get(endKey)?.value as string | null | undefined;
        if (!start || !end) return null; // la rÃ¨gle "required" reste sur les contrÃ´les
        const s = parseMonthInput(start);
        const e = parseMonthInput(end);
        if (!s || !e) return { invalidRange: true };
        return monthKey(e.y, e.m) >= monthKey(s.y, s.m) ? null : { invalidRange: true };
    };
}
