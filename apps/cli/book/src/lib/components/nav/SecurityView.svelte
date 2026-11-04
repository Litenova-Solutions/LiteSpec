<script lang="ts">
  import { book } from "$lib/book";
  import Icon from "$lib/components/book/Icon.svelte";
  import TreeRow from "./TreeRow.svelte";
  import type { SecurityGroup } from "./rows";

  // The Security view: cross-tenant access, outside calls and personal or
  // sensitive data, grouped by kind.
  let { sgroups }: { sgroups: SecurityGroup[] } = $props();
  const loaded = $derived.by(() => {
    void book.rev;
    return book.security !== null;
  });
</script>

{#if !loaded}
  <p class="mx-2 my-2 text-xs text-muted-foreground">Loading the security items.</p>
{:else if !sgroups.length}
  <p class="mx-2 my-2 text-xs text-muted-foreground">Nothing here reaches across tenants, calls outside, or holds personal or sensitive data.</p>
{:else}
  {#each sgroups as g (g.kind)}
    <section aria-label={g.label} class="mb-2">
      <div class="flex h-[30px] items-center gap-1.5 px-1.5 text-[11.5px] font-semibold text-subtle-foreground">
        <span class="grid flex-none text-muted-foreground"><Icon name={g.icon} class="size-3.5" /></span>
        <span class="min-w-0 flex-1 overflow-hidden text-ellipsis whitespace-nowrap">{g.label}</span>
        <span class="flex-none font-mono text-[11px] font-normal text-muted-foreground">{g.items.length}</span>
      </div>
      <p class="px-1.5 pb-1 text-[11.5px] text-muted-foreground">{g.text}</p>
      <ul>
        {#each g.items as item (item.kind + "|" + item.id + "|" + item.text)}
          <TreeRow id={item.id} variant="security" sub={item.text} tail={false} />
        {/each}
      </ul>
    </section>
  {/each}
{/if}
