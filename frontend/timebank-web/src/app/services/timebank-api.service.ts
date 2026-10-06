import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// Capa de comunicación entre Angular y la API de TimeBank
@Injectable({ providedIn: 'root' })
export class TimeBankApiService {
  // private: solo se usa dentro de esta clase; readonly: no se puede reasignar
  private readonly http = inject(HttpClient);

  // URL base de la API (backend ASP.NET Core)
  private readonly apiUrl = 'http://localhost:5070/api';

  // GET /api/categories: lista de categorías
  getCategories(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/categories`);
  }
}
