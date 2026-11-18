import { Directive, Injectable, OnDestroy, TemplateRef, inject, signal } from '@angular/core';

/**
 * Lets a page put its primary action into the mobile header, beside the menu button (docs/design.md §14:
 * the Applications page's "Add"). Provided by the shell, so it ends with the signed-in session.
 */
@Injectable()
export class ShellActions {
  readonly mobileAction = signal<TemplateRef<unknown> | null>(null);
}

/**
 * Marks the template a page shows in the mobile header while the page is open:
 * `<ng-template appShellMobileAction><button …>Add</button></ng-template>`.
 */
@Directive({ selector: 'ng-template[appShellMobileAction]' })
export class ShellMobileAction implements OnDestroy {
  private readonly actions = inject(ShellActions);
  private readonly template = inject(TemplateRef);

  constructor() {
    this.actions.mobileAction.set(this.template);
  }

  ngOnDestroy(): void {
    if (this.actions.mobileAction() === this.template) {
      this.actions.mobileAction.set(null);
    }
  }
}
