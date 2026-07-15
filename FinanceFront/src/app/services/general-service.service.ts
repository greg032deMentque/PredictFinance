import { inject, Injectable, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AppRoutes } from '../Routes/app.routes.constants';


@Injectable({
  providedIn: 'root'
})
export class GeneralService {
  private readonly router = inject(Router);
  lastSyncError = signal<unknown | null>(null);
  lastSyncAt = signal<number | null>(null);


  logout() {
    sessionStorage.clear();
    this.router.navigate([AppRoutes.Login]);
  }


  static getRouteParamDeep(key: string, route: ActivatedRoute): string | null {
    let currentRoute: ActivatedRoute | null = route;
    while (currentRoute) {
      const value = currentRoute.snapshot.paramMap.get(key);
      if (value) return value;
      currentRoute = currentRoute.parent;
    }

    currentRoute = route;
    while (currentRoute) {
      const value = currentRoute.snapshot.queryParamMap.get(key);
      if (value) return value;
      currentRoute = currentRoute.parent;
    }

    return null;
  }


}

