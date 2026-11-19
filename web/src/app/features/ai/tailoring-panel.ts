import { Component, DestroyRef, computed, effect, inject, input, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NgTemplateOutlet } from '@angular/common';
import { RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';
import { Banner } from '../../shared/ui/banner/banner';
import { Button } from '../../shared/ui/button/button';
import { Icon } from '../../shared/ui/icon/icon';
import { Link } from '../../shared/ui/link/link';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { SHELL_ROUTES } from '../../shell/nav-items';
import { AiApi } from './ai-api';
import { AiTag } from './ai-tag';
import { quotaResetOf, resetTime, usageLine } from './ai-usage';
import { IN_PROGRESS, POLL_INTERVAL_MS, Tailoring } from './ai.models';

type RequestProblem =
  { readonly kind: 'quota'; readonly resetsAt: string } | { readonly kind: 'failed' };

/** States in which the panel shows nothing: AI is off, or the match panel already asks for what is missing. */
const HIDDEN = ['loading', 'Unavailable', 'NoPostingText', 'NoCv'];

/**
 * "How to improve your CV" (tailoring suggestions) under the skills match (docs/design.md §16): AI suggestions for reworking what the CV already says
 * for this posting. The first request asks for consent in place ("Allow and get suggestions", decided by the
 * author on 2026-11-19); it is withdrawn on the CV page. Suggestions are always labelled as written by AI.
 */
@Component({
  selector: 'app-tailoring-panel',
  imports: [NgTemplateOutlet, RouterLink, AiTag, Banner, Button, Icon, Link, Spinner],
  host: { class: 'flex flex-col gap-2 empty:hidden' },
  template: `
    @if (visible()) {
      <div class="flex items-center justify-between gap-2">
        <h4 class="text-label font-semibold text-ink-2">How to improve your CV</h4>
        <app-ai-tag />
      </div>

      @switch (view()) {
        @case ('error') {
          <div role="alert" class="flex flex-wrap items-center gap-2 text-small text-danger">
            <app-icon name="warning" [size]="14" />
            Couldn’t load the suggestions.
            <button appButton size="xs" type="button" (click)="reload()">Try again</button>
          </div>
        }
        @case ('ConsentRequired') {
          <p class="text-small text-ink-2">
            AI can suggest how to rework what your CV already says for this posting.
          </p>
          <p class="text-caption text-ink-muted">
            To do that, Heardback sends your CV text (emails, phone numbers, links and addresses
            already removed) and this posting to Groq, an AI provider. You can stop this at any time
            on the
            <a appLink [routerLink]="cvPath">CV page</a>.
          </p>
          <div>
            <button
              appButton="primary"
              size="xs"
              type="button"
              class="max-lg:h-11"
              [disabled]="requesting()"
              [attr.aria-busy]="requesting() || null"
              (click)="allowAndRequest()"
            >
              @if (requesting()) {
                <app-spinner tone="current" />
                Starting…
              } @else {
                Allow and get suggestions
              }
            </button>
          </div>
        }
        @case ('NotRequested') {
          <p class="text-small text-ink-2">
            AI can suggest how to rework what your CV already says for this posting.
          </p>
          <ng-container *ngTemplateOutlet="requestRow" />
        }
        @case ('Failed') {
          <div role="alert" class="flex flex-wrap items-center gap-2 text-small text-danger">
            <app-icon name="warning" [size]="14" />
            Couldn’t write suggestions.
            <button appButton size="xs" type="button" [disabled]="requesting()" (click)="request()">
              Try again
            </button>
          </div>
        }
      }

      @if (inProgress()) {
        <div role="status" class="flex items-center gap-2 text-small text-ink-2">
          <app-spinner />
          Writing suggestions…
        </div>
      }
      @if (view() === 'WaitingForCapacity') {
        <div role="status" class="flex items-start gap-2 text-small text-status-no-response">
          <app-icon name="clock" [size]="14" class="mt-0.5" />
          AI is busy for today. The suggestions are written after the daily reset at
          {{ resetsAt() }}.
        </div>
      }

      @if (details(); as details) {
        @if (details.isOutdated && !inProgress() && view() !== 'WaitingForCapacity') {
          <div
            class="flex flex-col gap-1.5 rounded-md border border-field-border bg-surface px-3 py-2"
          >
            <p class="flex items-start gap-2 text-small text-ink-2">
              <app-icon name="info" [size]="14" class="mt-0.5" />
              Your CV or this posting changed since these suggestions were written.
            </p>
            <ng-container
              *ngTemplateOutlet="requestRow; context: { $implicit: 'Update suggestions' }"
            />
          </div>
        }
        <p class="text-small text-ink">{{ details.summary }}</p>
        <ol class="flex flex-col gap-2.5" aria-label="Suggestions for your CV">
          @for (suggestion of details.suggestions; track $index) {
            <li class="flex flex-col gap-0.5 border-l-2 border-rule-strong pl-2.5">
              <span class="text-label font-semibold text-ink-2">{{ suggestion.cvSection }}</span>
              <q class="font-mono text-caption text-ink-muted">{{ suggestion.cvQuote }}</q>
              <span class="text-small text-ink">{{ suggestion.advice }}</span>
            </li>
          }
        </ol>
        <p class="flex items-start gap-1.5 text-caption text-ink-muted">
          <app-icon name="info" [size]="12" class="mt-0.5" />
          Written by AI from your CV and this posting. Check each suggestion before you use it.
        </p>
      }

      @switch (problem()?.kind) {
        @case ('quota') {
          <app-banner kind="warning" alert heading="You’ve used today’s suggestions">
            You can ask for more after the daily reset at {{ problemReset() }}.
          </app-banner>
        }
        @case ('failed') {
          <app-banner kind="error" heading="Couldn’t start the suggestions">
            The server didn’t respond. Try again.
          </app-banner>
        }
      }

      <ng-template #requestRow let-label>
        <div class="flex flex-wrap items-center gap-2.5">
          <button
            appButton
            size="xs"
            type="button"
            class="max-lg:h-11"
            [disabled]="requesting()"
            [attr.aria-busy]="requesting() || null"
            (click)="request()"
          >
            @if (requesting()) {
              <app-spinner tone="current" />
              Starting…
            } @else {
              {{ label ?? 'Get suggestions' }}
            }
          </button>
          @if (usage(); as text) {
            <span class="font-mono text-label text-ink-muted">{{ text }}</span>
          }
        </div>
      </ng-template>
    }
  `,
})
export class TailoringPanel {
  private readonly api = inject(AiApi);
  private readonly destroyRef = inject(DestroyRef);

  readonly applicationId = input.required<string>();
  /** The application's row version: an edit can change the posting, so the panel loads again. */
  readonly version = input.required<number>();

  protected readonly cvPath = SHELL_ROUTES.cv;

  private readonly polls = signal(0);
  protected readonly requesting = signal(false);
  protected readonly problem = signal<RequestProblem | null>(null);

  private readonly resource = rxResource({
    params: () => ({ id: this.applicationId(), version: this.version(), poll: this.polls() }),
    stream: ({ params }) => this.api.tailoring(params.id),
  });

  private readonly tailoring = computed<Tailoring | null>(() =>
    this.resource.hasValue() ? this.resource.value() : null,
  );

  protected readonly view = computed(() =>
    this.resource.status() === 'error' ? 'error' : (this.tailoring()?.state ?? 'loading'),
  );

  protected readonly visible = computed(() => !HIDDEN.includes(this.view()));

  protected readonly inProgress = computed(() =>
    (IN_PROGRESS as readonly string[]).includes(this.view()),
  );

  protected readonly details = computed(() => this.tailoring()?.tailoring ?? null);

  protected readonly usage = computed(() => {
    const usage = this.tailoring()?.usage;
    return usage ? usageLine(usage) : null;
  });

  protected readonly resetsAt = computed(() => {
    const usage = this.tailoring()?.usage;
    return usage ? resetTime(usage.resetsAt) : '';
  });

  protected readonly problemReset = computed(() => {
    const problem = this.problem();
    return problem?.kind === 'quota' ? resetTime(problem.resetsAt) : '';
  });

  constructor() {
    // While suggestions are queued or being written, check again shortly.
    effect((onCleanup) => {
      if (!this.inProgress()) {
        return;
      }
      const timer = setTimeout(() => this.polls.update((count) => count + 1), POLL_INTERVAL_MS);
      onCleanup(() => clearTimeout(timer));
    });
  }

  protected reload(): void {
    this.resource.reload();
  }

  protected request(): void {
    this.start(this.api.requestTailoring(this.applicationId()));
  }

  /** Records the consent, then asks for suggestions: one click, as the explanation above the button says. */
  protected allowAndRequest(): void {
    const id = this.applicationId();
    this.start(this.api.setConsent(true).pipe(switchMap(() => this.api.requestTailoring(id))));
  }

  private start(call: ReturnType<AiApi['requestTailoring']>): void {
    this.requesting.set(true);
    this.problem.set(null);
    call.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (tailoring) => {
        this.requesting.set(false);
        this.resource.set(tailoring);
      },
      error: (error: unknown) => {
        this.requesting.set(false);
        const resetsAt = quotaResetOf(error);
        this.problem.set(resetsAt ? { kind: 'quota', resetsAt } : { kind: 'failed' });
        this.resource.reload();
      },
    });
  }
}
