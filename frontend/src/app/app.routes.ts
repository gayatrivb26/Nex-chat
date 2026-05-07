import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'auth',
    loadComponent: () => import('./features/auth/auth-shell.component').then(m => m.AuthShellComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./features/chat/chat-shell.component').then(m => m.ChatShellComponent)
  },
  { path: '**', redirectTo: '' }
];
