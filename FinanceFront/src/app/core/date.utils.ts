export type DateLike = string | Date | null | undefined;

export function toYMD(dateLike: string | Date): string {
  const d = new Date(dateLike);
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

export function mondayOf(dateLike: string | Date): Date {
  const d = new Date(dateLike);
  const day = d.getDay() || 7;
  if (day !== 1) d.setDate(d.getDate() + (1 - day));
  d.setHours(0, 0, 0, 0);
  return d;
}

export function sundayOfMonday(mondayDate: Date): Date {
  const sunday = new Date(mondayDate);
  sunday.setDate(sunday.getDate() + 6);
  sunday.setHours(0, 0, 0, 0);
  return sunday;
}

export function formatDMY(date: Date): string {
  const day = String(date.getDate()).padStart(2, '0');
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const year = date.getFullYear();
  return `${day}-${month}-${year}`;
}

export function ts(d: DateLike): number {
  if (!d) return NaN;
  if (d instanceof Date) return d.getTime();

  if (/^\d{4}-\d{2}-\d{2}/.test(d)) return new Date(d).getTime();

  const m = d.match(/^(\d{2})[-/](\d{2})[-/](\d{4})$/);
  if (m) {
    const [, dd, mm, yyyy] = m;
    return new Date(Number(yyyy), Number(mm) - 1, Number(dd)).getTime();
  }

  return Date.parse(d);
}

export function cmpDescByPeriod<
  T extends { StartDate?: DateLike; EndDate?: DateLike }
>(a: T, b: T): number {
  const aEnd = a.EndDate ? ts(a.EndDate) : Number.POSITIVE_INFINITY;
  const bEnd = b.EndDate ? ts(b.EndDate) : Number.POSITIVE_INFINITY;
  if (aEnd !== bEnd) return bEnd - aEnd;

  const aStart = a.StartDate ? ts(a.StartDate) : 0;
  const bStart = b.StartDate ? ts(b.StartDate) : 0;
  return bStart - aStart;
}
