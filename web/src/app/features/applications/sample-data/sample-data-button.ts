import { Component, DestroyRef, computed, inject, output, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { Button } from '../../../shared/ui/button/button';
import { Icon } from '../../../shared/ui/icon/icon';
import { Spinner } from '../../../shared/ui/spinner/spinner';
import { AuthService } from '../../auth/auth.service';
import { ApplicationsApi } from '../applications-api';
import { SampleApplicationsResult } from '../applications.models';

/** "Added 8 sample applications" / "All sample applications are already in your list". */
export function samplesMessage(result: SampleApplicationsResult): string {
  if (result.added === 0) {
    return 'All sample applications are already in your list';
  }
  return `Added ${result.added} sample ${result.added === 1 ? 'application' : 'applications'}`;
}

/**
 * "Add sample applications" for admins, for trying the app out (decided by the author on 2026-11-19). Shown only
 * to admins on a server that turns it on (on in development, off in production); samples go to the admin's own
 * list. A tool for testing, so it is a small secondary button beside the page title, not a designed feature.
 */
@Component({
  selector: 'app-sample-data-button',
  imports: [Button, Icon, Spinner],
  host: { class: 'contents' },
  template: `
    @if (enabled()) {
      <button
        appButton
        size="sm"
        type="button"
        class="self-center"
        [disabled]="adding()"
        [attr.aria-busy]="adding() || null"
        (click)="add()"
      >
        @if (adding()) {
          <app-spinner tone="current" />
          Adding…
        } @else {
          Add sample applications
        }
      </button>
      <span role="status" class="self-center text-small empty:hidden">
        @if (message(); as text) {
          <span class="inline-flex items-center gap-1.5 text-ink-2">
            <app-icon name="check" [size]="14" class="text-status-offer" />
            {{ text }}
          </span>
        }
        @if (failed()) {
          <span class="text-danger">Couldn’t add the samples. Try again.</span>
        }
      </span>
    }
  `,
})
export class SampleDataButton {
  private readonly api = inject(ApplicationsApi);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  /** Samples were added; the page reloads its list and counts. */
  readonly added = output();

  // Members never ask: the endpoint is admin-only.
  private readonly status = rxResource({
    params: () => this.auth.isAdmin(),
    stream: ({ params: isAdmin }) =>
      isAdmin ? this.api.sampleDataStatus() : of({ enabled: false }),
  });

  protected readonly enabled = computed(
    () => this.status.hasValue() && this.status.value().enabled,
  );

  protected readonly adding = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly failed = signal(false);

  protected add(): void {
    this.adding.set(true);
    this.message.set(null);
    this.failed.set(false);
    this.api
      .addSampleApplications()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.adding.set(false);
          this.message.set(samplesMessage(result));
          if (result.added > 0) {
            this.added.emit();
          }
        },
        error: () => {
          this.adding.set(false);
          this.failed.set(true);
        },
      });
  }
}
