import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/api-base-url';
import { MEMBERS_PAGE_SIZE, MemberPage, ResetCode } from './members.models';

export const MEMBERS_API = '/api/admin/members';

/** HTTP calls for the admin Members page. The API allows admins only. */
@Injectable({ providedIn: 'root' })
export class MembersApi {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_BASE_URL)}${MEMBERS_API}`;

  /** Oldest first, so the list does not shift when someone joins. */
  list(page: number): Observable<MemberPage> {
    const params = new HttpParams().set('page', page).set('pageSize', MEMBERS_PAGE_SIZE);
    return this.http.get<MemberPage>(this.url, { params });
  }

  /** Replaces the member's open code, if any. Not allowed for admins. */
  createResetCode(memberId: string): Observable<ResetCode> {
    return this.http.post<ResetCode>(
      `${this.url}/${encodeURIComponent(memberId)}/reset-code`,
      null,
    );
  }
}
