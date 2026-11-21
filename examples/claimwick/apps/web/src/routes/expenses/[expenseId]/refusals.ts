// Maps a failed expense command to the refusal sentence its use-case record
// states. The API reports a refusal as a problem; the map keys are the
// problem types of refusals.yaml and the refusal codes beside them.
const sentences: Record<string, Record<string, string>> = {
  approve: {
    "https://claimwick.dev/problems/own-expense":
      "You submitted this expense, so another manager must decide it.",
    "spending.own-expense": "You submitted this expense, so another manager must decide it.",
    "https://claimwick.dev/problems/not-submitted":
      "This expense is already decided, so there is nothing to approve.",
    "spending.not-submitted": "This expense is already decided, so there is nothing to approve.",
  },
  refuse: {
    "https://claimwick.dev/problems/not-submitted":
      "This expense is already decided, so there is nothing to refuse.",
    "spending.not-submitted": "This expense is already decided, so there is nothing to refuse.",
  },
  attach: {
    "https://claimwick.dev/problems/expense-decided":
      "This expense is already decided, so it takes no more receipts.",
    "spending.expense-decided": "This expense is already decided, so it takes no more receipts.",
  },
  reimburse: {
    "https://claimwick.dev/problems/not-approved":
      "This expense is not approved yet, so it cannot be reimbursed.",
    "spending.not-approved": "This expense is not approved yet, so it cannot be reimbursed.",
    "https://claimwick.dev/problems/already-reimbursed": "This expense is already reimbursed.",
    "spending.already-reimbursed": "This expense is already reimbursed.",
  },
  erase: {
    "https://claimwick.dev/problems/retention-not-ended":
      "The retention of this expense has not ended, so its personal data stays.",
    "spending.retention-not-ended":
      "The retention of this expense has not ended, so its personal data stays.",
  },
};

export type ExpenseCommand = keyof typeof sentences;

// The refusal sentence for a failed command, or the fallback when the
// failure is not a recorded refusal, such as a lost connection.
export function sentenceFor(action: ExpenseCommand, problem: unknown, fallback: string): string {
  if (typeof problem === "object" && problem !== null) {
    const payload = problem as Record<string, unknown>;
    for (const field of ["type", "code"]) {
      const value = payload[field];
      if (typeof value === "string") {
        const sentence = sentences[action]?.[value];
        if (sentence !== undefined) {
          return sentence;
        }
      }
    }
  }
  return fallback;
}
