import type { LiteSpecMark } from "#lib/app/litespec-records.ts";

export const litespec = { implements: "use-case/health.list-check-ins" } as const satisfies LiteSpecMark;
