import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject, Injector } from '@angular/core';
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';

function isApiRequest(url: string): boolean {
  if (environment.apiBaseUrl.startsWith('http')) {
    return url.startsWith(environment.apiBaseUrl);
  }

  return url.startsWith(environment.apiBaseUrl) || url.startsWith(`/api/`);
}

function shouldHandleUnauthorized(url: string): boolean {
  return !url.includes('/auth/login') && !url.includes('/auth/me');
}

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const injector = inject(Injector);
  const requestToSend = isApiRequest(request.url)
    ? request.clone({ withCredentials: true })
    : request;

  return next(requestToSend).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse &&
          error.status === 401 &&
          isApiRequest(request.url) &&
          shouldHandleUnauthorized(request.url)) {
        const authService = injector.get(AuthService);
        void authService.handleUnauthorized();
      }

      return throwError(() => error);
    })
  );
};
