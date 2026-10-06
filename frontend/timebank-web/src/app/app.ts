import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TimeBankApiService } from './services/timebank-api.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  // Servicio para comunicarnos con el backend
  private readonly api = inject(TimeBankApiService);

  // Datos que se muestran en la plantilla (signals para que la vista se actualice sola)
  protected readonly categories = signal<any[]>([]);
  protected readonly error = signal('');
  protected readonly loading = signal(true);

  // Angular ejecuta este método cuando el componente se inicializa
  ngOnInit(): void {
    this.api.getCategories().subscribe({
      next: (data) => {
        console.log('Categorías recibidas:', data);
        this.categories.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error conectando con la API:', err);
        this.error.set('No se pudo conectar con el backend de TimeBank.');
        this.loading.set(false);
      }
    });
  }
}
