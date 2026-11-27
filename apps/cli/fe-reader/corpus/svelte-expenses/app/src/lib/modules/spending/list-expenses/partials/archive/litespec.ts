import type { LiteSpecMark } from "#lib/app/litespec-records.ts";

export const litespec = { implements: "use-case/spending.list-expenses-partial" } as const satisfies LiteSpecMark;
