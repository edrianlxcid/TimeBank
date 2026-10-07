import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

// Se ejecuta en CADA petición HTTP:
// 1) agrega el encabezado "Authorization: Bearer <token>" si hay sesión
// 2) si la API responde 401 (token vencido o sesión cerrada), borra la sesión y manda al login
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const token = auth.getToken();
  const request = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      const isLoginRequest = req.url.endsWith('/auth/login');
      if (error.status === 401 && !isLoginRequest) {
        auth.clearSession();
        router.navigate(['/login']);
      }
      return throwError(() => error);
    })
  );
};
