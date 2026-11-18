import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, inject, output } from '@angular/core';
import { Button } from '../../../shared/ui/button/button';
import { StatusGlyphIcon } from '../../../shared/ui/status-label/status-glyph';
import { STATUS_DISPLAY } from '../application-display';
import { APPLICATION_STATUSES, ApplicationStatus } from '../applications.models';
import { STATUS_GLYPH_COLOURS } from './board-display';

export interface MoveToSheetData {
  readonly company: string;
  readonly role: string;
  readonly status: ApplicationStatus;
}

/**
 * "Move to" on phones and tablets (docs/design.md §14): a bottom sheet with the three other statuses as
 * 52 px rows and a full-width Cancel. Open it with DialogLauncher (bottom sheet).
 */
@Component({
  selector: 'app-move-to-sheet',
  imports: [Button, StatusGlyphIcon],
  host: {
    class: 'flex flex-col gap-3 rounded-t-sheet border-t border-rule-strong bg-base px-4 pt-5 pb-6',
  },
  template: `
    <div>
      <h2 [id]="titleId" class="font-serif text-dialog-title font-semibold text-ink">Move to</h2>
      <p class="text-cell text-ink-2">
        {{ data.company }} · {{ data.role }} · now in {{ statusLabels[data.status].label }}
      </p>
    </div>
    <div class="flex flex-col overflow-hidden rounded-md border border-rule">
      @for (status of targets; track status) {
        <button
          type="button"
          class="flex h-13 cursor-pointer items-center gap-2.5 border-b border-rule px-3.5 text-left text-ui-lg text-ink last:border-b-0 hover:bg-row-hover focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-focus"
          (click)="choose(status)"
        >
          <app-status-glyph
            [glyph]="statusLabels[status].glyph"
            [size]="16"
            [class]="glyphColours[status]"
          />
          {{ statusLabels[status].label }}
        </button>
      }
    </div>
    <p class="text-label text-ink-muted">Adds a timeline event dated today.</p>
    <button appButton size="xl" type="button" class="w-full" (click)="close()">Cancel</button>
  `,
})
export class MoveToSheet {
  private readonly dialogRef = inject(DialogRef);

  protected readonly data = inject<MoveToSheetData>(DIALOG_DATA);
  protected readonly titleId = this.dialogRef.config.ariaLabelledBy ?? undefined;
  protected readonly statusLabels = STATUS_DISPLAY;
  protected readonly glyphColours = STATUS_GLYPH_COLOURS;
  protected readonly targets = APPLICATION_STATUSES.filter((status) => status !== this.data.status);

  /** The status the user picked; the sheet closes itself. */
  readonly chosen = output<ApplicationStatus>();

  protected choose(status: ApplicationStatus): void {
    this.chosen.emit(status);
    this.dialogRef.close();
  }

  protected close(): void {
    this.dialogRef.close();
  }
}
