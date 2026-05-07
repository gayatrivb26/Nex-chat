import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Observable, map, tap } from 'rxjs';
import { ApiResponse, AuthResponse, User } from '../models';
import { TokenService } from './token.service';

const API_BASE = '/api/v1';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly currentUser = signal<User | null>(null);

  user = this.currentUser.asReadonly();
  isAuthenticated = computed(() => !!this.currentUser() && !!this.tokens.accessToken());

  constructor(private readonly http: HttpClient, private readonly tokens: TokenService) {}

  login(phone: string, password: string, totpCode?: string): Observable<AuthResponse> {
    return this.http.post<ApiResponse<AuthResponse>>(`${API_BASE}/auth/login`, { phone, password, totpCode }, { withCredentials: true })
      .pipe(mapRequiredData(), tap(response => this.setSession(response)));
  }

  register(username: string, phone: string, password: string, email?: string): Observable<AuthResponse> {
    return this.http.post<ApiResponse<AuthResponse>>(`${API_BASE}/auth/register`, { username, phone, password, email })
      .pipe(mapRequiredData());
  }

  verifyOtp(phone: string, otp: string): Observable<AuthResponse> {
    return this.http.post<ApiResponse<AuthResponse>>(`${API_BASE}/auth/otp/verify`, { phone, otp }, { withCredentials: true })
      .pipe(mapRequiredData(), tap(response => this.setSession(response)));
  }

  refresh(): Observable<AuthResponse> {
    return this.http.post<ApiResponse<AuthResponse>>(`${API_BASE}/auth/refresh`, {}, { withCredentials: true })
      .pipe(mapRequiredData(), tap(response => this.setSession(response)));
  }

  logout(): Observable<unknown> {
    return this.http.post<ApiResponse<unknown>>(`${API_BASE}/auth/logout`, {}, { withCredentials: true })
      .pipe(tap(() => this.clearSession()));
  }

  private setSession(response: AuthResponse): void {
    this.tokens.setAccessToken(response.accessToken);
    this.currentUser.set(response.user);
  }

  private clearSession(): void {
    this.tokens.clear();
    this.currentUser.set(null);
  }
}

function mapRequiredData<T>() {
  return map((response: ApiResponse<T>) => {
    if (!response.success || !response.data) {
      throw new Error(response.message ?? 'Request failed');
    }
    return response.data;
  });
}
