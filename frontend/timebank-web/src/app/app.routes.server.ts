import { RenderMode, ServerRoute } from '@angular/ssr';

// El login usa localStorage (solo existe en el navegador),
// así que las páginas se dibujan en el cliente y no en el servidor.
export const serverRoutes: ServerRoute[] = [
  {
    path: '**',
    renderMode: RenderMode.Client
  }
];
