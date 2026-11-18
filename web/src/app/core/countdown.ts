import { DestroyRef, Signal, computed, inject, signal } from '@angular/core';

const TICK_MS = 1000;

export interface Countdown {
  /** Seconds left; 0 when idle or finished. */
  readonly seconds: Signal<number>;
  readonly running: Signal<boolean>;
  start(seconds: number): void;
}

/**
 * A one-second countdown, e.g. "Try again in 42 seconds" after a rate limit.
 * Must be created in an injection context; the timer stops when that context is destroyed.
 */
export function createCountdown(): Countdown {
  const seconds = signal(0);
  let timer: ReturnType<typeof setInterval> | null = null;

  const stop = () => {
    if (timer !== null) {
      clearInterval(timer);
      timer = null;
    }
  };

  inject(DestroyRef).onDestroy(stop);

  return {
    seconds: seconds.asReadonly(),
    running: computed(() => seconds() > 0),
    start(value: number) {
      stop();
      seconds.set(Math.max(0, Math.ceil(value)));
      if (seconds() === 0) {
        return;
      }
      timer = setInterval(() => {
        seconds.update((left) => left - 1);
        if (seconds() <= 0) {
          stop();
        }
      }, TICK_MS);
    },
  };
}
