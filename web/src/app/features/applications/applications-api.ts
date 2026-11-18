import { HttpClient, HttpParams, HttpStatusCode } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from '../../core/api-base-url';
import {
  APPLICATIONS_PAGE_SIZE,
  AddEventRequest,
  Application,
  ApplicationFilter,
  ApplicationPage,
  ApplicationSummary,
  Board,
  BoardFilter,
  CreateApplicationRequest,
  TrackedApplication,
  UpdateApplicationRequest,
} from './applications.models';

export const APPLICATIONS_API = '/api/applications';

/** HTTP calls for the applications pages. Every call only ever sees the signed-in user's data. */
@Injectable({ providedIn: 'root' })
export class ApplicationsApi {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_BASE_URL)}${APPLICATIONS_API}`;

  /** Most recently changed first. Empty filters are left out of the query. */
  list(page: number, filter: ApplicationFilter): Observable<ApplicationPage> {
    let params = new HttpParams().set('page', page).set('pageSize', APPLICATIONS_PAGE_SIZE);
    const search = filter.search.trim();
    if (search !== '') {
      params = params.set('search', search);
    }
    if (filter.status) {
      params = params.set('status', filter.status);
    }
    if (filter.channel) {
      params = params.set('channel', filter.channel);
    }
    return this.http.get<ApplicationPage>(this.url, { params });
  }

  summary(): Observable<ApplicationSummary> {
    return this.http.get<ApplicationSummary>(`${this.url}/summary`);
  }

  get(id: string): Observable<Application> {
    return this.http.get<Application>(`${this.url}/${encodeURIComponent(id)}`);
  }

  /** The user's own application for this link, or null when they do not track it yet. */
  findTracked(url: string): Observable<TrackedApplication | null> {
    const params = new HttpParams().set('url', url);
    return this.http
      .get<TrackedApplication>(`${this.url}/tracked`, { params, observe: 'response' })
      .pipe(
        map((response) => (response.status === HttpStatusCode.NoContent ? null : response.body)),
      );
  }

  create(request: CreateApplicationRequest): Observable<Application> {
    return this.http.post<Application>(this.url, request);
  }

  /** Sends the version the form was opened with; 409 "changed elsewhere" when it is out of date. */
  update(id: string, request: UpdateApplicationRequest): Observable<Application> {
    return this.http.put<Application>(`${this.url}/${encodeURIComponent(id)}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/${encodeURIComponent(id)}`);
  }

  /** Add update and board moves. Returns the application with its new timeline. */
  addEvent(id: string, request: AddEventRequest): Observable<Application> {
    return this.http.post<Application>(`${this.url}/${encodeURIComponent(id)}/events`, request);
  }

  /** Four columns with the newest cards each and the real totals. Empty filters are left out. */
  board(filter: BoardFilter): Observable<Board> {
    let params = new HttpParams();
    const search = filter.search.trim();
    if (search !== '') {
      params = params.set('search', search);
    }
    if (filter.channel) {
      params = params.set('channel', filter.channel);
    }
    return this.http.get<Board>(`${this.url}/board`, { params });
  }
}
