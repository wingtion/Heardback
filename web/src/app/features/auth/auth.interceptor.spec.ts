import {
  HttpClient,
  HttpErrorResponse,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { AUTH_API } from './auth-paths';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';
import { authResponse } from './auth.testing';

const unauthorized = { status: 401, statusText: 'Unauthorized' };
const applicationsUrl = '/api/applications';

describe('authInterceptor', () => {
  let client: HttpClient;
  let http: HttpTestingController;
  let auth: AuthService;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    client = TestBed.inject(HttpClient);
    http = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);

    const login = firstValueFrom(auth.login({ email: 'ada@example.com', password: 'pw' }));
    http.expectOne(AUTH_API.login).flush(authResponse({ token: 'old-token' }));
    await login;
  });

  afterEach(() => http.verify());

  it('adds the access token to API requests', () => {
    client.get(applicationsUrl).subscribe();

    const request = http.expectOne(applicationsUrl);
    expect(request.request.headers.get('Authorization')).toBe('Bearer old-token');
    request.flush([]);
  });

  it('never sends the token to other hosts', () => {
    client.get('https://example.com/api/data').subscribe();

    const request = http.expectOne('https://example.com/api/data');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });

  it('leaves auth endpoints alone', () => {
    client.post(AUTH_API.logout, null).subscribe();

    const request = http.expectOne(AUTH_API.logout);
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush(null);
  });

  it('on 401 refreshes once and retries with the new token', async () => {
    const result = firstValueFrom(client.get<string[]>(applicationsUrl));

    http.expectOne(applicationsUrl).flush(null, unauthorized);
    http.expectOne(AUTH_API.refresh).flush(authResponse({ token: 'new-token' }));
    const retry = http.expectOne(applicationsUrl);
    expect(retry.request.headers.get('Authorization')).toBe('Bearer new-token');
    retry.flush(['ok']);

    expect(await result).toEqual(['ok']);
  });

  it('concurrent 401s share a single refresh', async () => {
    const first = firstValueFrom(client.get(`${applicationsUrl}/1`));
    const second = firstValueFrom(client.get(`${applicationsUrl}/2`));

    http.expectOne(`${applicationsUrl}/1`).flush(null, unauthorized);
    http.expectOne(`${applicationsUrl}/2`).flush(null, unauthorized);
    http.expectOne(AUTH_API.refresh).flush(authResponse({ token: 'new-token' }));
    http.expectOne(`${applicationsUrl}/1`).flush({});
    http.expectOne(`${applicationsUrl}/2`).flush({});

    await Promise.all([first, second]);
  });

  it('a 401 arriving after the session was already refreshed retries without refreshing again', async () => {
    const first = firstValueFrom(client.get(`${applicationsUrl}/1`));
    const late = firstValueFrom(client.get(`${applicationsUrl}/2`));
    const lateRequest = http.expectOne(`${applicationsUrl}/2`);

    http.expectOne(`${applicationsUrl}/1`).flush(null, unauthorized);
    http.expectOne(AUTH_API.refresh).flush(authResponse({ token: 'new-token' }));
    http.expectOne(`${applicationsUrl}/1`).flush({});
    await first;

    lateRequest.flush(null, unauthorized);
    http.expectNone(AUTH_API.refresh);
    const retry = http.expectOne(`${applicationsUrl}/2`);
    expect(retry.request.headers.get('Authorization')).toBe('Bearer new-token');
    retry.flush({});

    await late;
  });

  it('when the refresh fails, passes on the original 401 and signs out', async () => {
    const result = firstValueFrom(client.get(applicationsUrl));

    http.expectOne(applicationsUrl).flush(null, unauthorized);
    http.expectOne(AUTH_API.refresh).flush(null, unauthorized);

    const error = await result.catch((caught: unknown) => caught);
    expect(error).toBeInstanceOf(HttpErrorResponse);
    expect((error as HttpErrorResponse).url).toContain(applicationsUrl);
    expect(auth.isAuthenticated()).toBe(false);
  });

  it('does not refresh on other errors', async () => {
    const result = firstValueFrom(client.get(applicationsUrl));

    http.expectOne(applicationsUrl).flush(null, { status: 403, statusText: 'Forbidden' });

    await expect(result).rejects.toBeInstanceOf(HttpErrorResponse);
    http.expectNone(AUTH_API.refresh);
  });
});
