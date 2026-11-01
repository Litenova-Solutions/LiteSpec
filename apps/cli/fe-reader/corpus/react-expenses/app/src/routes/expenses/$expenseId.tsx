import { createFileRoute } from "@tanstack/react-router";
import type { LiteSpecMark } from "@/app/litespec-records";

export const litespec = { implements: "screen/web.expense-detail" } as const satisfies LiteSpecMark;

export const Route = createFileRoute("/expenses/$expenseId")({
  component: () => null,
});
