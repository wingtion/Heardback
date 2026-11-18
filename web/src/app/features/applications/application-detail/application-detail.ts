import { A11yModule } from '@angular/cdk/a11y';
import { DatePipe } from '@angular/common';
import { HttpStatusCode } from '@angular/common/http';
import { Component, computed, inject, input, output } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { httpErrorOf } from '../../../core/http-errors';
import { Banner } from '../../../shared/ui/banner/banner';
import { PostageStamp } from '../../../shared/ui/brand/postage-stamp';
import { Postmark } from '../../../shared/ui/brand/postmark';
import { TimelineStamp } from '../../../shared/ui/brand/timeline-stamp';
import { Button } from '../../../shared/ui/button/button';
import { DialogLauncher } from '../../../shared/ui/dialog/dialog-launcher';
import { Icon } from '../../../shared/ui/icon/icon';
import { Spinner } from '../../../shared/ui/spinner/spinner';
import { StatusGlyphIcon } from '../../../shared/ui/status-label/status-glyph';
import { StatusLabel } from '../../../shared/ui/status-label/status-label';
import {
  APPLICATION_DATE_FORMAT,
  CHANNEL_LABELS,
  TIMELINE_DATE_FORMAT,
  daysAgo,
  statusDisplay,
} from '../application-display';
import { ApplicationsApi } from '../applications-api';
import { Application } from '../applications.models';
import {
  DeleteApplicationData,
  DeleteApplicationDialog,
} from '../delete-application-dialog/delete-application-dialog';
import { lastActivityOn, timelineRows } from './application-timeline';

/** "panel" beside the list from the xl breakpoint; "sheet" full screen below it (docs/design.md §9). */
export type DetailLayout = 'panel' | 'sheet';

const URL_SCHEME = /^https?:\/\//i;
const TRAILING_SLASH = /\/$/;

type DetailView =
  | { readonly kind: 'loading' }
  | { readonly kind: 'not-found' }
  | { readonly kind: 'error' }
  | { readonly kind: 'loaded'; readonly application: Application };

/**
 * One application as an envelope (docs/design.md §8): a header bar with Close, the envelope with stamp,
 * postmark, addressee, status, notes and timeline, and a bar with Delete at the bottom (decided
 * 2026-11-18; Edit and Add update arrive with their forms). The sheet layout is a modal dialog.
 */
@Component({
  selector: 'app-application-detail',
  imports: [
    A11yModule,
    DatePipe,
    Banner,
    Button,
    Icon,
    PostageStamp,
    Postmark,
    Spinner,
    StatusGlyphIcon,
    StatusLabel,
    TimelineStamp,
  ],
  templateUrl: './application-detail.html',
  host: {
    '[class]': 'hostClass()',
    '[attr.role]': 'sheet() ? "dialog" : null',
    '[attr.aria-modal]': 'sheet() ? "true" : null',
    '[attr.aria-labelledby]': 'sheet() ? titleId : null',
    '[attr.aria-label]': 'sheet() ? null : "Application details"',
    '(keydown.escape)': 'sheet() && closed.emit()',
  },
})
export class ApplicationDetail {
  private readonly api = inject(ApplicationsApi);
  private readonly dialogs = inject(DialogLauncher);

  readonly applicationId = input.required<string>();
  readonly layout = input<DetailLayout>('panel');

  readonly closed = output();
  readonly deleted = output();

  protected readonly titleId = 'application-detail-title';
  protected readonly dateFormat = APPLICATION_DATE_FORMAT;
  protected readonly timelineDateFormat = TIMELINE_DATE_FORMAT;
  protected readonly channelLabels = CHANNEL_LABELS;

  protected readonly sheet = computed(() => this.layout() === 'sheet');

  private readonly resource = rxResource({
    params: () => ({ id: this.applicationId() }),
    stream: ({ params }) => this.api.get(params.id),
  });

  protected readonly view = computed<DetailView>(() => {
    if (this.resource.status() === 'error') {
      const status = httpErrorOf(this.resource.error())?.status;
      return status === HttpStatusCode.NotFound ? { kind: 'not-found' } : { kind: 'error' };
    }
    return this.resource.hasValue() && !this.resource.isLoading()
      ? { kind: 'loaded', application: this.resource.value() }
      : { kind: 'loading' };
  });

  protected readonly application = computed(() => {
    const view = this.view();
    return view.kind === 'loaded' ? view.application : null;
  });

  protected readonly status = computed(() => {
    const application = this.application();
    return application ? statusDisplay(application.status, application.noResponseDays) : null;
  });

  protected readonly timeline = computed(() => {
    const application = this.application();
    return application ? timelineRows(application) : [];
  });

  protected readonly postmarkDate = computed(() => {
    const application = this.application();
    return application ? lastActivityOn(application) : null;
  });

  protected readonly updated = computed(() => {
    const application = this.application();
    return application ? daysAgo(application.updatedAt, new Date()) : '';
  });

  protected readonly hostClass = computed(() =>
    this.sheet()
      ? 'fixed inset-0 z-40 flex flex-col bg-surface'
      : 'flex h-full w-95 shrink-0 flex-col border-l border-rule bg-surface',
  );

  /** "northwind-logistics.example/jobs/4471": the link without its scheme, as the design shows it. */
  protected linkText(url: string): string {
    return url.replace(URL_SCHEME, '').replace(TRAILING_SLASH, '');
  }

  protected retry(): void {
    this.resource.reload();
  }

  protected confirmDelete(application: Application): void {
    const data: DeleteApplicationData = { id: application.id, company: application.company };
    const ref = this.dialogs.open(DeleteApplicationDialog, { role: 'alertdialog', data });
    ref.componentInstance?.deleted.subscribe(() => this.deleted.emit());
  }
}
