import { Injectable, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { ApplicationsApi } from './applications-api';
import { ApplicationSummary } from './applications.models';

/**
 * The user's application counts, shared by the page heading ("8 applications · 2 with no response") and
 * the sidebar count. Provided by the shell, so a new sign-in never sees the previous user's numbers.
 */
@Injectable()
export class ApplicationsSummaryStore {
  private readonly api = inject(ApplicationsApi);

  private readonly resource = rxResource({ stream: () => this.api.summary() });

  /** Null until loaded, and while a reload after an error is pending. */
  readonly summary = computed<ApplicationSummary | null>(() =>
    this.resource.hasValue() ? this.resource.value() : null,
  );

  /** After adding or deleting an application. */
  reload(): void {
    this.resource.reload();
  }
}
