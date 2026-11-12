// Small text helpers: identifier steps, labels, counts and lists. Every word
// the book prints about a record still comes from the lexicon or the record.

/** An identifier without its #step suffix. */
export const stripStep = (s: unknown): string => String(s ?? "").split("#")[0] ?? "";

/** A word with its first letter uppercased. */
export const capitalize = (s: unknown) => { const t = String(s ?? ""); return t.charAt(0).toUpperCase() + t.slice(1); };

/** A count with its noun: pluralize(2, "day") reads "2 days". */
export const pluralize = (n: number, one: string, many?: string) => n + " " + (n === 1 ? one : many ?? one + "s");

/** A list without its repeats, in first-seen order. */
export const dedupe = <T>(a: T[]) => a.filter((x, i) => a.indexOf(x) === i);

/** A count with grouping: 1200 reads "1,200". */
export const formatCount = (x: unknown) => Number(x ?? 0).toLocaleString("en-GB");

/** A record value written with hyphens as words: "pre-approval" reads "pre approval". */
export function formatLabel(value: unknown): string {
  return String(value ?? "").replace(/-/g, " ");
}
