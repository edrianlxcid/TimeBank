import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { UserProfile } from '../../services/auth.service';
import { TimeBankApiService } from '../../services/timebank-api.service';

// Lista de usuarios: solo el Administrador entra aquí (roleGuard en app.routes.ts)
@Component({
  selector: 'app-usuarios',
  imports: [DatePipe, DecimalPipe],
  templateUrl: './usuarios.html',
  styleUrl: './usuarios.css'
})
export class Usuarios implements OnInit {
  private readonly api = inject(TimeBankApiService);

  protected readonly users = signal<UserProfile[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal('');

  ngOnInit(): void {
    this.api.getUsers().subscribe({
      next: data => {
        this.users.set(data);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        console.error('Error al cargar usuarios:', err);
        this.loading.set(false);
        this.error.set(err.status === 403
          ? 'No tienes permiso para ver esta información.'
          : 'No se pudo cargar la lista de usuarios.');
      }
    });
  }
}
