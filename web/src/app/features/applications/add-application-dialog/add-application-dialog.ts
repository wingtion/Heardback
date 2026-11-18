import { A11yModule } from '@angular/cdk/a11y';
import { DialogRef } from '@angular/cdk/dialog';
import { DatePipe } from '@angular/common';
import { HttpStatusCode } from '@angular/common/http';
import {
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  inject,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
} from '@angular/forms';
import { catchError, debounceTime, distinctUntilChanged, map, of, switchMap } from 'rxjs';
import { httpErrorOf, validationErrors } from '../../../core/http-errors';
import { Banner } from '../../../shared/ui/banner/banner';
import { Button } from '../../../shared/ui/button/button';
import { AppDialog } from '../../../shared/ui/dialog/dialog';
import { DialogFooter } from '../../../shared/ui/dialog/dialog-footer';
import { Field } from '../../../shared/ui/field/field';
import { Input } from '../../../shared/ui/field/input';
import { Select } from '../../../shared/ui/field/select';
import { Icon } from '../../../shared/ui/icon/icon';
import { Spinner } from '../../../shared/ui/spinner/spinner';
import { StatusLabel } from '../../../shared/ui/status-label/status-label';
import {
  APPLICATION_DATE_FORMAT,
  CHANNELS,
  CHANNEL_LABELS,
  localToday,
  statusDisplay,
} from '../application-display';
import { ApplicationsApi } from '../applications-api';
import {
  APPLICATION_ERROR_CODES,
  APPLICATION_LIMITS,
  Application,
  ApplicationChannel,
  EARLIEST_DATE,
  TrackedApplication,
} from '../applications.models';

/** Field copy from docs/design.md §14. */
export const ADD_APPLICATION_MESSAGES = {
  url: 'Enter a full link that starts with https://',
  company: 'Enter the company name',
  role: 'Enter the role',
  dateInFuture: 'Pick today or an earlier date',
  dateTooEarly: 'Pick a date after 1 January 2000',
} as const;

const DEFAULT_CHANNEL: ApplicationChannel = 'LinkedInEasyApply';
const TRACKED_LOOKUP_DELAY_MS = 400;
const HTTP_SCHEMES = new Set(['http:', 'https:']);
const HOST_LABEL_SEPARATOR = '.';
const ERROR_CODE = 'code';

type FieldName = 'url' | 'company' | 'role' | 'appliedOn' | 'postingText' | 'channel';

/** A link the server would accept: absolute http(s) with a dotted host. The server has the last word. */
export function isPostingLink(value: string): boolean {
  try {
    const url = new URL(value.trim());
    return HTTP_SCHEMES.has(url.protocol) && url.hostname.includes(HOST_LABEL_SEPARATOR);
  } catch {
    return false;
  }
}

function linkValidator(control: AbstractControl<string>): ValidationErrors | null {
  const value = control.value.trim();
  return value === '' || isPostingLink(value) ? null : { link: true };
}

function requiredText(control: AbstractControl<string>): ValidationErrors | null {
  return control.value.trim() === '' ? { required: true } : null;
}

/** Today or earlier on this device, and not before 2000. */
function appliedOnValidator(control: AbstractControl<string>): ValidationErrors | null {
  const value = control.value;
  if (value === '' || value > localToday(new Date())) {
    return { future: true };
  }
  return value < EARLIEST_DATE ? { tooEarly: true } : null;
}

/**
 * Add application (docs/design.md §14): the link first, with Paste, then company, role, channel, applied
 * date and posting text. When the link belongs to a posting the user already tracks, the form gives way
 * to "You already track this posting" with View application. There is no autofill from the link: the
 * server never fetches posting pages. A dialog from the lg breakpoint, a full-screen sheet below it.
 */
