<script lang="ts">
  import { book } from "$lib/frame";
  import RecordListRow from "./RecordListRow.svelte";
  import type { SecurityGroup } from "./outline-rows";

  // The Security view: cross-tenant access, outside calls and personal or
  // sensitive data, grouped by kind with a label and a count each.
  type Props = { sgroups: SecurityGroup[] };
  let { sgroups }: Props = $props();
  const loaded = $derived.by(() => {
    void book.rev;
    return book.security !== null;
  });
</script>

{#if !loaded}
  <p class="px-4 py-2 text-xs text-text-helper">Loading the security items.</p>
{:else if !sgroups.length}
  <p class="px-4 py-2 text-xs text-text-helper">No security items.</p>
{:else}
  {#each sgroups as g (g.kind)}
    <section aria-label={g.label} class="mb-2">
      <div class="flex h-8 items-center gap-2 px-4 text-xs font-semibold text-text-secondary">
        <span class="min-w-0 flex-1 truncate">{g.label}</span>
        <span class="flex-none font-normal tabular-nums">{g.items.length}</span>
      </div>
      <ul>
        {#each g.items as item, idx (item.id + "|" + idx)}
          <RecordListRow id={item.id} suffix={item.text} />
        {/each}
      </ul>
    </section>
  {/each}
{/if}
