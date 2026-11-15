// The kind badge: one short code per record type, read faster than an icon and
// learned from the hover explanation and the About this book table. Each code is
// unique; a type the table does not hold reads with its first two letters.
const CODES: Record<string, string> = {
  product: "P", vision: "V", goal: "G", problem: "Pb", feature: "F", requirement: "R", flow: "Fl",
  actor: "A", role: "Ro", constraint: "C", decision: "D", research: "Rs", departure: "Dp", term: "T",
  module: "M", aggregate: "Ag", "use-case": "U", result: "Rt", scenario: "S", rule: "Ru",
  "domain-event": "E", "integration-event": "IE", "read-model": "RM", reaction: "Re", job: "J",
  notification: "N", setting: "St", permission: "Pm", "entry-point": "EP", screen: "Sc",
  "external-system": "X", limit: "L", runbook: "Rb", code: "Cd", test: "Ts", route: "Pa",
};

/** The badge code for a record type. */
export function kindCode(type: string): string {
  const code = CODES[type];
  if (code) return code;
  const letters = type.replace(/[^a-z]/gi, "");
  return (letters.charAt(0).toUpperCase() + letters.charAt(1).toLowerCase()) || "?";
}

/** Every type the table codes, in table order, for the About this book table. */
export const CODED_TYPES: readonly string[] = Object.keys(CODES);

// One badge color per record layer, as Carbon tag types. The product root reads gray, apart from the layers.
const LAYER_CLASS: Record<string, string> = {
  root: "bg-tag-gray text-tag-gray-foreground",
  intent: "bg-tag-purple text-tag-purple-foreground",
  behavior: "bg-tag-blue text-tag-blue-foreground",
  governance: "bg-tag-warm text-tag-warm-foreground",
  implementation: "bg-tag-cyan text-tag-cyan-foreground",
  proof: "bg-tag-teal text-tag-teal-foreground",
};

/** The color classes for a badge in a record layer; an unknown layer reads gray. */
export function layerBadgeClass(layer: string): string {
  return LAYER_CLASS[layer] ?? "bg-tag-gray text-tag-gray-foreground";
}
