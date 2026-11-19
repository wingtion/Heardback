import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Banner } from '../../shared/ui/banner/banner';
import { EnvelopeIllustration } from '../../shared/ui/brand/envelope-illustration';
import { Button } from '../../shared/ui/button/button';
import { Icon } from '../../shared/ui/icon/icon';
import { Link } from '../../shared/ui/link/link';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { SHELL_ROUTES } from '../../shell/nav-items';
import { CHANNEL_LABELS, localToday, noResponseLabel } from '../applications/application-display';
import { ApplicationsApi } from '../applications/applications-api';
import { SELECTED_PARAM, STATUS_PARAM } from '../applications/applications-page';
import { InsightsApi } from './insights-api';
import { FOLLOWED_UP_NOTE, FollowUp } from './insights.models';
import { MeterBar } from './meter-bar';
import { WeeklyChart } from './weekly-chart';

/** "1 of 3": the counts behind a rate, so a small sample is never mistaken for a trend. */
export function ofText(part: number, whole: number): string {
  return `${part} of ${whole}`;
}

/** "6.5 days", "1 day", "0 days". */
export function daysText(days: number): string {
  return `${days} ${days === 1 ? 'day' : 'days'}`;
}

/**
 * Overview (docs/design.md §17, ADR-0011; the Insights module's page): what to follow up on first, then the user's own numbers. Built from
 * the existing design system with hand-drawn SVG charts (decided by the author on 2026-11-19).
 */
@Component({
  selector: 'app-insights-page',
  imports: [
    RouterLink,
    Banner,
    Button,
    EnvelopeIllustration,
    Icon,
    Link,
    MeterBar,
    Spinner,
    WeeklyChart,
  ],
  templateUrl: './insights-page.html',
  host: { class: 'flex flex-col gap-4 p-4 lg:max-w-240 lg:gap-4.5 lg:px-7 lg:pt-6 lg:pb-5' },
})
export class InsightsPage {
  private readonly api = inject(InsightsApi);
  private readonly applicationsApi = inject(ApplicationsApi);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly channelLabels = CHANNEL_LABELS;
  protected readonly noResponseLabel = noResponseLabel;
  protected readonly ofText = ofText;
  protected readonly daysText = daysText;
  protected readonly applicationsPath = SHELL_ROUTES.applications;
  protected readonly noResponseQuery = { [STATUS_PARAM]: 'NoResponse' };

  private readonly dashboard = rxResource({ stream: () => this.api.dashboard() });
  private readonly followUps = rxResource({ stream: () => this.api.followUps() });

  protected readonly view = computed(() => {
    if (this.dashboard.status() === 'error') {
      return 'error';
    }
    if (!this.dashboard.hasValue()) {
      return 'loading';
    }
    return this.dashboard.value().totalApplications === 0 ? 'empty' : 'ready';
  });

  protected readonly numbers = computed(() =>
    this.dashboard.hasValue() ? this.dashboard.value() : null,
  );

  protected readonly followUpPage = computed(() =>
    this.followUps.hasValue() ? this.followUps.value() : null,
  );

  protected readonly followUpsFailed = computed(() => this.followUps.status() === 'error');

  /** Applications being marked as followed up, and the one that failed. */
  protected readonly marking = signal<ReadonlySet<string>>(new Set());
  protected readonly markFailed = signal<string | null>(null);

  protected openQuery(followUp: FollowUp): Record<string, string> {
    return { [SELECTED_PARAM]: followUp.applicationId };
  }

  protected retry(): void {
    this.dashboard.reload();
    this.followUps.reload();
  }

  protected reloadFollowUps(): void {
    this.followUps.reload();
  }

  /** Adds the "Followed up" note dated today, which moves the last activity and takes it off the list. */
  protected markFollowedUp(followUp: FollowUp): void {
    const id = followUp.applicationId;
    this.marking.update((ids) => new Set([...ids, id]));
    this.markFailed.set(null);
    this.applicationsApi
      .addEvent(id, { status: null, occurredOn: localToday(new Date()), note: FOLLOWED_UP_NOTE })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.marking.update((ids) => new Set([...ids].filter((other) => other !== id)));
          this.followUps.reload();
        },
        error: () => {
          this.marking.update((ids) => new Set([...ids].filter((other) => other !== id)));
          this.markFailed.set(id);
        },
      });
  }
}
