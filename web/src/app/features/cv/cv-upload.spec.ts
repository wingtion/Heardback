import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { CV_LIMITS } from './cv.models';
import { checkFile, uploadProblem } from './cv-upload';
import { storedTextParts } from './stored-text';

describe('CV upload helpers', () => {
  function file(name: string, type: string, size = 100): File {
    return new File([new Uint8Array(size)], name, { type });
  }

  function failure(
    status: number,
    error: unknown = null,
    headers?: HttpHeaders,
  ): HttpErrorResponse {
    return new HttpErrorResponse({ status, error, headers });
  }

  it('checkFile_Pdf_IsAccepted', () => {
    expect(checkFile(file('cv.pdf', 'application/pdf'))).toBeNull();
    expect(checkFile(file('CV.PDF', ''))).toBeNull();
  });

  it('checkFile_WordFile_AsksForAPdf', () => {
    expect(checkFile(file('cv.docx', 'application/msword'))).toEqual({
      kind: 'file',
      message: 'Choose a PDF file.',
    });
  });

  it('checkFile_TooLarge_SaysTheLimit', () => {
    expect(checkFile(file('cv.pdf', 'application/pdf', CV_LIMITS.maxBytes + 1))).toEqual({
      kind: 'file',
      message: 'The PDF is larger than 5 MB.',
    });
  });

  it('uploadProblem_FieldError_UsesTheServerMessage', () => {
    const body = { errors: { file: ['This PDF is password-protected.'] } };

    expect(uploadProblem(failure(400, body))).toEqual({
      kind: 'file',
      message: 'This PDF is password-protected.',
    });
  });

  it('uploadProblem_RequestTooLarge_SaysTheLimit', () => {
    expect(uploadProblem(failure(413))).toEqual({
      kind: 'file',
      message: 'The PDF is larger than 5 MB.',
    });
  });

  it('uploadProblem_HourlyLimit_SaysWhenToTryAgain', () => {
    const headers = new HttpHeaders({ 'Retry-After': '700' });

    expect(uploadProblem(failure(429, null, headers))).toEqual({
      kind: 'limit',
      message: 'You’ve uploaded 5 CVs in the last hour. Try again in about 12 minutes.',
    });
  });

  it('uploadProblem_HourlyLimitWithoutRetryAfter_SaysLater', () => {
    expect(uploadProblem(failure(429))).toEqual({
      kind: 'limit',
      message: 'You’ve uploaded 5 CVs in the last hour. Try again later.',
    });
  });

  it('uploadProblem_ServerDown_IsUnavailable', () => {
    expect(uploadProblem(failure(0))).toEqual({ kind: 'unavailable' });
    expect(uploadProblem(failure(500))).toEqual({ kind: 'unavailable' });
  });

  it('storedTextParts_Markers_AreSeparated', () => {
    expect(storedTextParts('Ada\n[email] | [phone]')).toEqual([
      { text: 'Ada\n', marker: false },
      { text: '[email]', marker: true },
      { text: ' | ', marker: false },
      { text: '[phone]', marker: true },
    ]);
  });
});
