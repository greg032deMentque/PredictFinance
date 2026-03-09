import { ActivatedRoute } from '@angular/router';

export function getRouteParamDeep(route: ActivatedRoute, key: string): string | null {
  for (let r: ActivatedRoute | null = route; r; r = r.parent) {
    const v = r.snapshot.paramMap.get(key);
    if (v) return v;
  }

  for (let r: ActivatedRoute | null = route; r; r = r.parent) {
    const v = r.snapshot.queryParamMap.get(key);
    if (v) return v;
  }

  return null;
}
