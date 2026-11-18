import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { AUTH_API } from './features/auth/auth-paths';
import { AuthService } from './features/auth/auth.service';

describe('App', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  it('startup_WhileSessionRestores_ShowsTheLoadingScreenOnly', async () => {
    const http = TestBed.inject(HttpTestingController);
    const restore = TestBed.inject(AuthService).restoreSession();
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.textContent).toContain('Restoring your session…');

    http.expectOne(AUTH_API.refresh).flush(null, { status: 401, statusText: 'Unauthorized' });
    await restore;
    fixture.detectChanges();

    expect(root.textContent).not.toContain('Restoring your session…');
    expect(root.querySelector('router-outlet')).not.toBeNull();
    http.verify();
  });
});
