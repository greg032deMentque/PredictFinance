import { DestroyRef, EventEmitter, inject, Injectable, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AppRoutes } from '../Routes/app.routes.constants';
import { environment } from '../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, finalize, firstValueFrom, lastValueFrom, Observable, shareReplay, tap, throwError } from 'rxjs';


@Injectable({
  providedIn: 'root'
})
export class GeneralService {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  lastSyncError = signal<unknown | null>(null);
  lastSyncAt = signal<number | null>(null);


  constructor(private router: Router) { }


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

