// One proof-state order and label: failing first, passing last, unknown last.

import type { ProofState } from "./shard-types";

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
  }
}

/** The words the book prints for a proof state. */
export function proofLabel(state: ProofState): string {
  return state;
}
