import { Routes } from '@angular/router';
import { Login } from './pages/login/login';
import { Inicio } from './pages/inicio/inicio';
import { Usuarios } from './pages/usuarios/usuarios';
import { authGuard, guestGuard, roleGuard } from './guards/auth.guard';
import { ROLES } from './services/auth.service';

// Rutas de la aplicación
export const routes: Routes = [
  { path: 'login', component: Login, canActivate: [guestGuard] },
  { path: 'inicio', component: Inicio, canActivate: [authGuard] },
  // Solo el Administrador puede ver la lista de usuarios
  { path: 'usuarios', component: Usuarios, canActivate: [roleGuard], data: { roles: [ROLES.admin] } },
  // localhost:4200 abre el login
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  // Cualquier ruta que no exista vuelve al login
  { path: '**', redirectTo: 'login' }
];
