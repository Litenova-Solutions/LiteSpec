// Maps a failed expense submission to the refusal sentence its use-case
// record states. The API reports a refusal as a problem; the map keys are
// the problem type of refusals.yaml and the refusal code beside it.
const sentences: Record<string, Record<string, string>> = {
  submit: {
    "https://claimwick.dev/problems/receipt-missing":
      "Attach the receipt for this expense, then submit again.",
    "spending.receipt-missing": "Attach the receipt for this expense, then submit again.",
  },
};

export type ExpenseCommand = keyof typeof sentences;

// The refusal sentence for a failed submission, or the fallback when the
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
