import { Application, ApplicationListItem, ApplicationPage } from './applications.models';

/** Builds an ApplicationListItem for tests. */
export function applicationItem(overrides: Partial<ApplicationListItem> = {}): ApplicationListItem {
  return {
    id: 'app-1',
    company: 'Northwind Logistics',
    role: 'Backend Engineer (.NET)',
    channel: 'LinkedInEasyApply',
    appliedOn: '2026-11-03',
    status: 'Interview',
    noResponseDays: null,
    updatedAt: '2026-11-15T10:00:00Z',
    ...overrides,
  };
}

/** Builds a page of applications for tests. */
export function applicationPage(overrides: Partial<ApplicationPage> = {}): ApplicationPage {
  const items = overrides.items ?? [applicationItem()];
  return {
    items,
    page: 1,
    pageSize: 20,
    totalCount: items.length,
    totalPages: items.length === 0 ? 0 : 1,
    ...overrides,
  };
}

/** Builds an Application (detail) for tests: applied 3 Nov, interview 14 Nov. */
export function application(overrides: Partial<Application> = {}): Application {
  return {
    id: 'app-1',
    company: 'Northwind Logistics',
    role: 'Backend Engineer (.NET)',
    url: 'https://northwind-logistics.example/jobs/4471',
    postingText: null,
    channel: 'LinkedInEasyApply',
    appliedOn: '2026-11-03',
    status: 'Interview',
    noResponseDays: null,
    noResponseSince: null,
    notes: 'Recruiter call went well.',
    updatedAt: '2026-11-15T10:00:00Z',
    version: 7,
    events: [
      {
        id: 'e2',
        status: 'Interview',
        occurredOn: '2026-11-14',
        note: 'Recruiter call, 30 minutes',
        isSubmission: false,
      },
      { id: 'e1', status: 'Applied', occurredOn: '2026-11-03', note: null, isSubmission: true },
    ],
    ...overrides,
  };
}
