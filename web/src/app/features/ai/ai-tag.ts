import { Component } from '@angular/core';

/**
 * A small "AI" label beside the heading of a section an LLM wrote (docs/design.md §16), so it is clear at a glance
 * which parts are AI-written and which are worked out exactly. Built from the status label's measurements (§8),
 * smaller; plain text, no icon.
 */
@Component({
  selector: 'app-ai-tag',
  host: {
    class:
      'inline-flex h-5 items-center rounded-sm border border-field-border px-1.5 text-caption ' +
      'font-medium text-ink-2',
    title: 'Written by AI',
  },
  template: `<span aria-hidden="true">AI</span><span class="sr-only">written by AI</span>`,
})
export class AiTag {}
