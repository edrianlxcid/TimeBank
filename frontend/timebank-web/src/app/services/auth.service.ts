import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Observable, finalize, tap } from 'rxjs';

// Datos del usuario que devuelve la API (UserProfileDto). Nunca incluye la contraseña.
export interface UserProfile {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phone: string | null;
  hoursBalance: number;
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string | null;
  roles: string[];
}

// Respuesta de POST /api/auth/login (AuthResponseDto)
export interface LoginResponse {
  token: string;
  tokenType: string;
  expiresAt: string;
  user: UserProfile;
}

// Nombres de los roles, iguales a los del backend (AppRoles.cs)
export const ROLES = {
  admin: 'Administrador',
  usuario: 'Usuario'
} as const;

const TOKEN_KEY = 'token';
const USER_KEY = 'user';
const EXPIRES_KEY = 'expiresAt';

// Servicio de autenticación: inicia y cierra sesión y guarda el token en el navegador
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5070/api/auth';

  // localStorage solo existe en el navegador; en el servidor (SSR) no se usa
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  // Usuario con sesión iniciada (signal para que el menú se actualice solo).
  // Si el token ya venció, se empieza sin sesión.
  readonly currentUser = signal<UserProfile | null>(this.isLoggedIn() ? this.readUser() : null);

  // POST /api/auth/login: envía correo y contraseña; si son correctos guarda el token y el usuario
  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, { email, password }).pipe(
      tap(response => {
        this.storage?.setItem(TOKEN_KEY, response.token);
        this.storage?.setItem(EXPIRES_KEY, response.expiresAt);
        this.storage?.setItem(USER_KEY, JSON.stringify(response.user));
        this.currentUser.set(response.user);
      })
    );
  }

  // DELETE /api/auth/logout: el backend revoca la sesión y aquí se borran los datos guardados
  logout(): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/logout`).pipe(
      finalize(() => this.clearSession())
    );
  }

  // GET /api/auth/me: vuelve a leer el perfil (por ejemplo, el saldo de horas actualizado)
  refreshProfile(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.apiUrl}/me`).pipe(
      tap(user => {
        this.storage?.setItem(USER_KEY, JSON.stringify(user));
        this.currentUser.set(user);
      })
    );
  }

  // Borra el token y el usuario del navegador (también se usa cuando la API responde 401)
  clearSession(): void {
    this.storage?.removeItem(TOKEN_KEY);
    this.storage?.removeItem(EXPIRES_KEY);
    this.storage?.removeItem(USER_KEY);
    this.currentUser.set(null);
  }

  getToken(): string | null {
    return this.storage?.getItem(TOKEN_KEY) ?? null;
  }

  // Hay sesión si existe un token y todavía no venció (el token dura 2 horas)
  isLoggedIn(): boolean {
    const token = this.getToken();
    const expiresAt = this.storage?.getItem(EXPIRES_KEY);
    if (!token || !expiresAt) return false;
    return new Date(expiresAt).getTime() > Date.now();
  }

  hasRole(role: string): boolean {
    return this.currentUser()?.roles.includes(role) ?? false;
  }

  isAdmin(): boolean {
    return this.hasRole(ROLES.admin);
  }

  private get storage(): Storage | null {
    return this.isBrowser ? localStorage : null;
  }

  // localStorage guarda texto; JSON.parse lo convierte otra vez en objeto
  private readUser(): UserProfile | null {
    const user = this.storage?.getItem(USER_KEY);
    return user ? JSON.parse(user) as UserProfile : null;
  }
}
