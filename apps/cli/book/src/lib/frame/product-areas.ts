// The Intent product-area grouping in the outline: the group keys of the
// product areas and the lookup of the areas holding a record.

import type { ProductAreas } from "./shard-types";

/** The area id of the features and flows no module reaches. */
export const UNFULFILLED_AREA = "unfulfilled";

/** The outline group key of one product area: the intent layer with the area id. */
export function areaGroupKey(id: string): string {
  return "intent|area:" + id;
}

/** The group keys of the areas holding a record or one of its ancestors. */
export function areaKeysOf(areas: ProductAreas, chain: readonly string[]): string[] {
  const held = (features: readonly string[], flows: readonly string[]): boolean =>
    chain.some((id) => features.includes(id) || flows.includes(id));
  const out: string[] = [];
  for (const mod of areas.modules) {
    if (held(mod.features, mod.flows)) out.push(areaGroupKey(mod.id));
  }
  if (held(areas.unfulfilled.features, areas.unfulfilled.flows)) out.push(areaGroupKey(UNFULFILLED_AREA));
  return out;
}
