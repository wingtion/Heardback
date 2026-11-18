import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/api-base-url';
import {
  CreateInviteCodeRequest,
  INVITE_CODES_PAGE_SIZE,
  InviteCode,
  InviteCodePage,
} from './invite-codes.models';

export const INVITE_CODES_API = '/api/admin/invite-codes';

/** HTTP calls for the admin invite codes page. The API allows admins only. */
@Injectable({ providedIn: 'root' })
export class InviteCodesApi {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_BASE_URL)}${INVITE_CODES_API}`;

  /** Newest first. */
  list(page: number): Observable<InviteCodePage> {
    const params = new HttpParams().set('page', page).set('pageSize', INVITE_CODES_PAGE_SIZE);
    return this.http.get<InviteCodePage>(this.url, { params });
  }

  create(request: CreateInviteCodeRequest): Observable<InviteCode> {
    return this.http.post<InviteCode>(this.url, request);
  }
}
