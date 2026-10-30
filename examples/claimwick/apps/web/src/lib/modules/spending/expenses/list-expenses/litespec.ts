import type { LiteSpecMark } from "$lib/app/litespec-records";

export const litespec = { implements: "use-case/spending.list-expenses" } as const satisfies LiteSpecMark;
