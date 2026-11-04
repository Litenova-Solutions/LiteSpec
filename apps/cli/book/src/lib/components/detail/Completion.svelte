<script lang="ts">
  import { book, cap } from "$lib/book";
  import type { BookRecord } from "$lib/book/types";
  import InlineRef from "$lib/components/book/InlineRef.svelte";
  import { completionOf } from "./helpers";

  // Completion first: the stage, the proof, what holds it, and the counts.
  let { r }: { r: BookRecord } = $props();
  const c = $derived(completionOf(r));
  const proofText = $derived(
    !c ? "" : c.proof === "unknown" ? "proof unknown (no test run in the model)" : c.proof === "known" ? "proof known" : c.proof ? "proof " + c.proof : "",
  );
  const rows = $derived(
    c ? Object.keys(c.counts ?? {}).map((label) => ({ label: cap(label), value: `${c.counts[label]?.done ?? "?"} of ${c.counts[label]?.of ?? "?"}` })) : [],
  );
</script>

{#if c && c.stage}
  <section class="mb-[26px] border-t-2 border-foreground pt-[18px]" aria-label="Completion">
    <h2 class="m-0 mb-2 text-base font-semibold">Completion</h2>
    <p class="m-0 mb-2 text-base font-semibold">{cap(c.stage)}{proofText ? ", " + proofText : ""}</p>
    {#if c.heldBy.length}
      <p class="m-0 mb-2 text-sm text-subtle-foreground">
        Held by:{" "}
        {#each c.heldBy as x, i}
          <span class="whitespace-nowrap">
            {#if String(x).indexOf("#") > 0}
              <InlineRef id={String(x).split("#")[0]} />, step {String(x).split("#")[1].replace(/^s/, "")}
            {:else}
              <InlineRef id={String(x)} />
            {/if}{i < c.heldBy.length - 1 ? "," : ""}
          </span>{" "}
        {/each}
        {#if c.heldByMore}and {c.heldByMore} more{/if}
      </p>
    {/if}
    {#if rows.length}
      <table class="mt-2.5 w-full border-collapse text-[14.5px]">
        <tbody class="[&_tr:last-child_th]:border-b-0 [&_tr:last-child_td]:border-b-0">
          {#each rows as row}
            <tr>
              <th class="w-[180px] border-b border-border py-2 pr-4 text-left align-top font-medium text-muted-foreground">{row.label}</th>
              <td class="border-b border-border py-2 align-top text-foreground">{row.value}</td>
            </tr>
          {/each}
        </tbody>
      </table>
    {/if}
  </section>
{/if}
