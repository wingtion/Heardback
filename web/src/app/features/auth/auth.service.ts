import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import {
  Observable,
  catchError,
  defer,
  finalize,
  firstValueFrom,
  map,
  of,
  retry,
  shareReplay,
  tap,
  throwError,
  timer,
} from 'rxjs';
import { AuthApi } from './auth-api';
import { isRefreshSuperseded, isUnauthorized, retryAfterSeconds } from './auth-errors';
import {
  AuthResponse,
  ChangePasswordRequest,
  CurrentUser,
  LoginRequest,
  RegisterRequest,
  ResetPasswordRequest,
} from './auth.models';

/** What is remembered (in memory only) about a session that expired, to pre-fill sign in. */
export interface ExpiredSession {
  /** Pre-fills sign in; null for a demo, whose made-up address nobody can sign in with. */
  readonly email: string | null;
  /** The session was a demo that ended (ADR-0018). */
  readonly demo: boolean;
}

// A reload's refresh is retried when the server is briefly unreachable or rate limited: waits of
// 1, 2 and 4 seconds, or the server's Retry-After if it is short enough to wait on the loading screen.
const RESTORE_RETRIES = 3;
const RESTORE_FIRST_RETRY_SECONDS = 1;
const RESTORE_MAX_WAIT_SECONDS = 10;
const MS_PER_SECOND = 1000;

function restoreRetryDelay(error: unknown, retryNumber: number): Observable<number> {
  if (isUnauthorized(error)) {
    return throwError(() => error);
  }
  const rateLimited =
    error instanceof HttpErrorResponse && error.status === HttpStatusCode.TooManyRequests;
  const seconds = rateLimited
    ? retryAfterSeconds(error)
    : RESTORE_FIRST_RETRY_SECONDS * 2 ** (retryNumber - 1);
  return seconds > RESTORE_MAX_WAIT_SECONDS
    ? throwError(() => error)
    : timer(seconds * MS_PER_SECOND);
}

/**
 * The signed-in session. The access token is kept in memory only; the refresh token is an httpOnly
 * cookie the browser manages, so a page reload restores the session through refresh().
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(AuthApi);

  private readonly session = signal<AuthResponse | null>(null);
  private readonly expiredSession = signal<ExpiredSession | null>(null);
  private readonly restoringSession = signal(false);
  private readonly restoreFailedFlag = signal(false);
  private restoration: Promise<void> = Promise.resolve();
  private refreshInFlight: Observable<void> | null = null;

  readonly user = computed<CurrentUser | null>(() => this.session()?.user ?? null);
  readonly accessToken = computed<string | null>(() => this.session()?.accessToken ?? null);
  readonly isAuthenticated = computed(() => this.session() !== null);
  readonly isAdmin = computed(() => this.session()?.user.isAdmin ?? false);
  readonly isDemo = computed(() => this.session()?.user.isDemo ?? false);
  /** True while the session is being restored from the refresh cookie at startup. */
  readonly restoring = this.restoringSession.asReadonly();
  /** The startup restore gave up because the server could not be reached (not because of a 401). */
  readonly restoreFailed = this.restoreFailedFlag.asReadonly();
  /** Set when a signed-in session ended because the server no longer accepts it (not on sign out). */
  readonly expired = this.expiredSession.asReadonly();

  login(request: LoginRequest): Observable<CurrentUser> {
    return this.api.login(request).pipe(map((response) => this.start(response)));
  }

  register(request: RegisterRequest): Observable<CurrentUser> {
    return this.api.register(request).pipe(map((response) => this.start(response)));
  }

  /** Starts a temporary demo account with sample data (ADR-0018) and signs in to it. */
  startDemo(): Observable<CurrentUser> {
    return this.api.startDemo().pipe(map((response) => this.start(response)));
  }

  /** Sets a new password with an admin's reset code. For signed-out users; it signs nobody in. */
  resetPassword(request: ResetPasswordRequest): Observable<void> {
    return this.api.resetPassword(request);
  }

  /**
   * The server ends every session, including this one, and answers with a new session for this
   * browser, so the user stays signed in here and is signed out everywhere else.
   */
  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.api.changePassword(request).pipe(
      tap((response) => this.session.set(response)),
      map(() => undefined),
    );
  }

  /** The server has deleted the account and its session, so the session ends here too. */
  deleteAccount(password: string): Observable<void> {
    return this.api.deleteAccount(password).pipe(tap(() => this.clear()));
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
   * A brief outage or rate limit is retried, because the cookie session is still valid then; if the
   * server stays unreachable the app starts signed out and restoreFailed() is set for sign in.
   */
  restoreSession(): Promise<void> {
    this.restoringSession.set(true);
    this.restoreFailedFlag.set(false);
    const restore = defer(() => this.refresh()).pipe(
      retry({ count: RESTORE_RETRIES, delay: restoreRetryDelay }),
      catchError((error: unknown) => {
        this.restoreFailedFlag.set(!isUnauthorized(error));
        return of(undefined);
      }),
    );
    this.restoration = firstValueFrom(restore).finally(() => this.restoringSession.set(false));
    return this.restoration;
  }

  /** Resolves once a startup restore (if any) has finished. Route guards wait for it. */
  whenRestored(): Promise<void> {
    return this.restoration;
  }

  private start(response: AuthResponse): CurrentUser {
    this.expiredSession.set(null);
    this.restoreFailedFlag.set(false);
    this.session.set(response);
    return response.user;
  }

  private endExpiredSession(): void {
    const user = this.session()?.user;
    this.session.set(null);
    if (user) {
      this.expiredSession.set({ email: user.isDemo ? null : user.email, demo: user.isDemo });
    }
  }

  private clear(): void {
    this.session.set(null);
    this.expiredSession.set(null);
  }
}
