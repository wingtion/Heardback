import { Component, input } from '@angular/core';

/**
 * A one-time code shown once after it is created: an invite code or a password reset code
 * (docs/design.md §8 "Invite code display"). DM Mono in a dotted accent frame on the envelope colour;
 * a single click selects the whole code.
 */
@Component({
  selector: 'app-code-display',
  host: { class: 'block border-2 border-dotted border-accent bg-envelope p-1' },
  template: `
    <div
      class="flex h-17 items-center justify-center border border-accent font-mono text-display-sm font-medium tracking-code-lg text-ink select-all lg:h-18 lg:text-display"
    >
      {{ code() }}
    </div>
  `,
})
export class CodeDisplay {
  readonly code = input.required<string>();
}
