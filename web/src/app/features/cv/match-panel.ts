import { Component, computed, inject, input, output } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Button } from '../../shared/ui/button/button';
import { Icon } from '../../shared/ui/icon/icon';
import { Link } from '../../shared/ui/link/link';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { SHELL_ROUTES } from '../../shell/nav-items';
import { CvApi } from './cv-api';
import { SkillChip } from './skill-chip';

/** "3 of 5 skills · 60%". */
export function matchSummary(matched: number, total: number, score: number): string {
  return `${matched} of ${total} ${total === 1 ? 'skill' : 'skills'} · ${score}%`;
}

/**
 * "Skills match" in the application detail (docs/design.md §15): the skills the posting names, split into those
 * the CV shows and those it doesn't, with the share matched as plain text. No gauge: the theme stays in the
 * envelope, not in a score. Reloads when the application's version changes (after Edit or Add update).
 */
@Component({
  selector: 'app-match-panel',
  imports: [RouterLink, Button, Icon, Link, SkillChip, Spinner],
  host: { class: 'flex flex-col gap-2' },
  template: `
    <div class="flex items-baseline justify-between gap-2">
      <h4 class="text-label font-semibold text-ink-2">Skills match</h4>
      @if (summary(); as text) {
        <span class="font-mono text-label text-ink tabular-nums">{{ text }}</span>
      }
    </div>

    @switch (view()) {
      @case ('loading') {
        <div role="status" class="flex items-center gap-2 text-small text-ink-2">
          <app-spinner />
          Checking your CV…
        </div>
      }
      @case ('error') {
        <div role="alert" class="flex flex-wrap items-center gap-2 text-small text-danger">
          <app-icon name="warning" [size]="14" />
          Couldn’t check your CV.
          <button appButton size="xs" type="button" (click)="retry()">Try again</button>
        </div>
      }
      @case ('Ready') {
        @if (match(); as match) {
          @if (match.matched.length > 0) {
            <p class="text-caption text-ink-muted">Your CV shows</p>
            <ul class="flex flex-wrap gap-1.5" aria-label="Skills your CV shows">
              @for (skill of match.matched; track skill.name) {
                <li><app-skill-chip kind="matched" [name]="skill.name" /></li>
              }
            </ul>
          }
          @if (match.missing.length > 0) {
            <p class="text-caption text-ink-muted">Missing from your CV</p>
            <ul class="flex flex-wrap gap-1.5" aria-label="Skills missing from your CV">
              @for (skill of match.missing; track skill.name) {
                <li><app-skill-chip kind="missing" [name]="skill.name" /></li>
              }
            </ul>
          }
          <p class="text-caption text-ink-muted">
            Skills found in the role and posting, checked against your CV.
          </p>
        }
      }
      @case ('NoCv') {
        @if (match(); as match) {
          <p class="text-caption text-ink-muted">This posting asks for</p>
          <ul class="flex flex-wrap gap-1.5" aria-label="Skills this posting asks for">
            @for (skill of match.missing; track skill.name) {
              <li><app-skill-chip kind="missing" [name]="skill.name" /></li>
            }
          </ul>
          <p class="text-small text-ink-2">
            <a appLink [routerLink]="cvPath">Upload your CV</a> to see which of these it shows.
          </p>
        }
      }
      @case ('NoPostingText') {
        <p class="text-small text-ink-2">
          Add the job description to see how your CV matches.
          <button
            type="button"
            class="cursor-pointer rounded-xs text-link underline underline-offset-2 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-focus"
            (click)="edit.emit()"
          >
            Edit application
          </button>
        </p>
      }
      @case ('NoSkillsInPosting') {
        <p class="text-small text-ink-2">No skills Heardback knows were found in this posting.</p>
      }
    }
  `,
})
export class MatchPanel {
  private readonly api = inject(CvApi);

  readonly applicationId = input.required<string>();
  /** The application's row version: a new one (after Edit or Add update) checks the match again. */
  readonly version = input.required<number>();

  /** The user asked to add posting text. */
  readonly edit = output();

  protected readonly cvPath = SHELL_ROUTES.cv;

  private readonly resource = rxResource({
    params: () => ({ id: this.applicationId(), version: this.version() }),
    stream: ({ params }) => this.api.match(params.id),
  });

  protected readonly match = computed(() =>
    this.resource.hasValue() ? this.resource.value() : null,
  );

  protected readonly view = computed(() => {
    if (this.resource.status() === 'error') {
      return 'error';
    }
    return this.match()?.state ?? 'loading';
  });

  protected readonly summary = computed(() => {
    const match = this.match();
    if (match?.state !== 'Ready' || match.score === null) {
      return null;
    }
    return matchSummary(
      match.matched.length,
      match.matched.length + match.missing.length,
      match.score,
    );
  });

  protected retry(): void {
    this.resource.reload();
  }
}
