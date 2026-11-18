import { TestBed } from '@angular/core/testing';
import { createCountdown } from './countdown';

describe('createCountdown', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('start_CountsDownOncePerSecond_AndStopsAtZero', () => {
    const countdown = TestBed.runInInjectionContext(() => createCountdown());

    countdown.start(3);
    expect(countdown.seconds()).toBe(3);
    expect(countdown.running()).toBe(true);

    vi.advanceTimersByTime(2000);
    expect(countdown.seconds()).toBe(1);

    vi.advanceTimersByTime(5000);
    expect(countdown.seconds()).toBe(0);
    expect(countdown.running()).toBe(false);
  });

  it('start_CalledAgain_RestartsFromTheNewValue', () => {
    const countdown = TestBed.runInInjectionContext(() => createCountdown());

    countdown.start(10);
    vi.advanceTimersByTime(4000);
    countdown.start(5);

    expect(countdown.seconds()).toBe(5);
    vi.advanceTimersByTime(1000);
    expect(countdown.seconds()).toBe(4);
  });

  it('start_Zero_IsNotRunning', () => {
    const countdown = TestBed.runInInjectionContext(() => createCountdown());

    countdown.start(0);

    expect(countdown.running()).toBe(false);
  });
});
