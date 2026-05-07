import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { TokenService } from './token.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokens = inject(TokenService);
  const auth = inject(AuthService);
  const token = tokens.accessToken();
  const authReq = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || req.url.includes('/auth/refresh')) {
        return throwError(() => error);
      }

      return auth.refresh().pipe(
        switchMap(() => {
          const refreshed = tokens.accessToken();
          const retry = refreshed ? req.clone({ setHeaders: { Authorization: `Bearer ${refreshed}` } }) : req;
          return next(retry);
        })
      );
    })
  );
};