@Component({
  selector: 'app-add-application-dialog',
  imports: [
    A11yModule,
    DatePipe,
    ReactiveFormsModule,
    AppDialog,
    Banner,
    Button,
    DialogFooter,
    Field,
    Icon,
    Input,
    Select,
    Spinner,
    StatusLabel,
  ],
  templateUrl: './add-application-dialog.html',
  host: { class: 'flex h-full flex-col' },
})
export class AddApplicationDialog {
  private readonly api = inject(ApplicationsApi);
  private readonly dialogRef = inject(DialogRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  /** The new application, once saved. */
  readonly created = output<Application>();
  /** The user chose to open the application that already tracks the posting. */
  readonly viewTracked = output<string>();

  protected readonly limits = APPLICATION_LIMITS;
  protected readonly channels = CHANNELS;
  protected readonly channelLabels = CHANNEL_LABELS;
  protected readonly dateFormat = APPLICATION_DATE_FORMAT;
  protected readonly today = localToday(new Date());
  protected readonly earliestDate = EARLIEST_DATE;
  protected readonly canPaste = typeof navigator.clipboard?.readText === 'function';

  protected readonly form = inject(NonNullableFormBuilder).group({
    url: ['', linkValidator],
    company: ['', requiredText],
    role: ['', requiredText],
    channel: [DEFAULT_CHANNEL],
    appliedOn: [this.today, appliedOnValidator],
    postingText: [''],
  });

  protected readonly submitting = signal(false);
  protected readonly unavailable = signal(false);
  /** The submission matched the user's own application by its posting text (no link to look it up by). */
  protected readonly duplicateText = signal(false);
  protected readonly tracked = signal<TrackedApplication | null>(null);

  private readonly submitted = signal(false);
  private readonly serverErrors = signal<Partial<Record<FieldName, string>>>({});
  private readonly formValue = toSignal(this.form.valueChanges, { initialValue: this.form.value });

  protected readonly trackedStatus = computed(() => {
    const tracked = this.tracked();
    return tracked ? statusDisplay(tracked.status, tracked.noResponseDays) : null;
  });

  protected readonly errors = computed<Partial<Record<FieldName, string>>>(() => {
    this.formValue();
    const server = this.serverErrors();
    if (!this.submitted()) {
      return server;
    }
    const controls = this.form.controls;
    return {
      url: controls.url.hasError('link') ? ADD_APPLICATION_MESSAGES.url : undefined,
      company: controls.company.invalid ? ADD_APPLICATION_MESSAGES.company : undefined,
      role: controls.role.invalid ? ADD_APPLICATION_MESSAGES.role : undefined,
      appliedOn: controls.appliedOn.hasError('future')
        ? ADD_APPLICATION_MESSAGES.dateInFuture
        : controls.appliedOn.hasError('tooEarly')
          ? ADD_APPLICATION_MESSAGES.dateTooEarly
          : undefined,
      ...server,
    };
  });

  constructor() {
    this.form.controls.url.valueChanges
      .pipe(
        map((value) => value.trim()),
        debounceTime(TRACKED_LOOKUP_DELAY_MS),
        distinctUntilChanged(),
        switchMap((url) =>
          isPostingLink(url)
            ? this.api.findTracked(url).pipe(catchError(() => of(null)))
            : of(null),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((tracked) => this.tracked.set(tracked));

    // A server message belongs to the value it was about; editing the field clears it.
    this.form.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      if (Object.keys(this.serverErrors()).length > 0) {
        this.serverErrors.set({});
      }
      this.duplicateText.set(false);
    });
  }

  /** Fills the link from the clipboard; the browser may ask for permission or refuse. */
  protected async paste(): Promise<void> {
    try {
      const text = await navigator.clipboard.readText();
      this.form.controls.url.setValue(text.trim());
    } catch {
      // Refused or empty: the user can still paste into the field by hand.
    }
  }

  protected submit(): void {
    this.submitted.set(true);
    this.serverErrors.set({});
    if (this.tracked() || this.submitting()) {
      return;
    }
    if (this.form.invalid) {
      this.focusFirstError();
      return;
    }

    const value = this.form.getRawValue();
    this.setBusy(true);
    this.api
      .create({
        company: value.company.trim(),
        role: value.role.trim(),
        url: value.url.trim() || null,
        postingText: value.postingText.trim() || null,
        channel: value.channel,
        appliedOn: value.appliedOn,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (application) => {
          this.setBusy(false);
          this.created.emit(application);
          this.dialogRef.close();
        },
        error: (error: unknown) => {
          this.setBusy(false);
          this.handleError(error, value.url.trim());
        },
      });
  }

  protected openTracked(tracked: TrackedApplication): void {
    this.viewTracked.emit(tracked.id);
    this.dialogRef.close();
  }

  protected close(): void {
    this.dialogRef.close();
  }

  private handleError(error: unknown, url: string): void {
    const response = httpErrorOf(error);
    const body: unknown = response?.error;

    if (
      response?.status === HttpStatusCode.Conflict &&
      this.errorCode(body) === APPLICATION_ERROR_CODES.alreadyTracked
    ) {
      this.showAlreadyTracked(url);
      return;
    }

    const fields = validationErrors(body);
    if (response?.status === HttpStatusCode.BadRequest && Object.keys(fields).length > 0) {
      this.serverErrors.set(fields);
      this.focusFirstError();
      return;
    }

    this.unavailable.set(true);
  }

  // The link may have matched through a form the instant lookup did not see; ask once more by link.
  private showAlreadyTracked(url: string): void {
    if (!isPostingLink(url)) {
      this.duplicateText.set(true);
      return;
    }
    this.api
      .findTracked(url)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (tracked) => (tracked ? this.tracked.set(tracked) : this.duplicateText.set(true)),
        error: () => this.duplicateText.set(true),
      });
  }

  private errorCode(body: unknown): unknown {
    return typeof body === 'object' && body !== null && ERROR_CODE in body
      ? body[ERROR_CODE]
      : null;
  }

  private setBusy(busy: boolean): void {
    this.submitting.set(busy);
    this.dialogRef.disableClose = busy;
    if (busy) {
      this.unavailable.set(false);
      this.form.disable({ emitEvent: false });
    } else {
      this.form.enable({ emitEvent: false });
    }
  }

  private focusFirstError(): void {
    afterNextRender(
      () => this.host.nativeElement.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      { injector: this.injector },
    );
  }
}
