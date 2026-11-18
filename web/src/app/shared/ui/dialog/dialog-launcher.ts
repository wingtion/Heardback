import { BreakpointObserver } from '@angular/cdk/layout';
import { Dialog, DialogRef } from '@angular/cdk/dialog';
import { GlobalPositionStrategy, createGlobalPositionStrategy } from '@angular/cdk/overlay';
import { ComponentType } from '@angular/cdk/portal';
import { Injectable, Injector, inject } from '@angular/core';
import { NavigationStart, Router } from '@angular/router';
import { filter } from 'rxjs';
import { DESKTOP_MEDIA_QUERY } from '../../../core/breakpoints';

const FULL_SIZE = '100%';
const SIZE_FROM_CONTENT = '';

/**
 * How a dialog looks below the lg breakpoint: a bottom sheet (most dialogs), or a full-screen sheet for
 * long forms such as Add application (docs/design.md §8, §14).
 */
export type MobileDialogLayout = 'bottom-sheet' | 'full-screen';

export interface DialogLaunchOptions {
  readonly mobile?: MobileDialogLayout;
  /** "alertdialog" for confirmations of something that cannot be undone, such as a delete. */
  readonly role?: 'dialog' | 'alertdialog';
  /** Read in the dialog with inject(DIALOG_DATA). */
  readonly data?: unknown;
}

let nextTitleId = 0;

/**
 * Opens dialogs the way docs/design.md §8 describes: centred on the `scrim` backdrop from the lg breakpoint,
 * a sheet below it. The layout follows the viewport while the dialog is open. CDK Dialog supplies
 * role="dialog", aria-modal, the focus trap, Escape and focus return. The dialog's content uses
 * <app-dialog>, which reads the title id set here.
 */
@Injectable({ providedIn: 'root' })
export class DialogLauncher {
  private readonly dialog = inject(Dialog);
  private readonly breakpoints = inject(BreakpointObserver);
  private readonly injector = inject(Injector);
  private readonly router = inject(Router);

  open<C>(component: ComponentType<C>, options: DialogLaunchOptions = {}): DialogRef<unknown, C> {
    const mobile = options.mobile ?? 'bottom-sheet';
    const desktop = this.breakpoints.isMatched(DESKTOP_MEDIA_QUERY);
    const ref = this.dialog.open<unknown, unknown, C>(component, {
      ariaLabelledBy: `dialog-title-${nextTitleId++}`,
      role: options.role ?? 'dialog',
      data: options.data,
      backdropClass: this.backdrop(desktop, mobile),
      positionStrategy: this.position(desktop, mobile),
      ...this.size(desktop, mobile),
    });

    const layout = this.breakpoints.observe(DESKTOP_MEDIA_QUERY).subscribe(({ matches }) => {
      ref.overlayRef.updatePositionStrategy(this.position(matches, mobile));
      const size = this.size(matches, mobile);
      ref.updateSize(size.width, size.height);
      ref.overlayRef.backdropElement?.classList.remove(this.backdrop(!matches, mobile));
      ref.overlayRef.backdropElement?.classList.add(this.backdrop(matches, mobile));
    });
    // CDK closes dialogs only on browser back/forward. Dialogs belong to the page they were opened
    // on, so any in-app navigation (session expired, sign out, no access) closes them too.
    const navigation = this.router.events
      .pipe(filter((event) => event instanceof NavigationStart))
      .subscribe(() => ref.close());
    ref.closed.subscribe(() => {
      layout.unsubscribe();
      navigation.unsubscribe();
    });

    return ref;
  }

  private position(desktop: boolean, mobile: MobileDialogLayout): GlobalPositionStrategy {
    const strategy = createGlobalPositionStrategy(this.injector).centerHorizontally();
    if (desktop) {
      return strategy.centerVertically();
    }
    return mobile === 'full-screen' ? strategy.top('0') : strategy.bottom('0');
  }

  private size(
    desktop: boolean,
    mobile: MobileDialogLayout,
  ): { readonly width: string; readonly height: string } {
    if (desktop) {
      return { width: SIZE_FROM_CONTENT, height: SIZE_FROM_CONTENT };
    }
    return { width: FULL_SIZE, height: mobile === 'full-screen' ? FULL_SIZE : SIZE_FROM_CONTENT };
  }

  // Centred dialogs keep the page faintly visible; sheets dim it more (docs/design.md §3).
  private backdrop(desktop: boolean, mobile: MobileDialogLayout): string {
    return desktop || mobile === 'full-screen' ? 'bg-scrim' : 'bg-scrim-sheet';
  }
}
