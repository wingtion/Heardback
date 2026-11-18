import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  TestRequest,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CV_API } from './cv-api';
import { CvPage } from './cv-page';
import { Cv } from './cv.models';

const STORED: Cv = {
  uploadedAt: '2026-11-18T10:00:00Z',
  pageCount: 2,
  text: 'Ada Lovelace\n[email]\nSkills: C#, .NET',
  skills: ['C#', '.NET'],
  excludedSkills: [],
};

describe('CvPage', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    document.querySelectorAll('.cdk-overlay-container').forEach((element) => element.remove());
    http.verify();
  });

  async function render(): Promise<void> {
    TestBed.tick();
    await new Promise((resolve) => setTimeout(resolve));
    TestBed.tick();
  }

  async function create(stored: Cv | null = STORED): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(CvPage);
    await render();
    const request = http.expectOne(
      (candidate) => candidate.url === CV_API && candidate.method === 'GET',
    );
    if (stored) {
      request.flush(stored);
    } else {
      request.flush(null, { status: 204, statusText: 'No Content' });
    }
    await render();
    return fixture.nativeElement as HTMLElement;
  }

  function button(container: ParentNode, text: string): HTMLButtonElement {
    const found = Array.from(container.querySelectorAll('button')).find(
      (candidate) => candidate.textContent?.trim() === text,
    );
    if (!found) throw new Error(`No button "${text}"`);
    return found;
  }

  async function choose(page: HTMLElement, file: File): Promise<void> {
    const input = page.querySelector<HTMLInputElement>('input[type="file"]');
    if (!input) throw new Error('No file input');
    Object.defineProperty(input, 'files', { value: { item: () => file }, configurable: true });
    input.dispatchEvent(new Event('change'));
    await render();
  }

  function uploadRequest(): TestRequest {
    return http.expectOne((candidate) => candidate.url === CV_API && candidate.method === 'POST');
  }

  const pdf = new File(['%PDF-1.7'], 'cv.pdf', { type: 'application/pdf' });

  it('load_NoCv_ShowsTheEmptyUploadArea', async () => {
    const page = await create(null);

    expect(page.textContent).toContain('No CV yet');
    expect(page.textContent).toContain('PDF, up to 5 MB and 10 pages');
    expect(page.textContent).toContain('Only the text of your CV is kept');
    expect(page.querySelector('#cv-skills-title')).toBeNull();
  });

  it('load_StoredCv_ShowsTheSummarySkillsAndCollapsedText', async () => {
    const page = await create();

    expect(page.textContent).toContain('Uploaded 18 Nov 2026');
    expect(page.textContent).toContain('2 pages');
    expect(
      Array.from(page.querySelectorAll('app-skill-chip'), (chip) => chip.textContent?.trim()),
    ).toEqual(['C#', '.NET']);
    expect(page.querySelector('#cv-stored-text')).toBeNull();

    button(page, 'Show stored text').click();
    await render();

    const text = page.querySelector('#cv-stored-text');
    expect(text?.textContent).toContain('Skills: C#, .NET');
    expect(Array.from(text?.querySelectorAll('mark') ?? [], (mark) => mark.textContent)).toEqual([
      '[email]',
    ]);
  });

  it('load_ServerDown_ShowsErrorAndTryAgainReloads', async () => {
    const fixture = TestBed.createComponent(CvPage);
    await render();
    http.expectOne(CV_API).flush(null, { status: 500, statusText: 'Server Error' });
    await render();
    const page = fixture.nativeElement as HTMLElement;

    expect(page.textContent).toContain('Couldn’t load your CV');
    button(page, 'Try again').click();
    await render();
    http.expectOne(CV_API).flush(STORED);
    await render();

    expect(page.textContent).toContain('Skills found');
  });

  it('upload_Pdf_ShowsReadingThenTheNewCv', async () => {
    const page = await create(null);

    await choose(page, pdf);
    expect(page.textContent).toContain('Reading your CV…');
    const request = uploadRequest();
    expect(request.request.body).toBeInstanceOf(FormData);
    const sent = (request.request.body as FormData).get('file') as File;
    expect(sent.name).toBe('cv.pdf');
    expect(sent.size).toBe(pdf.size);
    request.flush(STORED);
    await render();

    expect(page.querySelector('[role="status"] app-inline-success')?.textContent).toContain(
      'CV uploaded',
    );
    expect(page.textContent).toContain('Replace your CV');
    expect(page.textContent).toContain('Skills found');
  });

  it('upload_WordFile_IsRejectedWithoutSending', async () => {
    const page = await create(null);

    await choose(page, new File(['x'], 'cv.docx', { type: 'application/msword' }));

    expect(page.querySelector('app-banner[role="alert"]')?.textContent).toContain(
      'Choose a PDF file.',
    );
    http.expectNone((candidate) => candidate.method === 'POST');
  });

  it('upload_ServerRejectsTheFile_ShowsItsMessage', async () => {
    const page = await create(null);
    await choose(page, pdf);

    uploadRequest().flush(
      { code: 'matching.cv_no_text', errors: { file: ['This PDF has no text we can read.'] } },
      { status: 400, statusText: 'Bad Request' },
    );
    await render();

    expect(page.querySelector('app-banner[role="alert"]')?.textContent).toContain(
      'This PDF has no text we can read.',
    );
    expect(page.textContent).toContain('No CV yet');
  });

  it('upload_OverTheHourlyLimit_ShowsAWarningWithTheWait', async () => {
    const page = await create();
    await choose(page, pdf);

    uploadRequest().flush(null, {
      status: 429,
      statusText: 'Too Many Requests',
      headers: { 'Retry-After': '60' },
    });
    await render();

    expect(page.textContent).toContain('Try again in about 1 minute.');
    expect(page.textContent).toContain('Skills found');
  });

  it('upload_ServerDown_SaysSo', async () => {
    const page = await create();
    await choose(page, pdf);

    uploadRequest().flush(null, { status: 500, statusText: 'Server Error' });
    await render();

    expect(page.textContent).toContain('Couldn’t upload your CV');
  });

  it('delete_Confirmed_ReturnsToTheEmptyState', async () => {
    const page = await create();

    button(page, 'Delete CV').click();
    await render();
    const dialog = document.querySelector('app-delete-cv-dialog');
    if (!dialog) throw new Error('No dialog');
    expect(dialog.textContent).toContain('Delete your CV?');
    button(dialog, 'Delete CV').click();
    await render();
    http
      .expectOne((candidate) => candidate.url === CV_API && candidate.method === 'DELETE')
      .flush(null, { status: 204, statusText: 'No Content' });
    await render();

    expect(page.textContent).toContain('No CV yet');
    expect(document.querySelector('app-delete-cv-dialog')).toBeNull();
  });

  it('delete_ServerDown_KeepsTheDialogWithAnError', async () => {
    const page = await create();
    button(page, 'Delete CV').click();
    await render();
    const dialog = document.querySelector('app-delete-cv-dialog');
    if (!dialog) throw new Error('No dialog');

    button(dialog, 'Delete CV').click();
    await render();
    http
      .expectOne((candidate) => candidate.method === 'DELETE')
      .flush(null, {
        status: 500,
        statusText: 'Server Error',
      });
    await render();

    expect(dialog.textContent).toContain('Couldn’t delete your CV');
    expect(page.textContent).toContain('Skills found');
  });

  function excludeRequest(): TestRequest {
    return http.expectOne(`${CV_API}/excluded-skills`);
  }

  it('removeSkills_ChipClicked_SendsTheWholeListAndMovesItToNotCounted', async () => {
    const page = await create({ ...STORED, skills: ['C#', '.NET', 'Rust'] });
    expect(page.querySelector('app-skill-chip button')).toBeNull();

    button(page, 'Remove skills').click();
    await render();
    page
      .querySelector<HTMLButtonElement>('button[aria-label="Remove Rust: not my skill"]')
      ?.click();
    await render();

    const request = excludeRequest();
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ skills: ['Rust'] });
    request.flush({ ...STORED, excludedSkills: ['Rust'] });
    await render();

    expect(page.textContent).toContain('Not counted');
    expect(page.querySelector('button[aria-label="Count Rust again"]')).not.toBeNull();
  });

  it('restoreSkill_Clicked_RemovesItFromTheList', async () => {
    const page = await create({ ...STORED, excludedSkills: ['Go', 'Rust'] });

    page.querySelector<HTMLButtonElement>('button[aria-label="Count Rust again"]')?.click();
    await render();

    expect(excludeRequest().request.body).toEqual({ skills: ['Go'] });
  });

  it('removeSkills_ServerDown_SaysSoAndKeepsTheSkill', async () => {
    const page = await create();
    button(page, 'Remove skills').click();
    await render();
    page.querySelector<HTMLButtonElement>('button[aria-label="Remove C#: not my skill"]')?.click();
    await render();

    excludeRequest().flush(null, { status: 500, statusText: 'Server Error' });
    await render();

    expect(page.textContent).toContain('Couldn’t save your skills');
    expect(page.querySelector('button[aria-label="Remove C#: not my skill"]')).not.toBeNull();
  });
});
