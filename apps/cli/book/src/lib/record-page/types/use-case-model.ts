// The use-case page model: the results partitioned by outcome, the input and
// output, the states a command moves its aggregates into, and the counts the
// jump links read. The component resolves the linked records; the model reads
// only values.

import { dedupe } from "$lib/frame";
import type { BookRecord, RecordFields } from "$lib/frame";
import { freshnessDecidedBy, freshnessNoteHtml, isRecord, objectList, pagingRows, stringField, useCaseFields } from "../page-values";

/** One result linked from a use case, with its record resolved. */
export interface UseCaseResultInput {
  id: string;
  record: BookRecord;
}

export interface UseCaseResult {
  id: string;
  title: string;
  outcome: string;
  meaning: string;
  code: string | null;
  kind: string;
}

export interface UseCaseOutputSection {
  key: string;
  classification: string;
  freshnessHtml: string;
  decidedBy: string | null;
  fields: { name: string; meaning: string }[];
}

/** One written aggregate with the transitions that move it, resolved by the component. */
export interface WrittenAggregate {
  id: string;
  title: string;
  transitions: RecordFields[];
}

/** One state a command moves a written aggregate into. */
export interface AggregateMove {
  aggregate: string;
  to: string;
}

/**
 * The moves a command makes: for each written aggregate, the distinct states
 * its transitions enter through the command, in record order. Every fact comes
 * from the records.
 */
export function movesOf(movedBy: string, written: WrittenAggregate[]): AggregateMove[] {
  return written.flatMap((w) =>
    dedupe(
      w.transitions
        .filter((t) => String(t.movedBy ?? "") === movedBy)
        .map((t) => String(t.to ?? ""))
        .filter((to) => to !== ""),
    ).map((to) => ({ aggregate: w.title, to })),
  );
}

export interface UseCaseModel {
  results: UseCaseResult[];
  ok: UseCaseResult[];
  refused: UseCaseResult[];
  input: RecordFields[];
  outputSections: UseCaseOutputSection[];
  outputRows: { name: string; meaning: string }[];
  paging: { label: string; html: string }[] | null;
  hasOutput: boolean;
  dataCount: number;
  effectCount: number;
  hasImpl: boolean;
}

export function useCaseModel(f: RecordFields, r: BookRecord, results: UseCaseResultInput[]): UseCaseModel {
  const uf = useCaseFields(f);
  const rows = results.map(({ id, record }) => {
    const qf = record.fields ?? {};
    const code = stringField(qf, "code");
    return {
      id,
      title: record.title,
      outcome: stringField(qf, "outcome") ?? "",
      meaning: stringField(qf, "meaning") ?? "",
      code: code ? code : null,
      kind: /\.(forbidden|invalid-input)$/.test(id) ? "generic" : "",
    };
  });
  const output = uf.output;
  let outputSections: UseCaseOutputSection[] = [];
  let outputRows: { name: string; meaning: string }[] = [];
  if (!Array.isArray(output) && isRecord(output) && isRecord(output.include)) {
    const include = output.include;
    outputSections = Object.keys(include).map((k) => {
      const g: RecordFields = isRecord(include[k]) ? include[k] : {};
      return {
        key: k,
        classification: stringField(g, "classification") ?? "internal",
        freshnessHtml: g.freshness ? freshnessNoteHtml(g) : "",
        decidedBy: freshnessDecidedBy(g) ?? null,
        fields: objectList(g, "fields").map((x) => ({
          name: stringField(x, "name") ?? "",
          meaning: stringField(x, "meaning") ?? "",
        })),
      };
    });
  } else if (Array.isArray(output)) {
    outputRows = output
      .filter(isRecord)
      .map((x) => ({ name: stringField(x, "name") ?? "", meaning: stringField(x, "meaning") ?? "" }));
  }
  const paging = pagingRows(f);
  return {
    results: rows,
    ok: rows.filter((x) => x.outcome === "success"),
    refused: rows.filter((x) => x.outcome !== "success"),
    input: uf.input,
    outputSections,
    outputRows,
    paging,
    hasOutput: outputSections.length > 0 || outputRows.length > 0 || !!paging,
    dataCount: uf.writes.length + uf.reads.length,
    effectCount: uf.effects.length,
    hasImpl: (r.impl ?? []).length > 0,
  };
}
