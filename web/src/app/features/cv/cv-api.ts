import { HttpClient, HttpStatusCode } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from '../../core/api-base-url';
import { APPLICATIONS_API } from '../applications/applications-api';
import { ApplicationMatch, Cv } from './cv.models';

export const CV_API = '/api/cv';

const FILE_FIELD = 'file';

/** HTTP calls for the CV page and the match panel. Only the signed-in user's CV is ever read. */
@Injectable({ providedIn: 'root' })
export class CvApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  /** The stored CV, or null when the user has not uploaded one. */
  get(): Observable<Cv | null> {
    return this.http
      .get<Cv>(`${this.baseUrl}${CV_API}`, { observe: 'response' })
      .pipe(
        map((response) => (response.status === HttpStatusCode.NoContent ? null : response.body)),
      );
  }

  /** Uploads a PDF; it replaces any earlier CV. */
  upload(file: File): Observable<Cv> {
    const form = new FormData();
    form.append(FILE_FIELD, file, file.name);
    return this.http.post<Cv>(`${this.baseUrl}${CV_API}`, form);
  }

  delete(): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}${CV_API}`);
  }

  /** Matched and missing skills between one application's posting and the CV. */
  match(applicationId: string): Observable<ApplicationMatch> {
    return this.http.get<ApplicationMatch>(
      `${this.baseUrl}${APPLICATIONS_API}/${encodeURIComponent(applicationId)}/match`,
    );
  }
}
