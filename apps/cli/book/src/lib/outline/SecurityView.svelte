<script lang="ts">
  import { book } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
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
  <p class="mx-2 my-2 text-xs text-slate-600">Loading the security items.</p>
{:else if !sgroups.length}
  <p class="mx-2 my-2 text-xs text-slate-600">No security items.</p>
{:else}
  {#each sgroups as g (g.kind)}
    <section aria-label={g.label} class="mb-2">
      <div class="flex h-9 items-center gap-2 px-2 text-sm font-semibold text-slate-900">
        <span class="grid flex-none text-slate-600"><RecordIcon name={g.icon} class="size-3.5" /></span>
        <span class="min-w-0 flex-1 overflow-hidden text-ellipsis whitespace-nowrap">{g.label}</span>
        <span class="flex-none text-xs font-normal text-slate-600 tabular-nums">{g.items.length}</span>
      </div>
      <ul>
        {#each g.items as item, idx (item.id + "|" + idx)}
          <RecordListRow id={item.id} suffix={item.text} />
        {/each}
      </ul>
    </section>
  {/each}
{/if}
