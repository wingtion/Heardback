import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { APPLICATIONS_API } from '../applications/applications-api';
import { ApplicationMatch, MatchSkill } from './cv.models';
import { MatchPanel, matchSummary } from './match-panel';

@Component({
  imports: [MatchPanel],
  template: `<app-match-panel
    applicationId="app-1"
    [version]="version()"
    (edit)="edits = edits + 1"
  />`,
})
class PanelHost {
  readonly version = signal(7);
  edits = 0;
}

const MATCH_URL = `${APPLICATIONS_API}/app-1/match`;

const skill = (name: string): MatchSkill => ({ name, category: 'Language' });

describe('MatchPanel', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function render(): Promise<void> {
    TestBed.tick();
    await new Promise((resolve) => setTimeout(resolve));
    TestBed.tick();
  }

  async function create(
    match: ApplicationMatch | null,
  ): Promise<{ host: PanelHost; element: HTMLElement }> {
    const fixture = TestBed.createComponent(PanelHost);
    await render();
    if (match) {
      http.expectOne(MATCH_URL).flush(match);
      await render();
    }
    return { host: fixture.componentInstance, element: fixture.nativeElement as HTMLElement };
  }

  function chips(element: HTMLElement, label: string): string[] {
    const list = element.querySelector(`ul[aria-label="${label}"]`);
    return Array.from(
      list?.querySelectorAll('app-skill-chip') ?? [],
      (chip) => chip.textContent?.trim() ?? '',
    );
  }

  it('matchSummary_Counts_ReadPlainly', () => {
    expect(matchSummary(3, 5, 60)).toBe('3 of 5 skills · 60%');
    expect(matchSummary(1, 1, 100)).toBe('1 of 1 skill · 100%');
  });

  it('load_InFlight_ShowsChecking', async () => {
    const { element } = await create(null);

    expect(element.textContent).toContain('Checking your CV…');
    http
      .expectOne(MATCH_URL)
      .flush({ state: 'NoSkillsInPosting', score: null, matched: [], missing: [] });
  });

  it('load_Ready_ListsMatchedAndMissingWithTheScore', async () => {
    const { element } = await create({
      state: 'Ready',
      score: 60,
      matched: [skill('C#'), skill('.NET'), skill('PostgreSQL')],
      missing: [skill('Kubernetes'), skill('Azure')],
    });

    expect(element.textContent).toContain('3 of 5 skills · 60%');
    expect(chips(element, 'Skills your CV shows')).toEqual(['C#', '.NET', 'PostgreSQL']);
    expect(chips(element, 'Skills missing from your CV')).toEqual(['Kubernetes', 'Azure']);
  });

  it('load_NoCv_ListsThePostingSkillsAndLinksToTheCvPage', async () => {
    const { element } = await create({
      state: 'NoCv',
      score: null,
      matched: [],
      missing: [skill('C#'), skill('Docker')],
    });

    expect(chips(element, 'Skills this posting asks for')).toEqual(['C#', 'Docker']);
    expect(element.querySelector('a')?.getAttribute('href')).toBe('/cv');
    expect(element.textContent).not.toContain('%');
  });

  it('load_NoPostingText_OffersEdit', async () => {
    const { host, element } = await create({
      state: 'NoPostingText',
      score: null,
      matched: [],
      missing: [],
    });

    expect(element.textContent).toContain('Add the job description to see how your CV matches.');
    element.querySelector('button')?.click();

    expect(host.edits).toBe(1);
  });

  it('load_NoSkillsInPosting_SaysSoNeutrally', async () => {
    const { element } = await create({
      state: 'NoSkillsInPosting',
      score: null,
      matched: [],
      missing: [],
    });

    expect(element.textContent).toContain('No skills Heardback knows were found in this posting.');
    expect(element.querySelector('[role="alert"]')).toBeNull();
  });

  it('load_ServerDown_ShowsAnErrorAndTryAgainReloads', async () => {
    const { element } = await create(null);
    http.expectOne(MATCH_URL).flush(null, { status: 500, statusText: 'Server Error' });
    await render();

    expect(element.querySelector('[role="alert"]')?.textContent).toContain(
      'Couldn’t check your CV.',
    );
    element.querySelector('button')?.click();
    await render();

    http
      .expectOne(MATCH_URL)
      .flush({ state: 'NoSkillsInPosting', score: null, matched: [], missing: [] });
  });

  it('version_Changed_ChecksAgain', async () => {
    const { host } = await create({
      state: 'NoSkillsInPosting',
      score: null,
      matched: [],
      missing: [],
    });

    host.version.set(8);
    await render();

    http
      .expectOne(MATCH_URL)
      .flush({ state: 'NoSkillsInPosting', score: null, matched: [], missing: [] });
  });
});
