<script module lang="ts">
  export const litespec = { implements: "screen/web.expense-list" } as const;
</script>

<script lang="ts">
  import { listExpenses } from "../../api-client/sdk.gen";
  import { createClient, buildUrl } from "../../api-client/client/client.gen";
  import { createConfig } from "../../api-client/core/core.gen";

  const client = createClient(createConfig());
  const url = buildUrl("/expenses");
  const rows = listExpenses();
</script>

<h1>Expenses</h1>
