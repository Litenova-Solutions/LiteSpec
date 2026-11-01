<script module>
  export const litespec = { implements: "screen/web.expense-detail" };
</script>

<script>
  import { loadReceipt } from "#lib/modules/spending/reads/list-receipts/reader.ts";

  const receiptPromise = loadReceipt();
</script>

<h1>Expense</h1>
