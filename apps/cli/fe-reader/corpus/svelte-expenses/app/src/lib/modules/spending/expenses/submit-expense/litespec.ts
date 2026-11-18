import type { LiteSpecMark } from "#lib/app/litespec-records.ts";

export const litespec = { implements: "use-case/spending.submit-expense" } as const satisfies LiteSpecMark;
