import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CV_API } from '../cv/cv-api';
import { AiConsentSection } from './ai-consent-section';
import { AiConsent } from './ai.models';

const URL = `${CV_API}/ai-consent`;

describe('AiConsentSection', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    document.querySelectorAll('.cdk-overlay-container').forEach((element) => element.remove());
  });

  async function render(): Promise<void> {
    TestBed.tick();
    await new Promise((resolve) => setTimeout(resolve));
    TestBed.tick();
  }

  async function create(consent: AiConsent): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(AiConsentSection);
    await render();
    http.expectOne(URL).flush(consent);
    await render();
    return fixture.nativeElement as HTMLElement;
  }

  function button(root: ParentNode, label: string): HTMLButtonElement {
    const found = Array.from(root.querySelectorAll('button')).find((candidate) =>
      candidate.textContent?.includes(label),
    );
    if (!found) {
      throw new Error(`No "${label}" button`);
    }
    return found;
  }

  it('load_AiOff_ShowsNothing', async () => {
    const element = await create({ allowed: false, allowedAt: null, available: false });

    expect(element.textContent?.trim()).toBe('');
  });

  it('load_NotAllowed_ExplainsAndOffersAllow', async () => {
    const element = await create({ allowed: false, allowedAt: null, available: true });

    expect(element.textContent).toContain('Groq');
    expect(element.textContent).toContain('Not allowed');
    button(element, 'Allow').click();
    await render();
    const request = http.expectOne(URL);
    expect(request.request.body).toEqual({ allowed: true });
    request.flush({ allowed: true, allowedAt: '2026-11-19T09:00:00Z', available: true });
    await render();

    expect(element.textContent).toContain('Allowed since');
  });

  it('withdraw_Confirmed_SendsItAndShowsNotAllowed', async () => {
    const element = await create({
      allowed: true,
      allowedAt: '2026-11-19T09:00:00Z',
      available: true,
    });

    button(element, 'Stop sending my CV').click();
    await render();
    const dialog = document.querySelector('[role="alertdialog"]');
    expect(dialog?.textContent).toContain('All suggestions made from your CV are deleted.');
    button(dialog!, 'Stop and delete suggestions').click();
    await render();
    const request = http.expectOne(URL);
    expect(request.request.body).toEqual({ allowed: false });
    request.flush({ allowed: false, allowedAt: null, available: true });
    await render();

    expect(element.textContent).toContain('Not allowed');
  });
});
