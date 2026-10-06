import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideClientHydration } from '@angular/platform-browser';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { routes } from './app.routes';

// Configuración global de la aplicación Angular
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(), // manejadores globales de errores del navegador
    provideRouter(routes),                // sistema de rutas (app.routes.ts)
    provideClientHydration(),             // hidratación del contenido renderizado en el servidor (SSR)
    provideHttpClient(withFetch())        // HttpClient para hacer GET, POST, PUT y DELETE a la API
  ]
};
