import { LiveAnnouncer } from '@angular/cdk/a11y';
import { Clipboard } from '@angular/cdk/clipboard';
import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { isForbidden } from '../../core/http-errors';
import { AUTH_ROUTES } from '../auth/auth-paths';
import { Banner } from '../../shared/ui/banner/banner';
import { EnvelopeIllustration } from '../../shared/ui/brand/envelope-illustration';
import { Button } from '../../shared/ui/button/button';
import { DialogLauncher } from '../../shared/ui/dialog/dialog-launcher';
import { Icon } from '../../shared/ui/icon/icon';
import { Pagination } from '../../shared/ui/pagination/pagination';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { StatusLabel } from '../../shared/ui/status-label/status-label';
import {
  INVITE_DATE_FORMAT,
  INVITE_SHORT_DATE_FORMAT,
  INVITE_STATUS_DISPLAY,
  inviteMetaDate,
  inviteSummary,
} from './invite-code-display';
import { InviteCodeDialog } from './invite-code-dialog/invite-code-dialog';
import { InviteCodesApi } from './invite-codes-api';
import { InviteCodePage } from './invite-codes.models';

const SKELETON_ROWS = 4;

type PageView =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'empty' }
  | { readonly kind: 'list'; readonly data: InviteCodePage };

/**
 * Admin page listing invite codes, newest first, 20 per page, with a dialog to create one
 * (docs/design.md §10). The route guard and the API both require an admin; if the API answers 403
 * mid-session (admin rights removed), the no-access page is shown.
 */
@Component({
  selector: 'app-invite-codes-page',
  imports: [DatePipe, Banner, Button, EnvelopeIllustration, Icon, Pagination, Spinner, StatusLabel],
  templateUrl: './invite-codes-page.html',
  host: { class: 'flex flex-col lg:gap-4.5 lg:px-7 lg:pt-6 lg:pb-5' },
})
export class InviteCodesPage {
  private readonly api = inject(InviteCodesApi);
  private readonly dialogs = inject(DialogLauncher);
  private readonly clipboard = inject(Clipboard);
  private readonly announcer = inject(LiveAnnouncer);
  private readonly router = inject(Router);

  protected readonly dateFormat = INVITE_DATE_FORMAT;
  protected readonly shortDateFormat = INVITE_SHORT_DATE_FORMAT;
  protected readonly statusDisplay = INVITE_STATUS_DISPLAY;
  protected readonly metaDate = inviteMetaDate;
  protected readonly skeletonRows = Array.from({ length: SKELETON_ROWS }, (_, index) => index);

  protected readonly page = signal(1);

  // A new page cancels the request for the previous one, so a slow response cannot win.
  private readonly invites = rxResource({
    params: () => ({ page: this.page() }),
    stream: ({ params }) => this.api.list(params.page),
  });

  // A reload (after creating a code) keeps the current rows on screen; a page change shows loading.
  protected readonly view = computed<PageView>(() => {
    const status = this.invites.status();
    if (status === 'error') {
      return { kind: 'error' };
    }
    if (status === 'loading' || !this.invites.hasValue()) {
      return { kind: 'loading' };
    }
    const data = this.invites.value();
    return data.items.length > 0 ? { kind: 'list', data } : { kind: 'empty' };
  });

  protected readonly summary = computed(() => {
    const view = this.view();
    return view.kind === 'list' ? inviteSummary(view.data) : null;
  });

  protected readonly loading = this.invites.isLoading;

  constructor() {
    effect(() => {
      if (isForbidden(this.invites.error())) {
        untracked(() => this.showNoAccess());
      }
    });
  }

  protected openCreateDialog(): void {
    const ref = this.dialogs.open(InviteCodeDialog);
    ref.componentInstance?.created.subscribe(() => this.showNewest());
  }

  protected goToPage(page: number): void {
    this.page.set(page);
  }

  protected retry(): void {
    this.invites.reload();
  }

  protected copy(code: string): void {
    const copied = this.clipboard.copy(code);
    this.announcer.announce(copied ? 'Code copied to clipboard' : 'Couldn’t copy the code');
  }

  /** New codes are listed first, so after creating one the first page is shown. */
  private showNewest(): void {
    if (this.page() === 1) {
      this.invites.reload();
    } else {
      this.page.set(1);
    }
  }

  private showNoAccess(): void {
    void this.router.navigateByUrl(AUTH_ROUTES.noAccess, { skipLocationChange: true });
  }
}
