/**
 * The live counter beside a new password's hint: "19 characters". Counts characters as people see
 * them (an emoji is one), not UTF-16 code units.
 */
export function characterCount(value: string): string {
  const length = [...value].length;
  return `${length} ${length === 1 ? 'character' : 'characters'}`;
}
