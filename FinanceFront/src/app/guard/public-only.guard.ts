import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs/operators';
import { AdminPaths, UserPaths } from '../Routes/app.routes.constants';
import { AuthService } from '../services/AuthService.service';

export const publicOnlyGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.ensureValidAccessToken().pipe(
    map((token) => {
      if (!token) {
        return true;
      }

      return router.createUrlTree([
        authService.isAdmin(token) ? AdminPaths.Dashboard : UserPaths.Dashboard
      ]);
    })
  );
};
