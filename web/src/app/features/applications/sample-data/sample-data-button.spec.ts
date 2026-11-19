import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { AuthService } from '../../auth/auth.service';
import { APPLICATIONS_API } from '../applications-api';
import { SampleDataButton, samplesMessage } from './sample-data-button';

@Component({
  imports: [SampleDataButton],
  template: `<app-sample-data-button (added)="added = added + 1" />`,
})
class Host {
  added = 0;
}

const URL = `${APPLICATIONS_API}/samples`;

describe('SampleDataButton', () => {
  let http: HttpTestingController;
  const isAdmin = signal(true);

  beforeEach(() => {
    isAdmin.set(true);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { isAdmin } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function render(): Promise<void> {
    TestBed.tick();
    await new Promise((resolve) => setTimeout(resolve));
    TestBed.tick();
  }

  it('samplesMessage_Counts_ReadPlainly', () => {
    expect(samplesMessage({ added: 8, skipped: 0 })).toBe('Added 8 sample applications');
    expect(samplesMessage({ added: 1, skipped: 7 })).toBe('Added 1 sample application');
    expect(samplesMessage({ added: 0, skipped: 8 })).toBe(
      'All sample applications are already in your list',
    );
  });

  it('member_NeverAsksAndShowsNothing', async () => {
    isAdmin.set(false);
    const fixture = TestBed.createComponent(Host);
    await render();

    expect((fixture.nativeElement as HTMLElement).querySelector('button')).toBeNull();
  });

  it('admin_SettingOff_ShowsNothing', async () => {
    const fixture = TestBed.createComponent(Host);
    await render();
    http.expectOne(URL).flush({ enabled: false });
    await render();

    expect((fixture.nativeElement as HTMLElement).querySelector('button')).toBeNull();
  });

  it('add_Clicked_PostsShowsTheResultAndTellsThePage', async () => {
    const fixture = TestBed.createComponent(Host);
    await render();
    http.expectOne(URL).flush({ enabled: true });
    await render();
    const element = fixture.nativeElement as HTMLElement;

    element.querySelector('button')!.click();
    await render();
    const request = http.expectOne(URL);
    expect(request.request.method).toBe('POST');
    request.flush({ added: 8, skipped: 0 });
    await render();

    expect(element.textContent).toContain('Added 8 sample applications');
    expect(fixture.componentInstance.added).toBe(1);
  });

  it('add_ServerDown_SaysSo', async () => {
    const fixture = TestBed.createComponent(Host);
    await render();
    http.expectOne(URL).flush({ enabled: true });
    await render();
    const element = fixture.nativeElement as HTMLElement;

    element.querySelector('button')!.click();
    await render();
    http.expectOne(URL).flush(null, { status: 500, statusText: 'Server Error' });
    await render();

    expect(element.textContent).toContain('Couldn’t add the samples.');
    expect(fixture.componentInstance.added).toBe(0);
  });
});
