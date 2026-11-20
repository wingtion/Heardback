import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { EMPTY, Observable, expand, map, reduce } from 'rxjs';
import { API_BASE_URL } from '../../core/api-base-url';
import {
  ApplicationCommunity,
  CommunityBadge,
  CommunityBadgePage,
  CompanyCommunity,
} from './community.models';

export const COMMUNITY_API = '/api/community';

/** The server's largest page (PageRequest.MaxPageSize), so badges usually arrive in one request. */
export const BADGES_PAGE_SIZE = 100;

/**
 * HTTP calls for community signals (ADR-0013). Every call starts from the signed-in user's own applications;
 * other members only ever appear as counts.
 */
@Injectable({ providedIn: 'root' })
export class CommunityApi {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_BASE_URL)}${COMMUNITY_API}`;

  forApplication(applicationId: string): Observable<ApplicationCommunity> {
    return this.http.get<ApplicationCommunity>(
      `${this.url}/applications/${encodeURIComponent(applicationId)}`,
    );
  }

  forCompany(companyKey: string): Observable<CompanyCommunity> {
    return this.http.get<CompanyCommunity>(
      `${this.url}/companies/${encodeURIComponent(companyKey)}`,
    );
  }

  /** Every Medium and High badge of the user's applications, all pages. */
  badges(): Observable<readonly CommunityBadge[]> {
    return this.badgePage(1).pipe(
      expand((page) =>
        page.page * page.pageSize < page.totalCount ? this.badgePage(page.page + 1) : EMPTY,
      ),
      map((page) => page.items),
      reduce<readonly CommunityBadge[], readonly CommunityBadge[]>(
        (all, items) => [...all, ...items],
        [],
      ),
    );
  }

  private badgePage(page: number): Observable<CommunityBadgePage> {
    const params = new HttpParams().set('page', page).set('pageSize', BADGES_PAGE_SIZE);
    return this.http.get<CommunityBadgePage>(`${this.url}/badges`, { params });
  }
}
