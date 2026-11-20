import { hasMissingTime, showsBadge, statRows } from './community-display';
import { CommunityNumbers } from './community.models';

const answered: CommunityNumbers = {
  members: 11,
  applications: 14,
  responded: 3,
  responseRatePercent: 21,
  averageDays: 23.5,
  medianDays: 19,
};

const unanswered: CommunityNumbers = {
  members: 3,
  applications: 3,
  responded: 0,
  responseRatePercent: 0,
  averageDays: null,
  medianDays: null,
};

describe('community display', () => {
  it('statRows_TwoGroups_GiveFiveRowsWithACellPerGroup', () => {
    const rows = statRows([unanswered, answered]);

    expect(rows.map((row) => row.label)).toEqual([
      'Members who applied',
      'Applications',
      'Heard back',
      'Average days to first answer',
      'Median days to first answer',
    ]);
    expect(rows[2].cells.map((cell) => cell.text)).toEqual(['0 · 0%', '3 · 21%']);
    expect(rows[2].cells[1].aria).toBe('3, 21 percent');
    expect(rows[3].cells.map((cell) => cell.text)).toEqual(['—', '23.5']);
    expect(rows[3].cells[0].aria).toBe('No answers yet');
    expect(rows[4].cells[1].aria).toBe('19 days');
  });

  it('hasMissingTime_OnlyWhenSomeGroupHasNoAnswers', () => {
    expect(hasMissingTime([answered])).toBe(false);
    expect(hasMissingTime([answered, unanswered])).toBe(true);
  });

  it('showsBadge_OnlyMediumAndHigh', () => {
    expect(showsBadge(null)).toBe(false);
    expect(showsBadge({ level: 'Low', score: 1 })).toBe(false);
    expect(showsBadge({ level: 'Medium', score: 2 })).toBe(true);
    expect(showsBadge({ level: 'High', score: 4 })).toBe(true);
  });
});
