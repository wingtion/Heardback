import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';

/** Hands a file the app made in memory to the browser's download, e.g. the data export. */
@Injectable({ providedIn: 'root' })
export class FileSaver {
  private readonly document = inject(DOCUMENT);

  save(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const link = this.document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.hidden = true;
    this.document.body.append(link);
    link.click();
    link.remove();
    // The download has its own reference once it starts; the object URL is only needed for the click.
    setTimeout(() => URL.revokeObjectURL(url));
  }
}
