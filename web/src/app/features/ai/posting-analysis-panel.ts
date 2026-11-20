import { DatePipe } from '@angular/common';
import { Component, DestroyRef, computed, effect, inject, input, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Banner } from '../../shared/ui/banner/banner';
import { Button } from '../../shared/ui/button/button';
import { Icon } from '../../shared/ui/icon/icon';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { SkillChip } from '../cv/skill-chip';
import { AiApi } from './ai-api';
import { AiTag } from './ai-tag';
import { quotaResetOf, resetTime, usageLine } from './ai-usage';
import {
  IN_PROGRESS,
  POLL_INTERVAL_MS,
  PostingAnalysis,
  PostingSeniority,
  PostingWorkMode,
} from './ai.models';

const SENIORITY: Record<PostingSeniority, string> = {
  Unknown: 'Not stated',
  Intern: 'Intern',
  Junior: 'Junior',
  Mid: 'Mid-level',
  Senior: 'Senior',
  Lead: 'Lead',
};

const WORK_MODE: Record<PostingWorkMode, string> = {
  Unknown: 'Not stated',
  Onsite: 'On-site',
  Hybrid: 'Hybrid',
  Remote: 'Remote',
};

const NOT_STATED = 'Not stated';

type RequestProblem =
  { readonly kind: 'quota'; readonly resetsAt: string } | { readonly kind: 'failed' };

/**
& (docs/design.md §16): on request, an LLM reads the posting text
 * into a few fields. Hidden while loading, when AI is off and when there is no posting text (the match panel
 * already asks for it). Checks again every few seconds while the analysis is queued or running.
 */
@Component({
  selector: 'app-posting-analysis-panel',
  imports: [DatePipe, AiTag, Banner, Button, Icon, SkillChip, Spinner],
  host: { class: 'flex flex-col gap-2 empty:hidden' },
  template: `
    @if (visible()) {
      <h3
        class="border-t border-dashed border-field-border pt-2.5 text-cell font-semibold text-ink"
      >
        About this job
      </h3>
      <div class="flex items-center justify-between gap-2">
        <h4 class="text-label font-semibold text-ink-2">Job summary</h4>
        <app-ai-tag />
      </div>

      @switch (view()) {
        @case ('error') {
          <div role="alert" class="flex flex-wrap items-center gap-2 text-small text-danger">
            <app-icon name="warning" [size]="14" />
            Couldn’t load the analysis.
            <button appButton size="xs" type="button" (click)="reload()">Try again</button>
          </div>
        }
        @case ('NotRequested') {
          <p class="text-small text-ink-2">
            AI can summarize this job for you: title, seniority, work mode, location and the skills
            it asks for.
          </p>
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
                Summarize job
              }
            </button>
            @if (usage(); as text) {
              <span class="font-mono text-label text-ink-muted">{{ text }}</span>
            }
          </div>
        }
        @case ('Queued') {
          <div role="status" class="flex items-center gap-2 text-small text-ink-2">
            <app-spinner />
            Summarizing the job…
          </div>
        }
        @case ('Running') {
          <div role="status" class="flex items-center gap-2 text-small text-ink-2">
            <app-spinner />
            Summarizing the job…
          </div>
        }
        @case ('WaitingForCapacity') {
          <div role="status" class="flex items-start gap-2 text-small text-status-no-response">
            <app-icon name="clock" [size]="14" class="mt-0.5" />
            AI has reached its daily limit. Your summary will be ready after {{ resetsAt() }}.
          </div>
        }
        @case ('Failed') {
          <div role="alert" class="flex flex-wrap items-center gap-2 text-small text-danger">
            <app-icon name="warning" [size]="14" />
            Couldn’t summarize this job.
            <button appButton size="xs" type="button" [disabled]="requesting()" (click)="request()">
              Try again
            </button>
          </div>
        }
        @case ('Ready') {
          @if (fields(); as fields) {
            <dl class="grid grid-cols-[auto_1fr] gap-x-3 gap-y-1 text-small">
              @for (row of rows(); track row.label) {
                <dt class="text-ink-muted">{{ row.label }}</dt>
                <dd class="text-ink" [class.text-ink-muted]="row.value === notStated">
                  {{ row.value }}
                </dd>
              }
            </dl>
            @if (fields.skills.length > 0) {
              <ul class="flex flex-wrap gap-1.5" aria-label="Skills the job asks for">
                @for (skill of fields.skills; track skill) {
                  <li><app-skill-chip kind="found" [name]="skill" /></li>
                }
              </ul>
            }
            <p class="text-caption text-ink-muted">
              Summarized by AI from the job description on
              <span class="font-mono">{{ fields.analyzedAt | date: 'd MMM y' }}</span>.
            </p>
          }
        }
      }

      @switch (problem()?.kind) {
        @case ('quota') {
          <app-banner kind="warning" alert heading="Daily limit reached">
            You’ve used all {{ limit() }} job summaries for today. You can summarize more after
            {{ problemReset() }}.
          </app-banner>
        }
        @case ('failed') {
          <app-banner kind="error" heading="Couldn’t start the summary">
            The server didn’t respond. Try again.
          </app-banner>
        }
      }
    }
  `,
})
export class PostingAnalysisPanel {
  private readonly api = inject(AiApi);
  private readonly destroyRef = inject(DestroyRef);

