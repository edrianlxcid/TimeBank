import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UserProfile } from './auth.service';

export interface Category {
  id: number;
  name: string;
  description: string | null;
}

// Capa de comunicación entre Angular y la API de TimeBank
@Injectable({ providedIn: 'root' })
export class TimeBankApiService {
  // private: solo se usa dentro de esta clase; readonly: no se puede reasignar
  private readonly http = inject(HttpClient);

  // URL base de la API (backend ASP.NET Core)
  private readonly apiUrl = 'http://localhost:5070/api';

  // GET /api/categories: lista de categorías (pública)
  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.apiUrl}/categories`);
  }

  // GET /api/users: lista de usuarios (solo Administrador; el token lo agrega el interceptor)
  getUsers(): Observable<UserProfile[]> {
    return this.http.get<UserProfile[]>(`${this.apiUrl}/users`);
  }
}
