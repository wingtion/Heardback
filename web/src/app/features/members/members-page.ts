import { Component } from '@angular/core';
import { InviteCodesSection } from '../invite-codes/invite-codes-section';
import { MembersSection } from './members-section';

/**
 * Admin page (docs/design.md §20): who is in the group, with reset codes for forgotten passwords, and the
 * invite codes for letting someone new in. Replaces the earlier Invite codes page.
 */
@Component({
  selector: 'app-members-page',
  imports: [InviteCodesSection, MembersSection],
  host: { class: 'flex flex-col gap-6 lg:gap-8 lg:px-7 lg:pt-6 lg:pb-5' },
  template: `
    <h1
      class="px-4 pt-4 font-serif text-page-title-sm font-semibold text-ink lg:p-0 lg:text-page-title"
    >
      Members
    </h1>
    <app-members-section />
    <app-invite-codes-section />
  `,
})
export class MembersPage {}
