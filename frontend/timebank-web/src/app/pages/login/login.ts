import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';

// Pantalla de inicio de sesión
@Component({
  selector: 'app-login',
  imports: [FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  // Datos del formulario (se conectan con [(ngModel)])
  email = '';
  password = '';
  showPassword = false;

  // Estado de la pantalla
  protected readonly errorMessage = signal('');
  protected readonly loading = signal(false);

  // Se ejecuta al enviar el formulario
  login(): void {
    // 1. Limpia el error del intento anterior
    this.errorMessage.set('');

    // 2. Validación básica antes de llamar a la API
    if (!this.email.trim() || !this.password) {
      this.errorMessage.set('Ingresa tu correo y contraseña.');
      return;
    }

    // 3. Llama a la API; mientras espera, el botón queda deshabilitado
    this.loading.set(true);
    this.authService.login(this.email.trim(), this.password).subscribe({
      next: response => {
        console.log('Login exitoso:', response.user);
        this.loading.set(false);
        this.router.navigate(['/inicio']);
      },
      error: (error: HttpErrorResponse) => {
        console.error('Error en el login:', error);
        this.loading.set(false);

        // 4. Mensajes para el usuario, sin errores técnicos
        if (error.status === 401) {
          this.errorMessage.set('Correo o contraseña incorrectos.');
        } else if (error.status === 400) {
          this.errorMessage.set('Revisa el formato del correo.');
        } else if (error.status === 0) {
          this.errorMessage.set('No se pudo conectar con el servidor. Revisa que la API esté encendida.');
        } else {
          this.errorMessage.set(error.error?.message ?? 'No se pudo iniciar sesión. Intenta de nuevo.');
        }
      }
    });
  }
}
