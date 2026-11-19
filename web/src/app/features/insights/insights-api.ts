import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/api-base-url';
import { Dashboard, FOLLOW_UPS_SHOWN, FollowUpPage } from './insights.models';

export const INSIGHTS_API = '/api/insights';

/** HTTP calls for the Insights page. Every number comes from the signed-in user's own applications. */
@Injectable({ providedIn: 'root' })
export class InsightsApi {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_BASE_URL)}${INSIGHTS_API}`;

  dashboard(): Observable<Dashboard> {
    return this.http.get<Dashboard>(`${this.url}/dashboard`);
  }

  /** The longest-silent applications first; the page shows only the first few. */
  followUps(): Observable<FollowUpPage> {
    const params = new HttpParams().set('page', 1).set('pageSize', FOLLOW_UPS_SHOWN);
    return this.http.get<FollowUpPage>(`${this.url}/follow-ups`, { params });
  }
}
