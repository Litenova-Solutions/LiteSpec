// The kind badge: one short code per record type, read faster than an icon and
// learned from the hover explanation and the About this book table. Each code is
// unique; a type the table does not hold reads with its first two letters.
const CODES: Record<string, string> = {
  product: "P", direction: "Di", goal: "G", problem: "Pb", feature: "F", requirement: "R", flow: "Fl",
  actor: "A", role: "Ro", constraint: "C", decision: "D", research: "Rs", waiver: "Dp", term: "T",
  module: "M", aggregate: "Ag", "use-case": "U", result: "Rt", scenario: "S", rule: "Ru",
  "domain-event": "E", "module-event": "ME", "integration-event": "IE", "read-model": "RM", reaction: "Re", job: "J",
  notification: "N", setting: "St", "reference-data": "RD", permission: "Pm", "entry-point": "EP", screen: "Sc",
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

// One badge color per record type, chosen by what the type is: problems and
// permissions red, goals and results green, decisions and events yellow, features
// and aggregates blue. Two types may share a hue; their codes tell them apart.
// Each class is written whole so the stylesheet build finds it.
const COLOR_CLASS: Record<string, string> = {
  gray: "bg-tag-gray text-tag-gray-foreground",
  cool: "bg-tag-cool text-tag-cool-foreground",
  warm: "bg-tag-warm text-tag-warm-foreground",
  blue: "bg-tag-blue text-tag-blue-foreground",
  purple: "bg-tag-purple text-tag-purple-foreground",
  magenta: "bg-tag-magenta text-tag-magenta-foreground",
  red: "bg-tag-red text-tag-red-foreground",
  yellow: "bg-tag-yellow text-tag-yellow-foreground",
  green: "bg-tag-green text-tag-green-foreground",
  teal: "bg-tag-teal text-tag-teal-foreground",
  cyan: "bg-tag-cyan text-tag-cyan-foreground",
};

const TYPE_COLOR: Record<string, string> = {
  product: "gray", term: "gray",
  direction: "magenta", goal: "green", problem: "red", feature: "blue", requirement: "purple", flow: "cyan",
  constraint: "warm", research: "cool", decision: "yellow", actor: "teal", role: "teal",
  module: "cool", aggregate: "blue", rule: "magenta", "use-case": "teal", result: "green", scenario: "cyan",
  "domain-event": "yellow", "module-event": "yellow", "integration-event": "yellow", reaction: "purple", job: "purple",
  notification: "magenta", setting: "warm", "read-model": "cyan", permission: "red", "entry-point": "blue", screen: "blue",
  "external-system": "cool", limit: "warm", runbook: "cyan", waiver: "warm", code: "cool", route: "cool", test: "green",
};

/** The color classes for a record type's badge; a type the table does not hold reads its layer's color. */
export function typeBadgeClass(type: string, layer: string): string {
  const color = TYPE_COLOR[type];
  return (color && COLOR_CLASS[color]) || layerBadgeClass(layer);
}

/** The color classes for a badge in a record layer; an unknown layer reads gray. */
export function layerBadgeClass(layer: string): string {
  return LAYER_CLASS[layer] ?? "bg-tag-gray text-tag-gray-foreground";
}
