<script>
  import { loadExpenses } from "$lib/modules/spending/expenses/list-expenses/client";
</script>

<div>
  {#if loadExpenses}
  <span>Broken</span>
</div>
