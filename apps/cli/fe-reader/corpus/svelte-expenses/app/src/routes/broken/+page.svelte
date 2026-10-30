<script>
  import { loadExpenses } from "#lib/modules/spending/expenses/list-expenses/client.ts";
</script>

<div>
  {#if loadExpenses}
  <span>Broken</span>
</div>
