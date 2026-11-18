import { Component } from '@angular/core';
import { EnvelopeIllustration } from '../../shared/ui/brand/envelope-illustration';

/**
 * Placeholder until Phase 2 builds the applications list. It shows the designed empty state; the
 * "Add application" button arrives together with the API that saves applications.
 */
@Component({
  selector: 'app-applications-page',
  imports: [EnvelopeIllustration],
  host: { class: 'flex flex-col gap-4.5 p-4 lg:px-7 lg:pt-6 lg:pb-5' },
  template: `
    <h1 class="font-serif text-page-title-sm font-semibold text-ink lg:text-page-title">
      Applications
    </h1>
    <div class="max-w-xl rounded-md border border-rule-strong bg-base">
      <div
        aria-hidden="true"
        class="flex gap-10 rounded-t-md border-b border-rule-strong bg-surface px-4 py-2.25 text-caption font-semibold text-ink-2"
      >
        <span>Company</span><span>Role</span><span>Status</span>
      </div>
      <div class="flex flex-col items-start gap-2.5 px-4.5 py-5.5">
        <app-envelope-illustration />
        <h2 class="font-serif text-empty-title font-semibold text-ink">No applications yet</h2>
        <p class="text-small text-ink-2">
          Add a job you applied for and Heardback will keep track of who replies.
        </p>
      </div>
    </div>
  `,
})
export class ApplicationsPage {}
