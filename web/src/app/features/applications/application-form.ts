import { AbstractControl, ValidationErrors } from '@angular/forms';
import { localToday } from './application-display';
import { ApplicationChannel, EARLIEST_DATE } from './applications.models';

/** Field copy shared by Add application and Edit application (docs/design.md §14). */
export const APPLICATION_FORM_MESSAGES = {
  url: 'Enter a full link that starts with https://',
  company: 'Enter the company name',
  role: 'Enter the role',
  dateInFuture: 'Pick today or an earlier date',
  dateTooEarly: 'Pick a date after 1 January 2000',
} as const;

/** The fields of both forms, for error messages keyed by field. */
export type ApplicationFormField =
  'url' | 'company' | 'role' | 'appliedOn' | 'postingText' | 'channel' | 'notes';

/** The channel a new application starts with. */
export const DEFAULT_CHANNEL: ApplicationChannel = 'LinkedInEasyApply';

const HTTP_SCHEMES = new Set(['http:', 'https:']);
const HOST_LABEL_SEPARATOR = '.';

/** A link the server would accept: absolute http(s) with a dotted host. The server has the last word. */
export function isPostingLink(value: string): boolean {
  try {
    const url = new URL(value.trim());
    return HTTP_SCHEMES.has(url.protocol) && url.hostname.includes(HOST_LABEL_SEPARATOR);
  } catch {
    return false;
  }
}

export function linkValidator(control: AbstractControl<string>): ValidationErrors | null {
  const value = control.value.trim();
  return value === '' || isPostingLink(value) ? null : { link: true };
}

export function requiredText(control: AbstractControl<string>): ValidationErrors | null {
  return control.value.trim() === '' ? { required: true } : null;
}

/** Today or earlier on this device, and not before 2000. */
export function appliedOnValidator(control: AbstractControl<string>): ValidationErrors | null {
  const value = control.value;
  if (value === '' || value > localToday(new Date())) {
    return { future: true };
  }
  return value < EARLIEST_DATE ? { tooEarly: true } : null;
}

/** The applied date's message for the errors the validators above set. */
export function appliedOnError(control: AbstractControl<string>): string | undefined {
  if (control.hasError('future')) {
    return APPLICATION_FORM_MESSAGES.dateInFuture;
  }
  return control.hasError('tooEarly') ? APPLICATION_FORM_MESSAGES.dateTooEarly : undefined;
}
