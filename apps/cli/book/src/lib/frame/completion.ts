// Completion and proof readers: the lines the completion block draws, the Intent
// coverage percent, and the proof, lacks and completion judgements the store shows.

import type { BookRecord, ProofState } from "./shard-types";
import { isScenarioType } from "./shard-types";

/** Whether a completion line expects nothing, has what it expects, or misses it. */
export type CompletionMark = "none-expected" | "present" | "missing";

/** One completion line: its words and whether it is present, missing or not expected. */
export type CompletionLine = { label: string; mark: CompletionMark; detail?: string };

/** The readers the completion judgements need from the store. */
export interface CompletionReaders {
  get(id: string): BookRecord | undefined;
  claimed(layer: string): boolean;
  scenariosOf(id: string): string[];
}

/** The stage line the completion block draws: the stage with its scope counts, and the parked count when any child is parked. */
export function stageLine(completion: BookRecord["completion"]): string | null {
  if (completion === null || completion === undefined) return null;
  const stage = completion.stage;
  const inScope = completion.inScope;
  const total = completion.total;
  const parked = completion.parked;
  if (stage === undefined || inScope === undefined || total === undefined || parked === undefined) return null;
  const parkedSuffix = parked > 0 ? "; " + parked + " parked" : "";
  return stage + ", " + inScope + " of " + total + parkedSuffix;
}

/** A stale proof as the book shows it: when its run commit passed and how many commits behind it sits. */
export function freshnessLine(proof: BookRecord["proof"]): string | null {
  if (proof === null || proof === undefined) return null;
  if (proof.status !== "stale") return null;
  const at = proof.lastPassedAt ?? "";
  const behind = proof.commitsBehind ?? 0;
  if (at === "") return null;
  const noun = behind === 1 ? "commit ago" : "commits ago";
  return "last passed at " + at + ", " + behind + " " + noun;
}

/** The lines the completion block draws: code, tests and scenarios, each marked. */
export function completionLines(completion: BookRecord["completion"]): CompletionLine[] {
  if (completion === null || completion === undefined) {
    return [
      { label: "Has code", mark: "none-expected" },
      { label: "Has tests", mark: "none-expected" },
      { label: "Scenarios", mark: "none-expected" },
    ];
  }
  const lines: CompletionLine[] = [];
  const hasCode = completion.hasCode;
  if (hasCode === null || hasCode === undefined) {
    lines.push({ label: "Has code", mark: "none-expected" });
  } else if (hasCode) {
    lines.push({ label: "Has code", mark: "present" });
  } else {
    lines.push({ label: "No code", mark: "missing" });
  }
  const hasTests = completion.hasTests;
  if (hasTests === null || hasTests === undefined) {
    lines.push({ label: "Has tests", mark: "none-expected" });
  } else if (hasTests) {
    lines.push({ label: "Has tests", mark: "present" });
  } else {
    lines.push({ label: "No tests", mark: "missing" });
  }
  const scenarios = completion.scenarios;
  if (scenarios === null || scenarios === undefined) {
    lines.push({ label: "Scenarios", mark: "none-expected" });
  } else {
    const pass = scenarios.pass ?? 0;
    const of = scenarios.of ?? 0;
    const all = of > 0 && pass >= of;
    const noun = of === 1 ? "scenario passes" : "scenarios pass";
    lines.push({ label: pass + " of " + of + " " + noun, mark: all ? "present" : "missing" });
  }
  return lines;
}

/** The Intent coverage percent: the share when the model carries it, else reached over total. */
export function coveragePercent(coverage: { share?: number | null; reached: number; total: number }): number {
  if (coverage.total <= 0) return 100;
  const share = coverage.share ?? coverage.reached / coverage.total;
  return Math.round(share * 100);
}

/** Whether a record is complete: it has the code and tests it expects, and its scenarios pass. */
export function isComplete(completion: BookRecord["completion"]): boolean {
  if (completion === null || completion === undefined) return true;
  if (completion.hasCode === false || completion.hasTests === false) return false;
  const scenarios = completion.scenarios;
  if (scenarios !== null && scenarios !== undefined && (scenarios.pass ?? 0) < (scenarios.of ?? 0)) return false;
  return true;
}

/** A record's proof state from its proof detail, or from its scenarios when it is a flow. */
export function proofStateFor(readers: CompletionReaders, id: string): ProofState | null {
  if (readers.claimed("proof") === false) return null;
  const record = readers.get(id);
  if (record === undefined) return null;
  if (record.proof) return record.proof.state ?? null;
  if (isScenarioType(record.type)) {
    const states: ProofState[] = [];
    for (const scenario of readers.scenariosOf(id)) {
      const state = readers.get(scenario)?.proof?.state;
      if (state !== null && state !== undefined) states.push(state);
    }
    if (states.includes("failing")) return "failing";
    if (record.machine?.verified) return "passing";
    if (record.machine && record.machine.coded === false) return "unproved";
    return states.length > 0 ? "awaiting" : "unproved";
  }
  if (record.type === "requirement" && record.machine) return record.machine.met ? "passing" : "awaiting";
  return null;
}

/** What a record still lacks in the layers the project claims, as words. */
export function lacksFor(readers: Pick<CompletionReaders, "claimed" | "get">, id: string): string {
  const record = readers.get(id);
  if (record === undefined) return "";
  const items = record.lacks ?? [];
  return items.filter((item) => readers.claimed(item === "not coded" ? "implementation" : "proof")).join(", ");
}

/** Whether a record's proof state is failing. */
export function hasFailingProofFor(readers: CompletionReaders, id: string): boolean {
  return proofStateFor(readers, id) === "failing";
}

/** How many of the records are complete, with the percent complete. */
export function completionSummaryFor(
  completeOf: (id: string) => boolean,
  ids: string[],
): { done: number; total: number; percent: number } {
  const done = ids.filter((id) => completeOf(id)).length;
  const total = ids.length;
  const percent = total === 0 ? 100 : Math.round((done / total) * 100);
  return { done, total, percent };
}
