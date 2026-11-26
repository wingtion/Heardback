import { Component, inject } from '@angular/core';
import { Button } from '../../shared/ui/button/button';
import { DialogLauncher } from '../../shared/ui/dialog/dialog-launcher';
import { Icon } from '../../shared/ui/icon/icon';
import { AuthService } from '../auth/auth.service';
import { roleLabel } from '../auth/role-label';
import { ChangePasswordForm } from './change-password-form';
import { DataExportSection } from './data-export-section';
import { DeleteAccountDialog } from './delete-account-dialog';
import { ThemeSetting } from './theme-setting';

const SECTION = 'flex flex-col gap-3 rounded-md border border-rule bg-base px-4 py-3.5';
const SECTION_TITLE = 'text-body font-semibold text-ink';

/**
 * Settings (PLAN.md Phase 7, docs/design.md §20): the account with change password, the theme for this
 * device, the data export, and deleting the account last. Composed from the existing design system.
 */
@Component({
  selector: 'app-settings-page',
  imports: [Button, ChangePasswordForm, DataExportSection, Icon, ThemeSetting],
  host: { class: 'flex flex-col gap-4 p-4 lg:max-w-200 lg:gap-4.5 lg:px-7 lg:pt-6 lg:pb-5' },
  template: `
    <h1 class="font-serif text-page-title-sm font-semibold text-ink lg:text-page-title">
      Settings
    </h1>

    <section aria-labelledby="settings-account" [class]="section">
      <h2 id="settings-account" [class]="sectionTitle">Account</h2>
      @if (auth.user(); as user) {
        <dl class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-1.5 text-small">
          <dt class="text-ink-muted">Name</dt>
          <dd class="min-w-0 break-words text-ink">{{ user.displayName }}</dd>
          @if (!user.isDemo) {
            <dt class="text-ink-muted">Email</dt>
            <dd class="min-w-0 break-words text-ink">{{ user.email }}</dd>
          }
          <dt class="text-ink-muted">Role</dt>
          <dd class="text-ink">{{ roleLabel(user) }}</dd>
        </dl>
      }
      @if (auth.isDemo()) {
        <p class="border-t border-rule pt-3.5 text-small text-ink-2">
          Demo accounts have no password. Sign out to end the demo.
        </p>
      } @else {
        <app-change-password-form class="border-t border-rule pt-3.5" />
      }
    </section>

    <section aria-labelledby="settings-appearance" [class]="section">
      <h2 id="settings-appearance" [class]="sectionTitle">Appearance</h2>
      <app-theme-setting />
    </section>

    <section aria-labelledby="settings-data" [class]="section">
      <h2 id="settings-data" [class]="sectionTitle">Your data</h2>
      <app-data-export-section />
    </section>

    @if (!auth.isDemo()) {
      <section aria-labelledby="settings-delete" [class]="section">
        <h2 id="settings-delete" [class]="sectionTitle">Delete account</h2>
        <p class="text-small text-ink-2">
          Deletes your account and everything in it: applications with their timelines and notes,
          your CV and AI suggestions. Stats from other members stop counting you at once. Download
          your data first if you want to keep a copy.
        </p>
        <div>
          <button
            appButton="danger"
            size="sm"
            type="button"
            class="max-lg:h-12 max-lg:w-full max-lg:text-ui-lg"
            (click)="confirmDelete()"
          >
            <app-icon name="trash" [size]="14" />
            Delete account
          </button>
        </div>
      </section>
    }
  `,
})
export class SettingsPage {
  protected readonly auth = inject(AuthService);
  protected readonly roleLabel = roleLabel;
  private readonly dialogs = inject(DialogLauncher);

  protected readonly section = SECTION;
  protected readonly sectionTitle = SECTION_TITLE;

  protected confirmDelete(): void {
    this.dialogs.open(DeleteAccountDialog, { role: 'alertdialog' });
  }
}
