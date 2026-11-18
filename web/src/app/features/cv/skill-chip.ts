import { Component, computed, input } from '@angular/core';
import { Icon } from '../../shared/ui/icon/icon';

/**
 * How a skill is shown: "found" on the CV page, "matched" or "missing" in the match panel. Matched and missing
 * differ by more than colour: matched has a check and a solid border, missing a dashed border.
 */
export type SkillChipKind = 'found' | 'matched' | 'missing';

// Full class names so Tailwind finds them.
const KINDS: Record<SkillChipKind, string> = {
  found: 'border border-rule-strong bg-surface text-ink',
  matched: 'border border-status-offer bg-base text-status-offer',
  missing: 'border border-dashed border-field-border bg-transparent text-ink-2',
};

/** A skill name in a small bordered label (built from the status label's sizes, docs/design.md §8). */
@Component({
  selector: 'app-skill-chip',
  imports: [Icon],
  host: { '[class]': 'hostClass()' },
  template: `
    @if (kind() === 'matched') {
      <app-icon name="check" [size]="12" />
    }
    <span>{{ name() }}</span>
  `,
})
export class SkillChip {
  readonly name = input.required<string>();
  readonly kind = input<SkillChipKind>('found');

  protected readonly hostClass = computed(
    () =>
      `inline-flex h-6 items-center gap-1 rounded-sm px-2 text-label font-medium whitespace-nowrap ${KINDS[this.kind()]}`,
  );
}
