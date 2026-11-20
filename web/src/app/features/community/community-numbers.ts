import { Component, computed, input } from '@angular/core';
import { CommunityNumbers } from './community.models';
import { NO_TIME, hasMissingTime, statRows } from './community-display';

export interface NumbersColumn {
  /** "This posting" or "This company"; the company page's single column has none. */
  readonly heading: string | null;
  readonly numbers: CommunityNumbers;
}

/**
 * Other members' numbers as a small table (docs/design.md §18): labels on the left, one right-aligned DM Mono
 * column per group, and the caption explaining "—" when an answer time is missing.
 */
@Component({
  selector: 'app-community-numbers',
  host: { class: 'flex flex-col gap-1.5' },
  template: `
    <table class="w-full table-fixed border-collapse text-small" [attr.aria-label]="label()">
      <colgroup>
        <col />
        @for (column of columns(); track $index) {
          <col [class]="columnWidth()" />
        }
      </colgroup>
      @if (hasHeadings()) {
        <thead>
          <tr>
            <td class="border-b border-rule-strong pb-1.5"></td>
            @for (column of columns(); track $index) {
              <th
                scope="col"
                class="border-b border-rule-strong pb-1.5 text-right text-caption font-semibold whitespace-nowrap text-ink-2"
              >
                {{ column.heading }}
              </th>
            }
          </tr>
        </thead>
      }
      <tbody>
        @for (row of rows(); track row.label) {
          <tr>
            <th
              scope="row"
              class="border-b border-rule pr-2 text-left font-normal text-ink-2"
              [class]="rowPadding()"
            >
              {{ row.label }}
            </th>
            @for (cell of row.cells; track $index) {
              <td
                class="border-b border-rule text-right font-mono text-label text-ink tabular-nums"
                [class]="rowPadding()"
              >
                <span aria-hidden="true">{{ cell.text }}</span>
                <span class="sr-only">{{ cell.aria }}</span>
              </td>
            }
          </tr>
        }
      </tbody>
    </table>
    @if (missingTime()) {
      <p class="text-caption text-ink-muted">
        “{{ noTime }}” means nobody has heard back yet, so there is no answer time.
      </p>
    }
  `,
})
export class CommunityNumbersTable {
  readonly columns = input.required<readonly NumbersColumn[]>();
  /** The table's accessible name. */
  readonly label = input.required<string>();

  protected readonly noTime = NO_TIME;
  protected readonly rows = computed(() =>
    statRows(this.columns().map((column) => column.numbers)),
  );
  protected readonly hasHeadings = computed(() =>
    this.columns().some((column) => column.heading !== null),
  );
  // The company page box has a little more room per row (padding 7 instead of 6).
  protected readonly rowPadding = computed(() => (this.hasHeadings() ? 'py-1.5' : 'py-1.75'));
  // 88 px each for two groups so "This company" fits on one line (the handoff's 76 px wraps it), 96 px for one.
  protected readonly columnWidth = computed(() => (this.columns().length > 1 ? 'w-22' : 'w-24'));
  protected readonly missingTime = computed(() =>
    hasMissingTime(this.columns().map((column) => column.numbers)),
  );
}
