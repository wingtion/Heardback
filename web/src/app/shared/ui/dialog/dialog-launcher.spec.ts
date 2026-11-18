import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { DialogLauncher } from './dialog-launcher';

@Component({ template: '<p>Dialog content</p>' })
class StubDialog {}

@Component({ template: '' })
class StubPage {}

describe('DialogLauncher', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'page', component: StubPage },
          { path: 'login', component: StubPage },
        ]),
      ],
    });
  });

  afterEach(() => {
    document.querySelectorAll('.cdk-overlay-container').forEach((element) => element.remove());
  });

  it('open_Default_UsesATitleIdAndTheScrimBackdrop', () => {
    const ref = TestBed.inject(DialogLauncher).open(StubDialog);

    expect(ref.config.ariaLabelledBy).toMatch(/^dialog-title-\d+$/);
    expect(document.querySelector('.cdk-overlay-backdrop')?.classList).toContain('bg-scrim');
    ref.close();
  });

  // Regression: CDK only closes on browser back/forward, so a dialog stayed over the sign-in page
  // after the session expired.
  it('open_AppNavigatesElsewhere_ClosesTheDialog', async () => {
    const ref = TestBed.inject(DialogLauncher).open(StubDialog);
    let closed = false;
    ref.closed.subscribe(() => (closed = true));

    await TestBed.inject(Router).navigateByUrl('/login');

    expect(closed).toBe(true);
    expect(document.querySelector('.cdk-dialog-container')).toBeNull();
  });
});
