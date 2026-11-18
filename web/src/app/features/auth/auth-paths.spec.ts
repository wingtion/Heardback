import { AUTH_ROUTES, safeReturnUrl } from './auth-paths';

describe('safeReturnUrl', () => {
  it('safeReturnUrl_AppPath_IsKept', () => {
    expect(safeReturnUrl('/applications/42?tab=notes')).toBe('/applications/42?tab=notes');
  });

  it.each([
    ['missing', null],
    ['empty', ''],
    ['absolute URL', 'https://evil.example/'],
    ['protocol-relative URL', '//evil.example'],
    ['backslash trick', '/\\evil.example'],
    ['relative path', 'applications'],
  ])('safeReturnUrl_%s_GoesHome', (_case, value) => {
    expect(safeReturnUrl(value)).toBe(AUTH_ROUTES.home);
  });
});
