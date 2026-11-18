import { Injectable, computed, inject, signal } from '@angular/core';
import {
  Observable,
  catchError,
  finalize,
  firstValueFrom,
  map,
  of,
  shareReplay,
  tap,
  throwError,
} from 'rxjs';
import { AuthApi } from './auth-api';
import { isRefreshSuperseded, isUnauthorized } from './auth-errors';
import { AuthResponse, CurrentUser, LoginRequest, RegisterRequest } from './auth.models';

/**
 * The signed-in session. The access token is kept in memory only; the refresh token is an httpOnly
 * cookie the browser manages, so a page reload restores the session through refresh().
 */
/** What is remembered (in memory only) about a session that expired, to pre-fill sign in. */
export interface ExpiredSession {
  readonly email: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(AuthApi);

  private readonly session = signal<AuthResponse | null>(null);
  private readonly expiredSession = signal<ExpiredSession | null>(null);
  private readonly restoringSession = signal(false);
  private restoration: Promise<void> = Promise.resolve();
  private refreshInFlight: Observable<void> | null = null;

  readonly user = computed<CurrentUser | null>(() => this.session()?.user ?? null);
  readonly accessToken = computed<string | null>(() => this.session()?.accessToken ?? null);
  readonly isAuthenticated = computed(() => this.session() !== null);
  readonly isAdmin = computed(() => this.session()?.user.isAdmin ?? false);
  /** True while the session is being restored from the refresh cookie at startup. */
  readonly restoring = this.restoringSession.asReadonly();
  /** Set when a signed-in session ended because the server no longer accepts it (not on sign out). */
  readonly expired = this.expiredSession.asReadonly();

  login(request: LoginRequest): Observable<CurrentUser> {
    return this.api.login(request).pipe(map((response) => this.start(response)));
  }

  register(request: RegisterRequest): Observable<CurrentUser> {
    return this.api.register(request).pipe(map((response) => this.start(response)));
  }

  /**
   * Gets a new access token using the refresh cookie. Concurrent callers share one request, because
   * the server rotates the cookie and a second parallel refresh would be rejected. If another tab
   * refreshed first, the cookie already holds its newer token, so the refresh is retried once.
   * Only a 401 ends the session; rate limits, server errors and network failures leave it intact.
   */
  refresh(): Observable<void> {
    this.refreshInFlight ??= this.api.refresh().pipe(
      catchError((error: unknown) =>
        isRefreshSuperseded(error) ? this.api.refresh() : throwError(() => error),
      ),
      tap((response) => this.session.set(response)),
      map(() => undefined),
      catchError((error: unknown) => {
        if (isUnauthorized(error)) {
          this.endExpiredSession();
        }
        return throwError(() => error);
      }),
      finalize(() => (this.refreshInFlight = null)),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    return this.refreshInFlight;
  }

  /** Ends the session locally even if the server call fails, so the user is never stuck signed in. */
  logout(): Observable<void> {
    return this.api.logout().pipe(
      catchError(() => of(undefined)),
      finalize(() => this.clear()),
    );
  }

  /**
   * Called once at startup without blocking it: the app shows its session loading screen while
   * restoring() is true. Resolves either way; without a valid cookie the app starts signed out.
   */
  restoreSession(): Promise<void> {
    this.restoringSession.set(true);
    this.restoration = firstValueFrom(this.refresh().pipe(catchError(() => of(undefined)))).finally(
      () => this.restoringSession.set(false),
    );
    return this.restoration;
  }

  /** Resolves once a startup restore (if any) has finished. Route guards wait for it. */
  whenRestored(): Promise<void> {
    return this.restoration;
  }

  private start(response: AuthResponse): CurrentUser {
    this.expiredSession.set(null);
    this.session.set(response);
    return response.user;
  }

  private endExpiredSession(): void {
    const user = this.session()?.user;
    this.session.set(null);
    if (user) {
      this.expiredSession.set({ email: user.email });
    }
  }

  private clear(): void {
    this.session.set(null);
    this.expiredSession.set(null);
  }
}
