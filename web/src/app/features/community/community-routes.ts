import { SHELL_ROUTES } from '../../shell/nav-items';

/**
 * The company page sits under Applications, so the Applications nav item stays active there
 * (docs/design.md §18). Only reached from the user's own applications.
 */
export const COMPANY_ROUTE = 'applications/companies/:key';

export const COMPANY_KEY_PARAM = 'key';

export function companyPath(companyKey: string): readonly string[] {
  return [SHELL_ROUTES.applications, 'companies', companyKey];
}
