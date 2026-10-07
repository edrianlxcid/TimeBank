import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

// Solo deja entrar si hay sesión iniciada; si no, manda al login
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isLoggedIn() ? true : router.parseUrl('/login');
};

// El login solo se muestra si NO hay sesión; si ya inició sesión, va directo al inicio
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isLoggedIn() ? router.parseUrl('/inicio') : true;
};

// Solo deja entrar a los roles indicados en la ruta (data: { roles: [...] })
export const roleGuard: CanActivateFn = route => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const allowed: string[] = route.data?.['roles'] ?? [];

  if (!auth.isLoggedIn()) return router.parseUrl('/login');
  return allowed.some(role => auth.hasRole(role)) ? true : router.parseUrl('/inicio');
};
