// The use-case page model: the results partitioned by outcome, the input and
// output, and the counts the jump links read. The component resolves the
// linked records; the model reads only values.

import type { BookRecord, RecordFields } from "$lib/frame";
import { freshnessNoteHtml, isRecord, objectList, pagingRows, stringField, useCaseFields } from "../page-values";

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
  fields: { name: string; meaning: string }[];
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
    dataCount: uf.changes.length + uf.reads.length,
    effectCount: uf.effects.length,
    hasImpl: (r.impl ?? []).length > 0,
  };
}
