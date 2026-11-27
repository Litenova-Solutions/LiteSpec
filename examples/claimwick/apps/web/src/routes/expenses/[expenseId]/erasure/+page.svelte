<script module lang="ts">
  import type { LiteSpecMark } from "#lib/app/litespec-records.ts";

  export const litespec = { implements: "screen/claimwick-app.expense-erasure" } as const satisfies LiteSpecMark;
</script>

<script lang="ts">
  import { goto } from "$app/navigation";
  import { page } from "$app/state";
  import { erasePersonalData } from "@claimwick/api";
  import { ListPage } from "@claimwick/ui/floorplans";
  import { sentenceFor } from "../refusals.ts";

  const expenseId = page.params.expenseId ?? "";

  let erasing = $state(false);
  let eraseNotice = $state<string | null>(null);

  async function erase(): Promise<void> {
    erasing = true;
    eraseNotice = null;
    try {
      await erasePersonalData({ throwOnError: true, path: { expenseId } });
      await goto("/expenses");
    } catch (error) {
      eraseNotice = sentenceFor(
        "erase",
        error,
        "The personal data could not be erased. Try again in a moment.",
      );
    } finally {
      erasing = false;
    }
  }
</script>

<ListPage title="Erasure">
  <nav aria-label="Breadcrumb">
    <a href="/expenses">Expenses</a>
    <a href={`/expenses/${expenseId}`}>{expenseId}</a>
    <span aria-current="page">Erasure</span>
  </nav>

  <section aria-label="Personal data erasure">
    <h2>Personal data erasure</h2>
    <p>Erasure removes the personal data of this decided expense. The amount and the receipt stay on file for the tax file. This cannot be undone.</p>
    <button onclick={() => void erase()} disabled={erasing}>
      {erasing ? "Erasing personal data." : "Erase Personal Data"}
    </button>
    {#if eraseNotice}
      <p role="alert">{eraseNotice}</p>
    {/if}
  </section>
</ListPage>
