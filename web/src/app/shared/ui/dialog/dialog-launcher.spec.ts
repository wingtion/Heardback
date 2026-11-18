import { BreakpointObserver } from '@angular/cdk/layout';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { DialogLauncher } from './dialog-launcher';

@Component({ template: '<p>Dialog content</p>' })
class StubDialog {}

@Component({ template: '' })
class StubPage {}

describe('DialogLauncher', () => {
  function configure(desktop: boolean): void {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'page', component: StubPage },
          { path: 'login', component: StubPage },
        ]),
        {
          provide: BreakpointObserver,
          useValue: {
            isMatched: () => desktop,
            observe: () => of({ matches: desktop, breakpoints: {} }),
          },
        },
      ],
    });
  }

  function backdropClasses(): DOMTokenList | undefined {
    return document.querySelector('.cdk-overlay-backdrop')?.classList;
  }

  afterEach(() => {
    document.querySelectorAll('.cdk-overlay-container').forEach((element) => element.remove());
  });

  it('open_Desktop_UsesATitleIdAndTheScrimBackdrop', () => {
    configure(true);

    const ref = TestBed.inject(DialogLauncher).open(StubDialog);

    expect(ref.config.ariaLabelledBy).toMatch(/^dialog-title-\d+$/);
    expect(backdropClasses()).toContain('bg-scrim');
    ref.close();
  });

  it('open_PhoneBottomSheet_UsesTheSheetScrim', () => {
    configure(false);

    const ref = TestBed.inject(DialogLauncher).open(StubDialog);

    expect(backdropClasses()).toContain('bg-scrim-sheet');
    expect(ref.config.width).toBe('100%');
    ref.close();
  });

  it('open_PhoneFullScreen_FillsTheScreen', () => {
    configure(false);

    const ref = TestBed.inject(DialogLauncher).open(StubDialog, { mobile: 'full-screen' });

    expect(ref.config.width).toBe('100%');
    expect(ref.config.height).toBe('100%');
    expect(backdropClasses()).toContain('bg-scrim');
    ref.close();
  });

  // Regression: CDK only closes on browser back/forward, so a dialog stayed over the sign-in page
  // after the session expired.
  it('open_AppNavigatesElsewhere_ClosesTheDialog', async () => {
    configure(false);
    const ref = TestBed.inject(DialogLauncher).open(StubDialog);
    let closed = false;
    ref.closed.subscribe(() => (closed = true));

    await TestBed.inject(Router).navigateByUrl('/login');

    expect(closed).toBe(true);
    expect(document.querySelector('.cdk-dialog-container')).toBeNull();
  });
});
