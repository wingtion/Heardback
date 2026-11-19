import { Provider } from '@angular/core';
import { of } from 'rxjs';
import { AiApi } from './ai-api';
import { AiConsent, AiUsage, PostingAnalysis, Tailoring } from './ai.models';

export const TEST_USAGE: AiUsage = {
  used: 2,
  limit: 30,
  remaining: 28,
  resetsAt: '2026-11-20T00:00:00Z',
};

const OFF_ANALYSIS: PostingAnalysis = { state: 'Unavailable', analysis: null, usage: TEST_USAGE };
const OFF_TAILORING: Tailoring = {
  state: 'Unavailable',
  tailoring: null,
  missingKeywords: [],
  usage: TEST_USAGE,
};
const OFF_CONSENT: AiConsent = { allowed: false, allowedAt: null, available: false };

/**
 * An AiApi that says AI is off, for pages whose own tests are about something else: the AI sections then
 * stay hidden and make no further calls.
 */
export function provideAiOff(): Provider {
  return {
    provide: AiApi,
    useValue: {
      postingAnalysis: () => of(OFF_ANALYSIS),
      tailoring: () => of(OFF_TAILORING),
      consent: () => of(OFF_CONSENT),
    },
  };
}
