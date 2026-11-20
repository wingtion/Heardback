import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { COMMUNITY_API } from './community-api';
import { CommunitySection } from './community-section';
import { ApplicationCommunity, CommunityNumbers, GhostSignal } from './community.models';
import { TEST_NUMBERS, quietCommunity } from './community.testing';

@Component({
  imports: [CommunitySection],
  template: `<app-community-section
    applicationId="app-1"
    company="Northwind"
    [version]="version()"
  />`,
})
class SectionHost {
  readonly version = signal(1);
}

const URL = `${COMMUNITY_API}/applications/app-1`;

const COMPANY: CommunityNumbers = {
  members: 11,
  applications: 14,
  responded: 3,
  responseRatePercent: 21,
  averageDays: 23.5,
  medianDays: 19,
};

const HIGH: GhostSignal = {
  score: 5,
  maxScore: 5,
  level: 'High',
  reasons: [
    {
      kind: 'NoPostingResponse',
      points: 2,
      text: 'None of the 3 other members who applied heard back.',
    },
    { kind: 'LongListed', points: 1, text: 'First added here 45 days ago.' },
  ],
};

describe('CommunitySection', () => {
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
    community?: ApplicationCommunity,
  ): Promise<{ host: SectionHost; element: HTMLElement }> {
    const fixture = TestBed.createComponent(SectionHost);
    await render();
    if (community) {
      http.expectOne(URL).flush(community);
      await render();
    }
    return { host: fixture.componentInstance, element: fixture.nativeElement as HTMLElement };
  }

  function text(element: HTMLElement): string {
    return element.textContent?.replace(/\s+/g, ' ') ?? '';
  }

  function columnHeadings(element: HTMLElement): string[] {
    return Array.from(element.querySelectorAll('th[scope=col]'), (th) => th.textContent!.trim());
  }

  it('load_InFlight_ShowsLoading', async () => {
    const { element } = await create();

    expect(element.querySelector('[role=status]')?.textContent).toContain(
      'Loading community numbers…',
    );
    http.expectOne(URL).flush(quietCommunity('app-1'));
  });

  it('load_ServerDown_ShowsErrorAndTryAgainReloads', async () => {
    const { element } = await create();
    http.expectOne(URL).flush(null, { status: 500, statusText: 'Server Error' });
    await render();

    expect(text(element)).toContain('Couldn’t load community numbers');
    expect(text(element)).toContain('Your own application details are fine.');
    element.querySelector<HTMLButtonElement>('[role=alert] button')!.click();
    await render();
    http.expectOne(URL).flush(quietCommunity('app-1'));
  });

  it('load_NoGroupHasEnoughMembers_ExplainsWhyAndStillLinksTheCompany', async () => {
    const { element } = await create(quietCommunity('app-1'));

    expect(text(element)).toContain('Not enough data yet');
    expect(text(element)).toContain('Fewer than 3 other members have applied to this job.');
    expect(element.querySelector('table')).toBeNull();
    const link = element.querySelector('a');
    expect(link?.textContent).toContain('All your applications at Northwind');
    expect(link?.getAttribute('href')).toBe('/applications/companies/northwind?from=app-1');
  });

  it('load_BothGroupsAndHighRisk_ShowsEveryReasonAndBothColumns', async () => {
    const { element } = await create({
      applicationId: 'app-1',
      companyKey: 'northwind',
      posting: TEST_NUMBERS,
      company: COMPANY,
      ghost: HIGH,
    });

    expect(text(element)).toContain('Ghost risk: High');
    expect(text(element)).toContain('5 of 5');
    expect(text(element)).toContain('This job is likely inactive. Here is why:');
    expect(Array.from(element.querySelectorAll('li'), (li) => li.textContent!.trim())).toEqual(
      HIGH.reasons.map((reason) => reason.text),
    );
    expect(columnHeadings(element)).toEqual(['This job', 'This company']);
    expect(text(element)).toContain('means nobody has heard back yet');
    expect(text(element)).toContain('You are not counted.');
  });

  it('load_OnlyTheCompany_ShowsOneColumnAndSaysThePostingHasTooFew', async () => {
    const { element } = await create({
      applicationId: 'app-1',
      companyKey: 'northwind',
      posting: null,
      company: COMPANY,
      ghost: { score: 1, maxScore: 5, level: 'Low', reasons: [] },
    });

    expect(text(element)).toContain(
      'Fewer than 3 other members applied to this exact job, so only company numbers are shown.',
    );
    expect(columnHeadings(element)).toEqual(['This company']);
    expect(text(element)).toContain('Ghost risk: Low. No warning signs from other members so far.');
    expect(text(element)).not.toContain('means nobody has heard back yet');
  });

  it('load_NoCompanyKey_ExplainsInsteadOfLinking', async () => {
    const { element } = await create({
      applicationId: 'app-1',
      companyKey: null,
      posting: TEST_NUMBERS,
      company: null,
      ghost: { score: 2, maxScore: 5, level: 'Medium', reasons: [] },
    });

    expect(text(element)).toContain('This job may be inactive.');
    expect(text(element)).not.toContain('so only job numbers are shown.');
    expect(text(element)).toContain('added before companies were linked');
    expect(element.querySelector('a')).toBeNull();
  });

  it('version_Changed_LoadsAgain', async () => {
    const { host } = await create(quietCommunity('app-1'));

    host.version.set(2);
    await render();

    http.expectOne(URL).flush(quietCommunity('app-1'));
  });
});
