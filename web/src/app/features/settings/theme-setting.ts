import { Component, inject } from '@angular/core';
import { THEME_PREFERENCES, ThemePreference, ThemeService } from '../../core/theme';

const LABELS: Readonly<Record<ThemePreference, string>> = {
  system: 'Same as device',
  light: 'Light',
  dark: 'Dark',
};

// The radio chips of Add update (docs/design.md §14), without glyphs.
const CHIP =
  'inline-flex h-11 cursor-pointer items-center gap-1.5 rounded-md bg-base pr-3 pl-2 text-cell ' +
  'focus-within:outline-2 focus-within:outline-offset-2 focus-within:outline-focus lg:h-8.5';
const CHIP_OFF = 'border border-field-border text-ink';
const CHIP_ON = 'border-thick border-ink bg-row-selected font-semibold text-ink';

/** Light / Dark / Same as device (docs/design.md §2). Applies at once and is kept on this device. */
@Component({
  selector: 'app-theme-setting',
  template: `
    <fieldset class="flex flex-col gap-1.5">
      <legend class="mb-1.5 text-label font-semibold text-ink-2">Theme</legend>
      <div class="flex flex-wrap gap-1.5">
        @for (option of options; track option) {
          <label [class]="chipClass(option)">
            <input
              type="radio"
              name="theme"
              class="m-0 accent-accent"
              [value]="option"
              [checked]="theme.preference() === option"
              (change)="theme.set(option)"
            />
            {{ labels[option] }}
          </label>
        }
      </div>
      <p class="text-caption text-ink-muted">Saved in this browser only.</p>
    </fieldset>
  `,
})
export class ThemeSetting {
  protected readonly theme = inject(ThemeService);
  protected readonly options = THEME_PREFERENCES;
  protected readonly labels = LABELS;

  protected chipClass(option: ThemePreference): string {
    return `${CHIP} ${this.theme.preference() === option ? CHIP_ON : CHIP_OFF}`;
  }
}
