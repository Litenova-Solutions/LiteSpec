<script lang="ts">
  import { book, formatCount } from "$lib/frame";
  import RecordListRow from "./RecordListRow.svelte";
  import type { SecurityGroup } from "./outline-rows";

  // The Security view: cross-tenant access, outside calls and personal or
  // sensitive data, grouped by kind. Each heading keeps its count in a
  // tooltip; the view shows none.
  type Props = { sgroups: SecurityGroup[] };
  let { sgroups }: Props = $props();
  const loaded = $derived.by(() => {
    void book.rev;
    return book.security !== null;
  });
</script>

{#if !loaded}
  <p class="px-3 py-2 text-xs text-text-helper">Loading the security items.</p>
{:else if !sgroups.length}
  <p class="px-3 py-2 text-xs text-text-helper">No security items.</p>
{:else}
  {#each sgroups as g (g.kind)}
    <section aria-label={g.label} class="mb-2">
      <div
        class="flex h-7 items-center gap-2 px-3 text-xs font-semibold text-text-secondary"
        title="{g.label}, {formatCount(g.items.length)} {g.items.length === 1 ? 'security item' : 'security items'}"
      >
        <span class="min-w-0 flex-1 truncate">{g.label}</span>
      </div>
      <ul>
        {#each g.items as item, idx (item.id + "|" + idx)}
          <RecordListRow id={item.id} suffix={item.text} />
        {/each}
      </ul>
    </section>
  {/each}
{/if}
