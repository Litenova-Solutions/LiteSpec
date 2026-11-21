import { createFileRoute } from "@tanstack/react-router";
import { listExpenses } from "../api-client/sdk.gen";
import { createClient, buildUrl } from "../api-client/client/client.gen";
import { createConfig } from "../api-client/core/core.gen";

export const litespec = { implements: "screen/web.expense-list" } as const;

export const Route = createFileRoute("/expenses")({
  loader: async () => {
    const client = createClient(createConfig());
    const url = buildUrl("/expenses");
    void client;
    void url;
    return listExpenses();
  },
  component: () => null,
});
