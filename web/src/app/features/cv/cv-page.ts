import { DatePipe } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Banner } from '../../shared/ui/banner/banner';
import { EnvelopeIllustration } from '../../shared/ui/brand/envelope-illustration';
import { Button } from '../../shared/ui/button/button';
import { DialogLauncher } from '../../shared/ui/dialog/dialog-launcher';
import { Icon } from '../../shared/ui/icon/icon';
import { InlineSuccess } from '../../shared/ui/inline-success/inline-success';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { APPLICATION_DATE_FORMAT } from '../applications/application-display';
import { CvApi } from './cv-api';
import { UPLOAD_LIMITS_TEXT, UploadProblem, checkFile, uploadProblem } from './cv-upload';
import { Cv, PDF_EXTENSION, PDF_TYPE } from './cv.models';
import { DeleteCvDialog } from './delete-cv-dialog';
import { SkillChip } from './skill-chip';
import { storedTextParts } from './stored-text';

type PageView =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly cv: Cv | null };

/**
 * The CV page: upload one PDF (button, or drop it on desktop), see the skills found and the text exactly as
 * stored, replace or delete it. Only the text is kept, with personal details replaced (ADR-0006). Built from
 * the existing design system (decided by the author on 2026-11-18, docs/design.md §15).
 */
@Component({
  selector: 'app-cv-page',
  imports: [
    DatePipe,
    Banner,
    Button,
    EnvelopeIllustration,
    Icon,
    InlineSuccess,
    SkillChip,
    Spinner,
  ],
  templateUrl: './cv-page.html',
  host: { class: 'flex flex-col gap-4 p-4 lg:max-w-200 lg:gap-4.5 lg:px-7 lg:pt-6 lg:pb-5' },
})
export class CvPage {
  private readonly api = inject(CvApi);
  private readonly dialogs = inject(DialogLauncher);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly dateFormat = APPLICATION_DATE_FORMAT;
  protected readonly limitsText = UPLOAD_LIMITS_TEXT;
  protected readonly accept = `${PDF_TYPE},${PDF_EXTENSION}`;

  private readonly resource = rxResource({ stream: () => this.api.get() });

  protected readonly view = computed<PageView>(() => {
    if (this.resource.status() === 'error') {
      return { kind: 'error' };
    }
    return this.resource.hasValue()
      ? { kind: 'ready', cv: this.resource.value() }
      : { kind: 'loading' };
  });

  protected readonly cv = computed(() => {
    const view = this.view();
    return view.kind === 'ready' ? view.cv : null;
  });

  protected readonly storedText = computed(() => {
    const cv = this.cv();
    return cv ? storedTextParts(cv.text) : [];
  });

  protected readonly uploading = signal(false);
  protected readonly problem = signal<UploadProblem | null>(null);
  protected readonly uploaded = signal(false);
  protected readonly showText = signal(false);
  /** A file is being dragged over the drop area. */
  protected readonly dropping = signal(false);

  protected retry(): void {
    this.resource.reload();
  }

  protected onFileChosen(input: HTMLInputElement): void {
    const file = input.files?.item(0);
    // Cleared so choosing the same file again (after fixing it) still fires a change.
    input.value = '';
    if (file) {
      this.upload(file);
    }
  }

  protected onDragOver(event: DragEvent): void {
    if (this.uploading()) {
      return;
    }
    event.preventDefault();
    this.dropping.set(true);
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dropping.set(false);
    const file = event.dataTransfer?.files.item(0);
    if (file && !this.uploading()) {
      this.upload(file);
    }
  }

  protected toggleText(): void {
    this.showText.update((shown) => !shown);
  }

  protected confirmDelete(): void {
    const ref = this.dialogs.open(DeleteCvDialog, { role: 'alertdialog' });
    ref.componentInstance?.deleted.subscribe(() => {
      this.resource.set(null);
      this.uploaded.set(false);
      this.showText.set(false);
    });
  }

  private upload(file: File): void {
    this.uploaded.set(false);
    const early = checkFile(file);
    if (early) {
      this.problem.set(early);
      return;
    }

    this.problem.set(null);
    this.uploading.set(true);
    this.api
      .upload(file)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (cv) => {
          this.uploading.set(false);
          this.resource.set(cv);
          this.uploaded.set(true);
        },
        error: (error: unknown) => {
          this.uploading.set(false);
          this.problem.set(uploadProblem(error));
        },
      });
  }
}
