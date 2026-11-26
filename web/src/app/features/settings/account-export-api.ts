import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/api-base-url';
import { ACCOUNT_API } from '../auth/auth-paths';

/** Downloads per hour the API allows (AccountRateLimiting.ExportPolicy). */
export const EXPORTS_PER_HOUR = 5;

/** "heardback-export-2026-11-25.json", dated in the user's own day, like the server's file name. */
export function exportFileName(today: Date): string {
  const year = today.getFullYear();
  const month = String(today.getMonth() + 1).padStart(2, '0');
  const day = String(today.getDate()).padStart(2, '0');
  return `heardback-export-${year}-${month}-${day}.json`;
}

/** The data export (ADR-0015): one JSON file with everything the user can see about themselves. */
@Injectable({ providedIn: 'root' })
export class AccountExportApi {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_BASE_URL)}${ACCOUNT_API.export}`;

  /** As a file, not parsed: it is saved exactly as the server wrote it. */
  download(): Observable<Blob> {
    return this.http.get(this.url, { responseType: 'blob' });
  }
}
