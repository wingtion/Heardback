import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { isForbidden } from '../../core/http-errors';
import { Banner } from '../../shared/ui/banner/banner';
import { Button } from '../../shared/ui/button/button';
import { DialogLauncher } from '../../shared/ui/dialog/dialog-launcher';
import { Pagination } from '../../shared/ui/pagination/pagination';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { AUTH_ROUTES } from '../auth/auth-paths';
import { MembersApi } from './members-api';
import { Member, MemberPage } from './members.models';
import { ResetCodeDialog, ResetCodeDialogData } from './reset-code-dialog';

/** "12 Nov 2026" in the table, "12 Nov" for an open code's expiry in the list. */
export const MEMBER_DATE_FORMAT = 'dd MMM yyyy';
export const MEMBER_SHORT_DATE_FORMAT = 'dd MMM';

const SKELETON_ROWS = 4;

type SectionView =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'list'; readonly data: MemberPage };

/** "14 members". */
export function memberSummary(totalCount: number): string {
  return `${totalCount} ${totalCount === 1 ? 'member' : 'members'}`;
}

/**
 * The member list on the admin Members page (ADR-0015, docs/design.md §20): name, email, role, joined, and a
 * reset code for members who forgot their password. Admins get no button: they recover with the owner command.
 */
@Component({
  selector: 'app-members-section',
  imports: [DatePipe, Banner, Button, Pagination, Spinner],
  templateUrl: './members-section.html',
  host: { class: 'flex flex-col lg:gap-3.5' },
})
export class MembersSection {
  private readonly api = inject(MembersApi);
  private readonly dialogs = inject(DialogLauncher);
  private readonly router = inject(Router);

  protected readonly dateFormat = MEMBER_DATE_FORMAT;
  protected readonly shortDateFormat = MEMBER_SHORT_DATE_FORMAT;
  protected readonly skeletonRows = Array.from({ length: SKELETON_ROWS }, (_, index) => index);

  protected readonly page = signal(1);

  private readonly members = rxResource({
    params: () => ({ page: this.page() }),
    stream: ({ params }) => this.api.list(params.page),
  });

  // A reload (after creating a code) keeps the rows on screen; a page change shows loading.
  protected readonly view = computed<SectionView>(() => {
    if (this.members.status() === 'error') {
      return { kind: 'error' };
    }
    if (this.members.status() === 'loading' || !this.members.hasValue()) {
      return { kind: 'loading' };
    }
    return { kind: 'list', data: this.members.value() };
  });

  protected readonly summary = computed(() => {
    const view = this.view();
    return view.kind === 'list' ? memberSummary(view.data.totalCount) : null;
  });

  protected readonly loading = this.members.isLoading;

  /** Whether any admin is on this page, to explain why they have no button. */
  protected readonly hasAdmins = computed(() => {
    const view = this.view();
    return view.kind === 'list' && view.data.items.some((item) => item.isAdmin);
  });

  constructor() {
    effect(() => {
      if (isForbidden(this.members.error())) {
        untracked(
          () => void this.router.navigateByUrl(AUTH_ROUTES.noAccess, { skipLocationChange: true }),
        );
      }
    });
  }

  protected openResetCode(member: Member): void {
    const data: ResetCodeDialogData = { member };
    const ref = this.dialogs.open(ResetCodeDialog, { data });
    ref.componentInstance?.created.subscribe(() => this.members.reload());
    ref.componentInstance?.gone.subscribe(() => this.members.reload());
  }

  protected goToPage(page: number): void {
    this.page.set(page);
  }

  protected retry(): void {
    this.members.reload();
  }
}
