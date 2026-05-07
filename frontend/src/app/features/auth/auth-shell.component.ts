import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  standalone: true,
  selector: 'app-auth-shell',
  imports: [FormsModule],
  template: `
    <main class="auth">
      <section class="panel">
        <h1>NexChat</h1>
        <form (ngSubmit)="login()">
          <input name="phone" [(ngModel)]="phone" placeholder="Phone" autocomplete="tel" />
          <input name="password" [(ngModel)]="password" placeholder="Password" type="password" autocomplete="current-password" />
          <input name="totp" [(ngModel)]="totpCode" placeholder="2FA code" autocomplete="one-time-code" />
          <button type="submit" [disabled]="isLoading()">Sign in</button>
        </form>
        @if (error()) { <p class="error">{{ error() }}</p> }
      </section>
    </main>
  `,
  styles: [`
    .auth { min-height: 100vh; display: grid; place-items: center; padding: 24px; }
    .panel { width: min(420px, 100%); padding: 24px; border: 1px solid #d8e4df; border-radius: 8px; background: #fff; }
    h1 { margin: 0 0 20px; font-size: 28px; }
    form { display: grid; gap: 12px; }
    input, button { min-height: 44px; border-radius: 6px; border: 1px solid #c8d6d1; padding: 0 12px; }
    button { border: 0; background: #0f766e; color: #fff; font-weight: 700; }
    .error { color: #b42318; }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AuthShellComponent {
  phone = '';
  password = '';
  totpCode = '';
  isLoading = signal(false);
  error = signal<string | null>(null);

  constructor(private readonly auth: AuthService, private readonly router: Router) {}

  login(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.auth.login(this.phone, this.password, this.totpCode || undefined).subscribe({
      next: () => this.router.navigateByUrl('/'),
      error: err => {
        this.error.set(err.message ?? 'Unable to sign in');
        this.isLoading.set(false);
      }
    });
  }
}
