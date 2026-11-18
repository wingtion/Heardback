import { A11yModule } from '@angular/cdk/a11y';
import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
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
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { httpErrorOf, problemCode, validationErrors } from '../../../core/http-errors';
import { Banner } from '../../../shared/ui/banner/banner';
import { Button } from '../../../shared/ui/button/button';
import { AppDialog } from '../../../shared/ui/dialog/dialog';
import { DialogFooter } from '../../../shared/ui/dialog/dialog-footer';
import { Field } from '../../../shared/ui/field/field';
import { Input } from '../../../shared/ui/field/input';
import { Select } from '../../../shared/ui/field/select';
import { Spinner } from '../../../shared/ui/spinner/spinner';
import { CHANNELS, CHANNEL_LABELS, localToday } from '../application-display';
import {
  APPLICATION_FORM_MESSAGES,
  ApplicationFormField,
  appliedOnError,
  appliedOnValidator,
  linkValidator,
  requiredText,
} from '../application-form';
import { ApplicationsApi } from '../applications-api';
import {
  APPLICATION_ERROR_CODES,
  APPLICATION_LIMITS,
  Application,
  EARLIEST_DATE,
} from '../applications.models';

export interface EditApplicationData {
  readonly application: Application;
}

/** What went wrong with the last save, shown as a banner above the fields. */
type SaveProblem = 'unavailable' | 'changed-elsewhere' | 'already-tracked' | 'not-found';

/**
 * Edit application (docs/design.md §14): link, company, role, channel, applied date, posting text and
 * the user's own notes. Status is not here; it changes through the board or Add update. The save sends
 * the version the form was opened with: if the application changed elsewhere meanwhile, Save is disabled
 * until the user reloads the latest version. A 420 px dialog from the lg breakpoint, a full-screen sheet
 * below it.
 */
@Component({
  selector: 'app-edit-application-dialog',
  imports: [
    A11yModule,
    ReactiveFormsModule,
    AppDialog,
    Banner,
    Button,
    DialogFooter,
    Field,
    Input,
    Select,
    Spinner,
  ],
  templateUrl: './edit-application-dialog.html',
  host: { class: 'flex h-full flex-col' },
})
export class EditApplicationDialog {
  private readonly api = inject(ApplicationsApi);
  private readonly dialogRef = inject(DialogRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  /** The application as saved. */
  readonly saved = output<Application>();
  /** The latest version, loaded after "changed elsewhere": the detail shows it too. */
  readonly reloaded = output<Application>();

  protected readonly limits = APPLICATION_LIMITS;
  protected readonly channels = CHANNELS;
  protected readonly channelLabels = CHANNEL_LABELS;
  protected readonly today = localToday(new Date());
  protected readonly earliestDate = EARLIEST_DATE;

  private readonly application = signal(inject<EditApplicationData>(DIALOG_DATA).application);

  protected readonly form = inject(NonNullableFormBuilder).group({
    url: ['', linkValidator],
    company: ['', requiredText],
    role: ['', requiredText],
    channel: [this.application().channel],
    appliedOn: ['', appliedOnValidator],
    postingText: [''],
    notes: [''],
  });

  protected readonly submitting = signal(false);
  protected readonly reloading = signal(false);
  protected readonly problem = signal<SaveProblem | null>(null);

  private readonly submitted = signal(false);
  private readonly serverErrors = signal<Partial<Record<ApplicationFormField, string>>>({});
  private readonly formValue = toSignal(this.form.valueChanges, { initialValue: this.form.value });

  protected readonly busy = computed(() => this.submitting() || this.reloading());

  protected readonly errors = computed<Partial<Record<ApplicationFormField, string>>>(() => {
    this.formValue();
    const server = this.serverErrors();
    if (!this.submitted()) {
      return server;
    }
    const controls = this.form.controls;
    return {
      url: controls.url.hasError('link') ? APPLICATION_FORM_MESSAGES.url : undefined,
      company: controls.company.invalid ? APPLICATION_FORM_MESSAGES.company : undefined,
      role: controls.role.invalid ? APPLICATION_FORM_MESSAGES.role : undefined,
      appliedOn: appliedOnError(controls.appliedOn),
      ...server,
    };
  });

  constructor() {
    this.fill(this.application());

    // A server message belongs to the value it was about; editing the field clears it.
    this.form.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      if (Object.keys(this.serverErrors()).length > 0) {
        this.serverErrors.set({});
      }
      if (this.problem() === 'already-tracked') {
        this.problem.set(null);
      }
    });
  }

  protected submit(): void {
    this.submitted.set(true);
    this.serverErrors.set({});
    if (this.busy() || this.problem() === 'changed-elsewhere') {
      return;
    }
    if (this.form.invalid) {
      this.focusFirstError();
      return;
    }

    const value = this.form.getRawValue();
    this.setBusy(this.submitting, true);
    this.api
      .update(this.application().id, {
        company: value.company.trim(),
        role: value.role.trim(),
        url: value.url.trim() || null,
        postingText: value.postingText.trim() || null,
        channel: value.channel,
        appliedOn: value.appliedOn,
        notes: value.notes.trim() || null,
        version: this.application().version,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (application) => {
          this.setBusy(this.submitting, false);
          this.saved.emit(application);
          this.dialogRef.close();
        },
        error: (error: unknown) => {
          this.setBusy(this.submitting, false);
          this.handleError(error);
        },
      });
  }

  /** "Reload latest version": the edits here are dropped, as the banner says. */
  protected reload(): void {
    this.setBusy(this.reloading, true);
    this.api
      .get(this.application().id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (application) => {
          this.setBusy(this.reloading, false);
          this.application.set(application);
          this.fill(application);
          this.submitted.set(false);
          this.problem.set(null);
          this.reloaded.emit(application);
        },
        error: (error: unknown) => {
          this.setBusy(this.reloading, false);
          if (httpErrorOf(error)?.status === HttpStatusCode.NotFound) {
            this.problem.set('not-found');
          }
        },
      });
  }

  protected close(): void {
    this.dialogRef.close();
  }

  private fill(application: Application): void {
    this.form.reset({
      url: application.url ?? '',
      company: application.company,
      role: application.role,
      channel: application.channel,
      appliedOn: application.appliedOn,
      postingText: application.postingText ?? '',
      notes: application.notes ?? '',
    });
  }

  private handleError(error: unknown): void {
    const response = httpErrorOf(error);
    const code = problemCode(response?.error);

    if (code === APPLICATION_ERROR_CODES.changedElsewhere) {
      this.problem.set('changed-elsewhere');
      return;
    }
    if (code === APPLICATION_ERROR_CODES.alreadyTracked) {
      this.problem.set('already-tracked');
      return;
    }
    if (response?.status === HttpStatusCode.NotFound) {
      this.problem.set('not-found');
      return;
    }

    const fields = validationErrors(response?.error);
    if (response?.status === HttpStatusCode.BadRequest && Object.keys(fields).length > 0) {
      this.serverErrors.set(fields);
      this.focusFirstError();
      return;
    }

    this.problem.set('unavailable');
  }

  private setBusy(flag: typeof this.submitting, busy: boolean): void {
    flag.set(busy);
    this.dialogRef.disableClose = busy;
    if (busy) {
      if (this.problem() !== 'changed-elsewhere') {
        this.problem.set(null);
      }
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
