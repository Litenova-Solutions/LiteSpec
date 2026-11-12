// One proof-state order and label: failing first, passing last, unknown last.

import type { ProofState } from "./shard-types";
import { capitalize } from "./text";

/** The sort rank of a proof state: failing first, passing last, no proof last. */
export function proofRank(state: ProofState | null): number {
  switch (state) {
    case "failing":
      return 0;
    case "awaiting":
      return 1;
    case "unconfirmed":
      return 2;
    case "norun":
      return 3;
    case "unproved":
      return 4;
    case "indirect":
      return 5;
    case "passing":
      return 6;
    case null:
      return 7;
    default: {
      const exhaustive: never = state;
      return exhaustive;
    }
  }
}

/** The words the book prints for each proof state, in lower case. */
const PROOF_WORDS = {
  passing: "passing",
  failing: "failing",
  awaiting: "awaiting",
  unconfirmed: "unconfirmed",
  unproved: "unproved",
  indirect: "indirect",
  norun: "not run",
} satisfies Record<ProofState, string>;

/** The display label the book prints for a proof state, capitalized. */
export function proofLabel(state: ProofState): string {
  return capitalize(PROOF_WORDS[state]);
}

/** The proof state words in lower case, for a sentence that needs them. */
export function proofWord(state: ProofState): string {
  return PROOF_WORDS[state];
}
