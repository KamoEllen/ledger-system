import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest, RefreshTokenRequest, UserDto } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly base = `${environment.apiUrl}/auth`;

  private _user = signal<UserDto | null>(this.loadUser());
  private _accessToken = signal<string | null>(localStorage.getItem('access_token'));

  readonly user = this._user.asReadonly();
  readonly isLoggedIn = computed(() => this._user() !== null);
  readonly isAdmin = computed(() => this._user()?.role === 'Admin');
  readonly isFinanceOrAdmin = computed(() => ['Finance', 'Admin'].includes(this._user()?.role ?? ''));

  constructor(private http: HttpClient, private router: Router) {}

  register(req: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.base}/register`, req).pipe(
      tap(res => this.storeSession(res))
    );
  }

  login(req: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.base}/login`, req).pipe(
      tap(res => this.storeSession(res))
    );
  }

  refresh(): Observable<AuthResponse> {
    const refreshToken = localStorage.getItem('refresh_token') ?? '';
    return this.http.post<AuthResponse>(`${this.base}/refresh`, { refreshToken } as RefreshTokenRequest).pipe(
      tap(res => this.storeSession(res))
    );
  }

  logout(): void {
    const refreshToken = localStorage.getItem('refresh_token') ?? '';
    this.http.post(`${this.base}/logout`, { refreshToken } as RefreshTokenRequest).subscribe({
      complete: () => this.clearSession()
    });
    this.clearSession();
  }

  getAccessToken(): string | null {
    return this._accessToken();
  }

  private storeSession(res: AuthResponse): void {
    localStorage.setItem('access_token', res.accessToken);
    localStorage.setItem('refresh_token', res.refreshToken);
    localStorage.setItem('user', JSON.stringify(res.user));
    this._accessToken.set(res.accessToken);
    this._user.set(res.user);
  }

  private clearSession(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('user');
    this._accessToken.set(null);
    this._user.set(null);
    this.router.navigate(['/auth/login']);
  }

  private loadUser(): UserDto | null {
    try {
      const raw = localStorage.getItem('user');
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  }
}
