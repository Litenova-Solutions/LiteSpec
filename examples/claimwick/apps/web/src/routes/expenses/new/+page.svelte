<script module lang="ts">
  import type { LiteSpecMark } from "#lib/app/litespec-records.ts";

  export const litespec = { implements: "screen/claimwick-app.new-expense" } as const satisfies LiteSpecMark;
</script>

<script lang="ts">
  import { goto } from "$app/navigation";
  import { submitExpense } from "@claimwick/api";
  import { ListPage } from "@claimwick/ui/floorplans";
  import { useQueryClient } from "@tanstack/svelte-query";
  import { listExpensesQueryKey } from "#lib/modules/spending/expenses/list-expenses-query.ts";
  import { sentenceFor } from "./refusals.ts";

  const queryClient = useQueryClient();

  let amount = $state("");
  let currency = $state("EUR");
  let files = $state<FileList | undefined>(undefined);
  let submitting = $state(false);
  let refusal = $state<string | null>(null);

  async function submit(): Promise<void> {
    submitting = true;
    refusal = null;
    try {
      const attached = (files?.length ?? 0) > 0;
      const result = await submitExpense({
        throwOnError: true,
        body: { amount, currency, receiptId: attached ? crypto.randomUUID() : null },
      });
      await queryClient.invalidateQueries({ queryKey: listExpensesQueryKey });
      await goto(`/expenses/${result.expenseId}`);
    } catch (error) {
      refusal = sentenceFor("submit", error, "The expense could not be submitted. Try again.");
    } finally {
      submitting = false;
    }
  }
</script>

<ListPage title="New expense">
  <nav aria-label="Breadcrumb">
    <a href="/expenses">Expenses</a>
    <span aria-current="page">New expense</span>
  </nav>
  <p>Expenses above EUR 25.00 need a receipt.</p>

  <form
    aria-label="New expense"
    onsubmit={(event) => {
      event.preventDefault();
      void submit();
    }}
  >
    <label for="amount">Amount</label>
    <input id="amount" name="amount" type="text" required bind:value={amount} />

    <label for="currency">Currency</label>
    <select id="currency" name="currency" bind:value={currency}>
      <option value="EUR">EUR</option>
      <option value="USD">USD</option>
      <option value="GBP">GBP</option>
    </select>

    <label for="receipt">Receipt</label>
    <input
      id="receipt"
      name="receipt"
      type="file"
      accept="application/pdf,image/jpeg,image/png"
      bind:files
    />

    {#if refusal}
      <p role="alert">{refusal}</p>
    {/if}

    <a href="/expenses">Cancel</a>
    <button type="submit" disabled={submitting}>
      {submitting ? "Submitting expense." : "Submit Expense"}
    </button>
  </form>
</ListPage>
