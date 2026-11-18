import { application } from '../applications.testing';
import { lastActivityOn, timelineRows } from './application-timeline';

describe('timelineRows', () => {
  it('timelineRows_StatusEvents_NewestFirstWithStatusTitlesAndDescriptions', () => {
    const rows = timelineRows(application());

    expect(rows.map((row) => [row.date, row.title, row.description])).toEqual([
      ['2026-11-14', 'Interview', 'Recruiter call, 30 minutes'],
      ['2026-11-03', 'Applied', 'Sent via LinkedIn Easy Apply'],
    ]);
    expect(rows[0].titleClass).toBe('text-status-interview');
    expect(rows[1].titleClass).toBe('text-ink');
  });

  it('timelineRows_NoteOnlyEvent_IsANoteRowInInk', () => {
    const rows = timelineRows(
      application({
        events: [
          {
            id: 'n',
            status: null,
            occurredOn: '2026-11-20',
            note: 'Followed up',
            isSubmission: false,
          },
          { id: 'e1', status: 'Applied', occurredOn: '2026-11-03', note: null, isSubmission: true },
        ],
      }),
    );

    expect(rows[0]).toMatchObject({
      title: 'Note',
      tone: 'note',
      glyph: 'note',
      titleClass: 'text-ink',
      glyphClass: 'text-ink-2',
      description: 'Followed up',
    });
  });

  it('timelineRows_StatusChangeWithoutNote_HasNoDescription', () => {
    const rows = timelineRows(
      application({
        events: [
          {
            id: 'r',
            status: 'Rejected',
            occurredOn: '2026-11-20',
            note: null,
            isSubmission: false,
          },
          { id: 'e1', status: 'Applied', occurredOn: '2026-11-03', note: null, isSubmission: true },
        ],
      }),
    );

    expect(rows[0]).toMatchObject({ title: 'Rejected', tone: 'danger', description: null });
  });

  it('timelineRows_NoResponse_AddsTheComputedRowOnTheDayItStarted', () => {
    const rows = timelineRows(
      application({
        status: 'Applied',
        noResponseDays: 20,
        noResponseSince: '2026-11-17',
        events: [
          { id: 'e1', status: 'Applied', occurredOn: '2026-11-03', note: null, isSubmission: true },
        ],
      }),
    );

    expect(rows.map((row) => row.title)).toEqual(['No response · 14 days', 'Applied']);
    expect(rows[0]).toMatchObject({
      date: '2026-11-17',
      tone: 'no-response',
      description: 'Marked automatically after 14 days',
    });
  });

  it('timelineRows_NoResponseOlderThanALaterNote_SitsBetweenTheEvents', () => {
    const rows = timelineRows(
      application({
        status: 'Applied',
        noResponseDays: 1,
        noResponseSince: '2026-11-17',
        events: [
          {
            id: 'n',
            status: null,
            occurredOn: '2026-11-03',
            note: 'Same day',
            isSubmission: false,
          },
          { id: 'e1', status: 'Applied', occurredOn: '2026-11-03', note: null, isSubmission: true },
        ],
      }),
    );

    expect(rows.map((row) => row.key)).toEqual(['no-response', 'n', 'e1']);
  });

  it('lastActivityOn_SeveralEvents_ReturnsTheLatestDate', () => {
    expect(lastActivityOn(application())).toBe('2026-11-14');
  });
});
