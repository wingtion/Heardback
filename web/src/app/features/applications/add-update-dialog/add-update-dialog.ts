import { A11yModule } from '@angular/cdk/a11y';
import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { formatDate } from '@angular/common';
import { HttpStatusCode } from '@angular/common/http';
import {
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  LOCALE_ID,
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
import { httpErrorOf, validationErrors } from '../../../core/http-errors';
import { Banner } from '../../../shared/ui/banner/banner';
import { Button } from '../../../shared/ui/button/button';
import { AppDialog } from '../../../shared/ui/dialog/dialog';
import { DialogFooter } from '../../../shared/ui/dialog/dialog-footer';
import { Field } from '../../../shared/ui/field/field';
import { Input } from '../../../shared/ui/field/input';
import { Spinner } from '../../../shared/ui/spinner/spinner';
import { StatusGlyph, StatusGlyphIcon } from '../../../shared/ui/status-label/status-glyph';
import { APPLICATION_DATE_FORMAT, STATUS_DISPLAY, localToday } from '../application-display';
import { ApplicationsApi } from '../applications-api';
import {
  APPLICATION_LIMITS,
  APPLICATION_STATUSES,
  Application,
  ApplicationStatus,
} from '../applications.models';
import { STATUS_GLYPH_COLOURS } from '../board/board-display';

/** Copy from docs/design.md §14. */
export const ADD_UPDATE_MESSAGES = {
  nothingChosen: 'Choose a new status or write a note.',
  dateInFuture: 'The date can’t be in the future. Pick today or earlier.',
} as const;

/** "Pick a date on or after the applied date (03 Nov 2026)" (decided 2026-11-18). */
export function beforeAppliedMessage(appliedOn: string, locale: string): string {
  return `Pick a date on or after the applied date (${formatDate(appliedOn, APPLICATION_DATE_FORMAT, locale)})`;
}

export interface AddUpdateData {
  readonly application: Application;
}

/** The radio value for "No change": a note only. */
const NO_CHANGE = 'none';
type StatusChoice = ApplicationStatus | typeof NO_CHANGE;

interface StatusChip {
  readonly value: StatusChoice;
  readonly label: string;
  readonly glyph: StatusGlyph;
  readonly glyphClass: string;
  readonly current: boolean;
}

type FieldName = 'occurredOn' | 'note';

const CHIP =
  'inline-flex h-11 cursor-pointer items-center gap-1.5 rounded-md bg-base pr-2.5 pl-2 text-cell ' +
  'focus-within:outline-2 focus-within:outline-offset-2 focus-within:outline-focus lg:h-8.5';
const CHIP_OFF = 'border border-field-border text-ink';
const CHIP_ON = 'border-thick border-ink bg-row-selected font-semibold text-ink';
const CHIP_CURRENT = 'cursor-default border border-field-border text-ink-muted';

/**
 * Add update (docs/design.md §14): an optional new status as radio chips (the current one disabled), a date
 * that is today or earlier and not before the applied date, and an optional note of up to 280 characters.
 * Either a status or a note is needed. A 410 px dialog from the lg breakpoint, a bottom sheet below it.
 */
@Component({
  selector: 'app-add-update-dialog',
  imports: [
    A11yModule,
    ReactiveFormsModule,
    AppDialog,
    Banner,
    Button,
    DialogFooter,
    Field,
    Input,
    Spinner,
    StatusGlyphIcon,
  ],
  templateUrl: './add-update-dialog.html',
})
export class AddUpdateDialog {
  private readonly api = inject(ApplicationsApi);
  private readonly dialogRef = inject(DialogRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly locale = inject(LOCALE_ID);

  protected readonly application = inject<AddUpdateData>(DIALOG_DATA).application;

  /** The application with its new timeline, once saved. */
  readonly saved = output<Application>();

  protected readonly messages = ADD_UPDATE_MESSAGES;
  protected readonly noteMaxLength = APPLICATION_LIMITS.eventNote;
  protected readonly today = localToday(new Date());
  protected readonly chips: readonly StatusChip[] = [
    {
      value: NO_CHANGE,
      label: 'No change',
      glyph: 'none',
      glyphClass: 'text-ink-muted',
      current: false,
    },
    ...APPLICATION_STATUSES.map((status) => ({
      value: status,
      label:
        status === this.application.status
          ? `${STATUS_DISPLAY[status].label} · current`
          : STATUS_DISPLAY[status].label,
      glyph: STATUS_DISPLAY[status].glyph,
      glyphClass: STATUS_GLYPH_COLOURS[status],
      current: status === this.application.status,
    })),
  ];

  protected readonly form = inject(NonNullableFormBuilder).group({
    status: [NO_CHANGE as StatusChoice],
    occurredOn: [this.today, (control: AbstractControl<string>) => this.validateDate(control)],
    note: [''],
  });

  protected readonly submitting = signal(false);
  protected readonly unavailable = signal(false);
  protected readonly nothingChosen = signal(false);
  /** A server message about the status, e.g. it changed to this one elsewhere meanwhile. */
  protected readonly statusError = signal<string | null>(null);

  private readonly submitted = signal(false);
  private readonly serverErrors = signal<Partial<Record<FieldName, string>>>({});
  private readonly formValue = toSignal(this.form.valueChanges, {
    initialValue: this.form.getRawValue(),
  });

  protected readonly noteCount = computed(
    () => `${this.formValue().note?.length ?? 0} / ${this.noteMaxLength}`,
  );

  protected readonly errors = computed<Partial<Record<FieldName, string>>>(() => {
    this.formValue();
    const server = this.serverErrors();
    if (!this.submitted()) {
      return server;
    }
    const date = this.form.controls.occurredOn;
    return {
      occurredOn: date.hasError('future')
        ? ADD_UPDATE_MESSAGES.dateInFuture
        : date.hasError('beforeApplied')
          ? beforeAppliedMessage(this.application.appliedOn, this.locale)
          : undefined,
      ...server,
    };
  });

  constructor() {
    // A message belongs to what the user had entered; changing the form clears it.
    this.form.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.serverErrors.set({});
      this.nothingChosen.set(false);
      this.statusError.set(null);
    });
  }

  protected chipClass(chip: StatusChip): string {
    if (chip.current) {
      return `${CHIP} ${CHIP_CURRENT}`;
    }
    return `${CHIP} ${this.formValue().status === chip.value ? CHIP_ON : CHIP_OFF}`;
  }

  protected submit(): void {
    this.submitted.set(true);
    this.serverErrors.set({});
    if (this.submitting()) {
      return;
    }
    if (this.form.invalid) {
      this.focusFirstError();
      return;
    }

    const value = this.form.getRawValue();
    const status = value.status === NO_CHANGE ? null : value.status;
    const note = value.note.trim();
    if (status === null && note === '') {
      this.nothingChosen.set(true);
      return;
    }

    this.setBusy(true);
    this.api
      .addEvent(this.application.id, {
        status,
        occurredOn: value.occurredOn,
        note: note === '' ? null : note,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (application) => {
          this.setBusy(false);
          this.saved.emit(application);
          this.dialogRef.close();
        },
        error: (error: unknown) => {
          this.setBusy(false);
          this.handleError(error);
        },
      });
  }

  protected close(): void {
    this.dialogRef.close();
  }

  private validateDate(control: AbstractControl<string>): ValidationErrors | null {
    const value = control.value;
    if (value === '' || value > localToday(new Date())) {
      return { future: true };
    }
    return value < this.application.appliedOn ? { beforeApplied: true } : null;
  }

  private handleError(error: unknown): void {
    const response = httpErrorOf(error);
    const fields = validationErrors(response?.error);
    if (response?.status !== HttpStatusCode.BadRequest || Object.keys(fields).length === 0) {
      this.unavailable.set(true);
      return;
    }
    const { status, occurredOn, note } = fields;
    this.statusError.set(status ?? null);
    this.serverErrors.set({ occurredOn, note });
    this.focusFirstError();
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
