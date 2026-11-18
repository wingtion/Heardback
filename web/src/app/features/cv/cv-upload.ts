import { HttpHeaders, HttpStatusCode } from '@angular/common/http';
import { httpErrorOf, validationErrors } from '../../core/http-errors';
import { CV_LIMITS, PDF_EXTENSION, PDF_TYPE } from './cv.models';

/** Why an upload did not go through, as the CV page shows it. */
export type UploadProblem =
  /** Something about the file; shown as an error under the upload area. */
  | { readonly kind: 'file'; readonly message: string }
  /** The hourly limit; a warning, since waiting fixes it. */
  | { readonly kind: 'limit'; readonly message: string }
  /** The server did not answer. */
  | { readonly kind: 'unavailable' };

const FILE_FIELD = 'file';
const SECONDS_PER_MINUTE = 60;
const BYTES_PER_MEGABYTE = 1024 * 1024;

/** Server copy for the same problems, used when the browser can tell before uploading. */
export const UPLOAD_MESSAGES = {
  notPdf: 'Choose a PDF file.',
  tooLarge: `The PDF is larger than ${CV_LIMITS.maxBytes / BYTES_PER_MEGABYTE} MB.`,
} as const;

/** "PDF, up to 5 MB and 10 pages". */
export const UPLOAD_LIMITS_TEXT = `PDF, up to ${CV_LIMITS.maxBytes / BYTES_PER_MEGABYTE} MB and ${CV_LIMITS.maxPages} pages`;

/** Checks the browser can make before sending the file; the server checks everything again. */
export function checkFile(file: File): UploadProblem | null {
  const looksLikePdf = file.type === PDF_TYPE || file.name.toLowerCase().endsWith(PDF_EXTENSION);
  if (!looksLikePdf) {
    return { kind: 'file', message: UPLOAD_MESSAGES.notPdf };
  }
  return file.size > CV_LIMITS.maxBytes
    ? { kind: 'file', message: UPLOAD_MESSAGES.tooLarge }
    : null;
}

export function uploadProblem(error: unknown): UploadProblem {
  const response = httpErrorOf(error);
  switch (response?.status) {
    case HttpStatusCode.BadRequest: {
      const message = validationErrors(response.error)[FILE_FIELD];
      return message ? { kind: 'file', message } : { kind: 'unavailable' };
    }
    case HttpStatusCode.PayloadTooLarge:
      return { kind: 'file', message: UPLOAD_MESSAGES.tooLarge };
    case HttpStatusCode.TooManyRequests:
      return { kind: 'limit', message: limitMessage(response.headers) };
    default:
      return { kind: 'unavailable' };
  }
}

/** "You've uploaded 5 CVs in the last hour. Try again in about 12 minutes." */
function limitMessage(headers: HttpHeaders): string {
  const seconds = Number(headers.get('Retry-After'));
  const uploaded = `You’ve uploaded ${CV_LIMITS.uploadsPerHour} CVs in the last hour.`;
  if (!Number.isFinite(seconds) || seconds <= 0) {
    return `${uploaded} Try again later.`;
  }
  const minutes = Math.max(1, Math.ceil(seconds / SECONDS_PER_MINUTE));
  return `${uploaded} Try again in about ${minutes} ${minutes === 1 ? 'minute' : 'minutes'}.`;
}
