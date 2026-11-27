import type { LiteSpecMark } from "#lib/app/litespec-records.ts";

export const litespec = { implements: "use-case/health.record-check-in" } as const satisfies LiteSpecMark;
