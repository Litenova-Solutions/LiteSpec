import type { LiteSpecMark } from "@/app/litespec-records";
import { listExpensesOptions } from "../api-client/query.gen";

export const litespec = { implements: "screen/web.about" } as const satisfies LiteSpecMark;

export function AboutPage(): unknown {
  return listExpensesOptions();
}
