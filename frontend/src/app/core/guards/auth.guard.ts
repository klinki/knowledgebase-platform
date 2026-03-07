import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.ensureSessionResolved().then((status) =>
    status === 'authenticated'
      ? true
      : router.createUrlTree(['/login'], {
          queryParams: { returnUrl: state.url }
        }));
};
