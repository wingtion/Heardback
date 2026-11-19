import { DatePipe } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Banner } from '../../shared/ui/banner/banner';
import { Button } from '../../shared/ui/button/button';
import { DialogLauncher } from '../../shared/ui/dialog/dialog-launcher';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { APPLICATION_DATE_FORMAT } from '../applications/application-display';
import { AiApi } from './ai-api';
import { WithdrawConsentDialog } from './withdraw-consent-dialog';

/**
 * "AI suggestions" on the CV page (docs/design.md §16): whether the user lets Heardback send their CV text to
 * the AI provider, with what is sent and to whom. Hidden while loading and when AI is off on the server.
 */
@Component({
  selector: 'app-ai-consent-section',
  imports: [DatePipe, Banner, Button, Spinner],
  host: { class: 'empty:hidden' },
  template: `
    @if (consent(); as consent) {
      @if (consent.available) {
        <section
          aria-labelledby="ai-consent-title"
          class="flex flex-col gap-2 rounded-md border border-rule bg-base px-4 py-3"
        >
          <h2 id="ai-consent-title" class="text-body font-semibold text-ink">AI suggestions</h2>
          <p class="text-small text-ink-2">
            For suggestions on an application, Heardback sends your CV text as stored here (emails,
            phone numbers, links and addresses already removed) and that posting to Groq, an AI
            provider. It is never sent to providers that may use it to train models.
          </p>
          @if (consent.allowed) {
            <p class="text-small text-ink">
              Allowed since
              <span class="font-mono">{{ consent.allowedAt | date: dateFormat }}</span>.
            </p>
            <div>
              <button
                appButton="danger"
                size="sm"
                type="button"
                class="max-lg:h-11"
                (click)="confirmWithdraw()"
              >
                Stop sending my CV
              </button>
            </div>
          } @else {
            <p class="text-small text-ink">Not allowed. Your CV text is not sent anywhere.</p>
            <div>
              <button
                appButton
                size="sm"
                type="button"
                class="max-lg:h-11"
                [disabled]="saving()"
                [attr.aria-busy]="saving() || null"
                (click)="allow()"
              >
                @if (saving()) {
                  <app-spinner tone="current" />
                  Saving…
                } @else {
                  Allow
                }
              </button>
            </div>
          }
          @if (failed()) {
            <app-banner kind="error" heading="Couldn’t save your choice">
              The server didn’t respond. Try again.
            </app-banner>
          }
        </section>
      }
    }
  `,
})
export class AiConsentSection {
  private readonly api = inject(AiApi);
  private readonly dialogs = inject(DialogLauncher);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly dateFormat = APPLICATION_DATE_FORMAT;

  private readonly resource = rxResource({ stream: () => this.api.consent() });

  protected readonly consent = computed(() =>
    this.resource.hasValue() ? this.resource.value() : null,
  );

  protected readonly saving = signal(false);
  protected readonly failed = signal(false);

  protected allow(): void {
    this.saving.set(true);
    this.failed.set(false);
    this.api
      .setConsent(true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (consent) => {
          this.saving.set(false);
          this.resource.set(consent);
        },
        error: () => {
          this.saving.set(false);
          this.failed.set(true);
        },
      });
  }

  protected confirmWithdraw(): void {
    this.failed.set(false);
    const ref = this.dialogs.open(WithdrawConsentDialog, { role: 'alertdialog' });
    ref.componentInstance?.withdrawn.subscribe((consent) => this.resource.set(consent));
  }
}
