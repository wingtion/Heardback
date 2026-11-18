import { Component, booleanAttribute, computed, input, output } from '@angular/core';
import { Icon } from '../../shared/ui/icon/icon';

/**
 * How a skill is shown: "found" on the CV page, "matched" or "missing" in the match panel, "excluded" for a
 * skill the user said is not theirs. Matched and missing differ by more than colour: matched has a check and a
 * solid border, missing and excluded a dashed border.
 */
export type SkillChipKind = 'found' | 'matched' | 'missing' | 'excluded';

/** What clicking the chip does, when it is a button: remove it ("not my skill") or count it again. */
export type SkillChipAction = 'remove' | 'restore';

// Full class names so Tailwind finds them.
const KINDS: Record<SkillChipKind, string> = {
  found: 'border border-rule-strong bg-surface text-ink',
  matched: 'border border-status-offer bg-base text-status-offer',
  missing: 'border border-dashed border-field-border bg-transparent text-ink-2',
  excluded: 'border border-dashed border-field-border bg-transparent text-ink-muted',
};

const CHIP =
  'inline-flex items-center gap-1 rounded-sm px-2 text-label font-medium whitespace-nowrap';
const LABEL_HEIGHT = 'h-6';

// As a button the whole chip is the target: 28 px, 44 px below the lg breakpoint (touch rule, docs/design.md §5).
const BUTTON =
  'h-7 cursor-pointer pr-1.5 hover:bg-secondary-hover focus-visible:outline-2 focus-visible:outline-offset-2 ' +
  'focus-visible:outline-focus disabled:cursor-default disabled:opacity-60 max-lg:h-11 max-lg:px-3';

/** A skill name in a small bordered label (built from the status label's sizes, docs/design.md §8, §15). */
@Component({
  selector: 'app-skill-chip',
  imports: [Icon],
  host: { class: 'inline-flex' },
  template: `
    @if (action(); as action) {
      <button
        type="button"
        [class]="chipClass() + ' ' + buttonClass"
        [disabled]="disabled()"
        [attr.aria-label]="actionLabel()"
        (click)="actioned.emit()"
      >
        <span>{{ name() }}</span>
        <app-icon [name]="action === 'remove' ? 'close' : 'plus'" [size]="12" class="text-ink-2" />
      </button>
    } @else {
      <span [class]="chipClass() + ' ' + labelHeight">
        @if (kind() === 'matched') {
          <app-icon name="check" [size]="12" />
        }
        <span>{{ name() }}</span>
      </span>
    }
  `,
})
export class SkillChip {
  readonly name = input.required<string>();
  readonly kind = input<SkillChipKind>('found');
  /** Makes the chip a button that removes or restores the skill. */
  readonly action = input<SkillChipAction | null>(null);
  readonly disabled = input(false, { transform: booleanAttribute });

  /** The chip's button was pressed. */
  readonly actioned = output();

  protected readonly buttonClass = BUTTON;
  protected readonly labelHeight = LABEL_HEIGHT;

  protected readonly chipClass = computed(() => `${CHIP} ${KINDS[this.kind()]}`);

  protected readonly actionLabel = computed(() =>
    this.action() === 'remove'
      ? `Remove ${this.name()}: not my skill`
      : `Count ${this.name()} again`,
  );
}