  readonly applicationId = input.required<string>();
  /** The application's row version: an edit can change the posting text, so the panel loads again. */
  readonly version = input.required<number>();

  protected readonly notStated = NOT_STATED;

  private readonly polls = signal(0);
  protected readonly requesting = signal(false);
  protected readonly problem = signal<RequestProblem | null>(null);

  private readonly resource = rxResource({
    params: () => ({ id: this.applicationId(), version: this.version(), poll: this.polls() }),
    stream: ({ params }) => this.api.postingAnalysis(params.id),
  });

  private readonly analysis = computed<PostingAnalysis | null>(() =>
    this.resource.hasValue() ? this.resource.value() : null,
  );

  protected readonly view = computed(() =>
    this.resource.status() === 'error' ? 'error' : (this.analysis()?.state ?? 'loading'),
  );

  protected readonly visible = computed(() => {
    const view = this.view();
    return view !== 'loading' && view !== 'Unavailable' && view !== 'NoPostingText';
  });

  protected readonly fields = computed(() => this.analysis()?.analysis ?? null);

  protected readonly rows = computed(() => {
    const fields = this.fields();
    if (!fields) {
      return [];
    }
    return [
      { label: 'Company', value: fields.company ?? NOT_STATED },
      { label: 'Title', value: fields.title ?? NOT_STATED },
      { label: 'Seniority', value: SENIORITY[fields.seniority] },
      { label: 'Work mode', value: WORK_MODE[fields.workMode] },
      { label: 'Location', value: fields.location ?? NOT_STATED },
    ];
  });

  protected readonly usage = computed(() => {
    const usage = this.analysis()?.usage;
    return usage ? usageLine(usage) : null;
  });

  protected readonly resetsAt = computed(() => {
    const usage = this.analysis()?.usage;
    return usage ? resetTime(usage.resetsAt) : '';
  });

  protected readonly limit = computed(() => this.analysis()?.usage.limit ?? 0);

  protected readonly problemReset = computed(() => {
    const problem = this.problem();
    return problem?.kind === 'quota' ? resetTime(problem.resetsAt) : '';
  });

  constructor() {
    // While the analysis is queued or running, check again shortly.
    effect((onCleanup) => {
      const state = this.analysis()?.state;
      if (!state || !(IN_PROGRESS as readonly string[]).includes(state)) {
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
    this.requesting.set(true);
    this.problem.set(null);
    this.api
      .requestPostingAnalysis(this.applicationId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (analysis) => {
          this.requesting.set(false);
          this.resource.set(analysis);
        },
        error: (error: unknown) => {
          this.requesting.set(false);
          const resetsAt = quotaResetOf(error);
          this.problem.set(resetsAt ? { kind: 'quota', resetsAt } : { kind: 'failed' });
        },
      });
  }
}
