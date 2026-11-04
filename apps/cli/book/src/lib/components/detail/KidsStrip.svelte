<script lang="ts">
  import { book, uniq } from "$lib/book";
  import Icon from "$lib/components/book/Icon.svelte";
  import { Button } from "$lib/components/ui/button";

  // The children of a record grouped by type. book.js defines this strip but
  // never renders it, so the page leaves it unmounted too.
  let { id }: { id: string } = $props();
  const kids = $derived(book.kids(id));
  const types = $derived(uniq(kids.map((x) => book.typeOf(x))));
</script>

{#if kids.length >= 2 && kids.length <= 60}
  <div class="mt-[-8px] mb-[22px] flex flex-wrap gap-1.5">
    {#each types as t}
      {@const n = kids.filter((x) => book.typeOf(x) === t)}
      <Button variant="outline" size="sm" data-go={n[0]}>
        <Icon name={t} class="size-3.5" />{n.length} {(n.length === 1 ? book.typeName(t) : book.pluralName(t)).toLowerCase()}
      </Button>
    {/each}
  </div>
{/if}
