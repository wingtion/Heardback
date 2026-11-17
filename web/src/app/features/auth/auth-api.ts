import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/api-base-url';
import { AUTH_API } from './auth-paths';
import { AuthResponse, CurrentUser, LoginRequest, RegisterRequest } from './auth.models';

/** HTTP calls to the Identity API. State lives in AuthService; components use that, not this. */
@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(this.url(AUTH_API.register), request);
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(this.url(AUTH_API.login), request);
  }

  /** Uses the httpOnly refresh cookie; the browser sends it, scripts never see it. */
  refresh(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(this.url(AUTH_API.refresh), null);
  }

  logout(): Observable<void> {
    return this.http.post<void>(this.url(AUTH_API.logout), null);
  }

  me(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>(this.url(AUTH_API.me));
  }

  private url(path: string): string {
    return `${this.baseUrl}${path}`;
  }
}
