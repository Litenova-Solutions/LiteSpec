import type { LiteSpecMark } from "@/app/litespec-records";

export const litespec = { implements: "screen/web.about" } as const satisfies LiteSpecMark;

export function AboutPage(): null {
  return null;
}
