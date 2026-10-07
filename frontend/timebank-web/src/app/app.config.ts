import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideClientHydration } from '@angular/platform-browser';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { authInterceptor } from './interceptors/auth.interceptor';

// Configuración global de la aplicación Angular
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(), // manejadores globales de errores del navegador
    provideRouter(routes),                // sistema de rutas (app.routes.ts)
    provideClientHydration(),             // hidratación del contenido renderizado en el servidor (SSR)
    // HttpClient para hacer GET, POST, PUT y DELETE; el interceptor agrega el token a cada petición
    provideHttpClient(withFetch(), withInterceptors([authInterceptor]))
  ]
};
