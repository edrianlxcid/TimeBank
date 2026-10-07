import { Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { Category, TimeBankApiService } from '../../services/timebank-api.service';

// Página principal después de iniciar sesión
@Component({
  selector: 'app-inicio',
  imports: [DecimalPipe],
  templateUrl: './inicio.html',
  styleUrl: './inicio.css'
})
export class Inicio implements OnInit {
  protected readonly auth = inject(AuthService);
  private readonly api = inject(TimeBankApiService);

  protected readonly categories = signal<Category[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal('');

  ngOnInit(): void {
    // Perfil actualizado (GET /api/auth/me, va con el token gracias al interceptor)
    this.auth.refreshProfile().subscribe({ error: err => console.error('Error al leer el perfil:', err) });

    // Categorías desde la API (lo mismo de la Actividad 4.3)
    this.api.getCategories().subscribe({
      next: data => {
        console.log('Categorías recibidas:', data);
        this.categories.set(data);
        this.loading.set(false);
      },
      error: err => {
        console.error('Error conectando con la API:', err);
        this.error.set('No se pudo conectar con el backend de TimeBank.');
        this.loading.set(false);
      }
    });
  }
}
