import { BreakpointObserver } from '@angular/cdk/layout';
import { Dialog, DialogRef } from '@angular/cdk/dialog';
import { GlobalPositionStrategy, createGlobalPositionStrategy } from '@angular/cdk/overlay';
import { ComponentType } from '@angular/cdk/portal';
import { Injectable, Injector, inject } from '@angular/core';
import { DESKTOP_MEDIA_QUERY } from '../../../core/breakpoints';

const FULL_WIDTH = '100%';
const SIZE_FROM_CONTENT = '';

let nextTitleId = 0;

/**
 * Opens dialogs the way docs/design.md §8 describes: centred from the lg breakpoint, a bottom sheet
 * below it, on the `scrim` backdrop. The layout follows the viewport while the dialog is open.
 * CDK Dialog supplies role="dialog", aria-modal, the focus trap, Escape and focus return.
 * The dialog's content uses <app-dialog>, which reads the title id set here.
 */
@Injectable({ providedIn: 'root' })
export class DialogLauncher {
  private readonly dialog = inject(Dialog);
  private readonly breakpoints = inject(BreakpointObserver);
  private readonly injector = inject(Injector);

  open<C>(component: ComponentType<C>): DialogRef<unknown, C> {
    const desktop = this.breakpoints.isMatched(DESKTOP_MEDIA_QUERY);
    const ref = this.dialog.open<unknown, unknown, C>(component, {
      ariaLabelledBy: `dialog-title-${nextTitleId++}`,
      backdropClass: 'bg-scrim',
      positionStrategy: this.position(desktop),
      width: desktop ? SIZE_FROM_CONTENT : FULL_WIDTH,
    });

    const layout = this.breakpoints.observe(DESKTOP_MEDIA_QUERY).subscribe(({ matches }) => {
      ref.overlayRef.updatePositionStrategy(this.position(matches));
      ref.updateSize(matches ? SIZE_FROM_CONTENT : FULL_WIDTH);
    });
    ref.closed.subscribe(() => layout.unsubscribe());

    return ref;
  }

  private position(desktop: boolean): GlobalPositionStrategy {
    const strategy = createGlobalPositionStrategy(this.injector).centerHorizontally();
    return desktop ? strategy.centerVertically() : strategy.bottom('0');
  }
}
