import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EnvelopeIllustration } from '../../shared/ui/brand/envelope-illustration';
import { Button } from '../../shared/ui/button/button';
import { SHELL_ROUTES } from '../../shell/nav-items';

/** Shown inside the shell when a member opens an admin page (docs/design.md §10). */
@Component({
  selector: 'app-no-access-page',
  imports: [RouterLink, EnvelopeIllustration, Button],
  host: { class: 'block px-6 py-14 lg:px-20 lg:py-24' },
  template: `
    <div class="flex max-w-130 flex-col items-start gap-3.5">
      <app-envelope-illustration kind="no-access" />
      <h1 class="font-serif text-page-title-sm font-semibold text-ink lg:text-page-title">
        You don’t have access to this page
      </h1>
      <p class="text-body text-ink-2">
        Only admins can manage invite codes. If you think you should have access, ask the person who
        invited you.
      </p>
      <a
        appButton
        [routerLink]="applicationsPath"
        class="mt-1 max-lg:mt-1.5 max-lg:h-12 max-lg:w-full max-lg:text-ui-lg"
      >
        Go to job applications
      </a>
    </div>
  `,
})
export class NoAccessPage {
  protected readonly applicationsPath = SHELL_ROUTES.applications;
}
