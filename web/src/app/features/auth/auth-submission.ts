import { DestroyRef, Signal, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { Countdown, createCountdown } from '../../core/countdown';
import { AuthFailure, toAuthFailure } from './auth-errors';

export interface AuthSubmission {
  readonly submitting: Signal<boolean>;
  /** The last failure. A rate limit counts only while its countdown runs. */
  readonly failure: Signal<AuthFailure | null>;
  /** True while a rate limit is in force: the submit button stays disabled. */
  readonly locked: Signal<boolean>;
  readonly countdown: Countdown;
  run<T>(
    request: Observable<T>,
    handlers: { success: (value: T) => void; failed: () => void },
  ): void;
}

/**
 * Shared submit flow of the sign in and create account forms: one request at a time, the
 * failure mapped to form terms, and a countdown after a 429. Create it in an injection context.
 */
export function createAuthSubmission(): AuthSubmission {
  const destroyRef = inject(DestroyRef);
  const countdown = createCountdown();
  const submitting = signal(false);
  const lastFailure = signal<AuthFailure | null>(null);

  const failure = computed(() => {
    const current = lastFailure();
    return current?.kind === 'rate-limited' && !countdown.running() ? null : current;
  });

  return {
    submitting: submitting.asReadonly(),
    failure,
    locked: countdown.running,
    countdown,
    run(request, handlers) {
      if (submitting() || countdown.running()) {
        return;
      }
      submitting.set(true);
      lastFailure.set(null);
      request.pipe(takeUntilDestroyed(destroyRef)).subscribe({
        next: (value) => {
          submitting.set(false);
          handlers.success(value);
        },
        error: (error: unknown) => {
          const mapped = toAuthFailure(error);
          if (mapped.kind === 'rate-limited') {
            countdown.start(mapped.retryAfterSeconds);
          }
          submitting.set(false);
          lastFailure.set(mapped);
          handlers.failed();
        },
      });
    },
  };
}
