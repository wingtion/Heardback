import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/api-base-url';
import { APPLICATIONS_API } from '../applications/applications-api';
import { CV_API } from '../cv/cv-api';
import { AiConsent, PostingAnalysis, Tailoring } from './ai.models';

/** HTTP calls for the AI features. The server only ever reads the signed-in user's data. */
@Injectable({ providedIn: 'root' })
export class AiApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  postingAnalysis(applicationId: string): Observable<PostingAnalysis> {
    return this.http.get<PostingAnalysis>(this.applicationUrl(applicationId, 'posting-analysis'));
  }

  /** Queues an analysis; text someone already had analysed comes back ready. */
  requestPostingAnalysis(applicationId: string): Observable<PostingAnalysis> {
    return this.http.post<PostingAnalysis>(
      this.applicationUrl(applicationId, 'posting-analysis'),
      null,
    );
  }

  tailoring(applicationId: string): Observable<Tailoring> {
    return this.http.get<Tailoring>(this.applicationUrl(applicationId, 'tailoring'));
  }

  /** Queues suggestions; current ones come back as they are. */
  requestTailoring(applicationId: string): Observable<Tailoring> {
    return this.http.post<Tailoring>(this.applicationUrl(applicationId, 'tailoring'), null);
  }

  consent(): Observable<AiConsent> {
    return this.http.get<AiConsent>(`${this.baseUrl}${CV_API}/ai-consent`);
  }

  /** Withdrawing deletes every suggestion made from the CV. */
  setConsent(allowed: boolean): Observable<AiConsent> {
    return this.http.put<AiConsent>(`${this.baseUrl}${CV_API}/ai-consent`, { allowed });
  }

  private applicationUrl(applicationId: string, resource: string): string {
    return `${this.baseUrl}${APPLICATIONS_API}/${encodeURIComponent(applicationId)}/${resource}`;
  }
}
