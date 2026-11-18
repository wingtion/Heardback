import { Component, booleanAttribute, computed, input } from '@angular/core';
import { Icon } from '../icon/icon';

let nextFieldId = 0;

/**
 * A labelled form field (docs/design.md §8): label above, the projected control, then an optional
 * hint and error. The control carries the appInput directive, which picks up the id, aria-invalid
 * and aria-describedby from this component, so labels and messages are always linked.
 */
@Component({
  selector: 'app-field',
  imports: [Icon],
  host: { class: 'flex flex-col gap-1.25' },
  template: `
    <label [attr.for]="controlId" class="text-label font-semibold text-ink-2">
      {{ label() }}
      @if (optional()) {
        <span class="font-normal text-ink-muted">· optional</span>
      } @else if (required()) {
        <span class="font-normal text-ink-muted">· required</span>
      }
    </label>
    <ng-content />
    @if (error(); as message) {
      <div [id]="errorId" class="flex items-start gap-1.5 text-label font-medium text-danger">
        <app-icon name="warning" [size]="14" class="mt-0.5" />
        <span>{{ message }}</span>
      </div>
    }
    @if (hint() || hintEnd()) {
      <div [id]="hintId" class="flex justify-between gap-3 text-caption text-ink-muted">
        <span>{{ hint() }}</span>
        @if (hintEnd(); as end) {
          <span class="font-mono">{{ end }}</span>
        }
      </div>
    }
  `,
})
export class Field {
  readonly label = input.required<string>();
  readonly hint = input<string | null>(null);
  /** Right-aligned hint text, e.g. a live character count. */
  readonly hintEnd = input<string | null>(null);
  readonly error = input<string | null>(null);
  readonly optional = input(false, { transform: booleanAttribute });
  /** Shows "· required" after the label, for forms that mix required and optional fields. */
  readonly required = input(false, { transform: booleanAttribute });

  readonly controlId = `field-${nextFieldId++}`;
  readonly hintId = `${this.controlId}-hint`;
  readonly errorId = `${this.controlId}-error`;

  readonly invalid = computed(() => this.error() !== null);
  readonly describedBy = computed(() => {
    const ids = [
      this.error() !== null ? this.errorId : null,
      this.hint() || this.hintEnd() ? this.hintId : null,
    ].filter((id) => id !== null);
    return ids.length > 0 ? ids.join(' ') : null;
  });
}
