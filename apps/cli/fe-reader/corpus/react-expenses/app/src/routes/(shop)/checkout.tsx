import { createFileRoute } from "@tanstack/react-router";
import type { LiteSpecMark } from "@/app/litespec-records";

export const litespec = { implements: "screen/web.checkout" } as const satisfies LiteSpecMark;

export const Route = createFileRoute("/checkout")({
  component: () => null,
});
