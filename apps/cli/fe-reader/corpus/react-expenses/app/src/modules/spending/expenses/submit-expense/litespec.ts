import type { LiteSpecMark } from "@/app/litespec-records";

export const litespec = { implements: "use-case/spending.submit-expense" } as const satisfies LiteSpecMark;
