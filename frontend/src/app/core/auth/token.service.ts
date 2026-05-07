import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class TokenService {
  private readonly token = signal<string | null>(null);

  accessToken = this.token.asReadonly();

  setAccessToken(token: string | null): void {
    this.token.set(token);
  }

  clear(): void {
    this.token.set(null);
  }
}
