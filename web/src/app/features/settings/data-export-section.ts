import { HttpStatusCode } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { httpErrorOf } from '../../core/http-errors';
import { FileSaver } from '../../core/save-file';
import { Banner } from '../../shared/ui/banner/banner';
import { Button } from '../../shared/ui/button/button';
import { InlineSuccess } from '../../shared/ui/inline-success/inline-success';
import { Link } from '../../shared/ui/link/link';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { retryAfterSeconds } from '../auth/auth-errors';
import { AUTH_ROUTES } from '../auth/auth-paths';
import { AccountExportApi, EXPORTS_PER_HOUR, exportFileName } from './account-export-api';

const SECONDS_PER_MINUTE = 60;

type ExportProblem =
  { readonly kind: 'limit'; readonly minutes: number } | { readonly kind: 'unavailable' };

/** "Your data" on the Settings page: download everything as one JSON file (ADR-0015). */
@Component({
  selector: 'app-data-export-section',
  imports: [RouterLink, Banner, Button, InlineSuccess, Link, Spinner],
  host: { class: 'flex flex-col gap-2.5' },
  template: `
    <p class="text-small text-ink-2">
      Download a copy of everything Heardback keeps about you as one JSON file: your account, your
      applications with their timelines and notes, job descriptions and job summaries, your CV text
      and skills, and AI suggestions. Other members’ data is not in it.
    </p>

    <div class="flex flex-wrap items-center gap-x-3 gap-y-2">
      <button
        appButton
        size="sm"
        type="button"
        class="max-lg:h-11"
        [disabled]="downloading()"
        [attr.aria-busy]="downloading() || null"
        (click)="download()"
      >
        @if (downloading()) {
          <app-spinner tone="current" />
          Preparing your file…
        } @else {
          Download my data
        }
      </button>
      <a appLink [routerLink]="privacyPath" class="text-small">Read the privacy page</a>
    </div>

    <div role="status" class="empty:hidden">
      @if (downloaded()) {
        <app-inline-success>Your file is downloading</app-inline-success>
      }
    </div>

    @if (problem(); as problem) {
      @switch (problem.kind) {
        @case ('limit') {
          <app-banner kind="warning" alert heading="Download limit reached">
            You can download your data {{ perHour }} times an hour. Try again in about
            <span class="font-mono font-medium">{{ problem.minutes }}</span>
            {{ problem.minutes === 1 ? 'minute' : 'minutes' }}.
          </app-banner>
        }
        @case ('unavailable') {
          <app-banner kind="error" heading="Couldn’t prepare your file">
            The server didn’t respond. Try again.
          </app-banner>
        }
      }
    }
  `,
})
export class DataExportSection {
  private readonly api = inject(AccountExportApi);
  private readonly files = inject(FileSaver);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly privacyPath = AUTH_ROUTES.privacy;
  protected readonly perHour = EXPORTS_PER_HOUR;
  protected readonly downloading = signal(false);
  protected readonly downloaded = signal(false);
  protected readonly problem = signal<ExportProblem | null>(null);

  protected download(): void {
    if (this.downloading()) {
      return;
    }
    this.downloading.set(true);
    this.downloaded.set(false);
    this.problem.set(null);
    this.api
      .download()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (file) => {
          this.downloading.set(false);
          this.files.save(file, exportFileName(new Date()));
          this.downloaded.set(true);
        },
        error: (error: unknown) => {
          this.downloading.set(false);
          this.problem.set(toProblem(error));
        },
      });
  }
}

function toProblem(error: unknown): ExportProblem {
  const response = httpErrorOf(error);
  if (response?.status === HttpStatusCode.TooManyRequests) {
    return {
      kind: 'limit',
      minutes: Math.max(1, Math.ceil(retryAfterSeconds(response) / SECONDS_PER_MINUTE)),
    };
  }
  return { kind: 'unavailable' };
}
