import { DOCUMENT } from '@angular/common';
import {
  EnvironmentProviders,
  Injectable,
  inject,
  provideAppInitializer,
  signal,
} from '@angular/core';

/** "system" follows the OS; the other two override it (docs/design.md §2). */
export type ThemePreference = 'system' | 'light' | 'dark';

export const THEME_PREFERENCES: readonly ThemePreference[] = ['system', 'light', 'dark'];

/** Kept on this device only; the choice is not part of the account. */
export const THEME_STORAGE_KEY = 'heardback.theme';

const THEME_ATTRIBUTE = 'data-theme';

function isThemePreference(value: string | null): value is ThemePreference {
  return value !== null && (THEME_PREFERENCES as readonly string[]).includes(value);
}

/**
 * The Light / Dark / System setting. The tokens read `data-theme` on <html> (web/src/styles/tokens.css);
 * without it the OS theme applies. Storage can be blocked (private windows, strict settings), so a failed
 * read or write falls back to following the OS for this visit.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly root = inject(DOCUMENT).documentElement;
  private readonly current = signal<ThemePreference>('system');

  readonly preference = this.current.asReadonly();

  /** Applies the stored choice. Runs once before the app first renders. */
  restore(): void {
    this.apply(this.read());
  }

  set(preference: ThemePreference): void {
    this.apply(preference);
    try {
      if (preference === 'system') {
        localStorage.removeItem(THEME_STORAGE_KEY);
      } else {
        localStorage.setItem(THEME_STORAGE_KEY, preference);
      }
    } catch {
      // Storage is blocked; the choice still applies until the page is closed.
    }
  }

  private apply(preference: ThemePreference): void {
    this.current.set(preference);
    if (preference === 'system') {
      this.root.removeAttribute(THEME_ATTRIBUTE);
    } else {
      this.root.setAttribute(THEME_ATTRIBUTE, preference);
    }
  }

  private read(): ThemePreference {
    try {
      const stored = localStorage.getItem(THEME_STORAGE_KEY);
      return isThemePreference(stored) ? stored : 'system';
    } catch {
      return 'system';
    }
  }
}

/** Applies the stored theme before the first render, so the page does not flash the OS theme. */
export function provideTheme(): EnvironmentProviders {
  return provideAppInitializer(() => inject(ThemeService).restore());
}
