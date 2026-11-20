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

/**
 * The application the company page was opened from, so "Back to Applications" opens it again. Only ever used to
 * select one of the user's own applications in the list; an unknown id shows the list's "not found" message.
 */
export const FROM_APPLICATION_PARAM = 'from';
