<script lang="ts">
  import { book } from "$lib/book";
  import Icon from "$lib/components/book/Icon.svelte";
  import { Separator } from "$lib/components/ui/separator";
  import FileLink from "./FileLink.svelte";
  import { kindWord } from "./rail";

  // The implementation section: the code that carries the record's mark, or a
  // plain statement that none was found.
  let { id }: { id: string | null } = $props();
  const rec = $derived(id ? book.get(id) : undefined);
  const show = $derived(!!rec && !!rec.expect && (!!rec.expect.impl || (rec.impl ?? []).length > 0));
  const list = $derived(rec?.impl ?? []);
  const typeWord = $derived(rec ? book.typeName(rec.type).toLowerCase() : "");
</script>

{#if show}
  <Separator class="h-0.5 bg-foreground" />
  <section aria-label="Implementation" class="mt-3 mb-6">
    <h3 class="mb-2.5 flex items-center gap-2 text-xs font-semibold tracking-[0.06em] uppercase">
      <Icon name="code" class="size-3.5" />
      <button type="button" data-explain="implemented" class="uppercase">Implementation</button>
      <span class="ml-auto font-mono text-[11px] font-normal tracking-normal text-muted-foreground">{list.length}</span>
    </h3>
    {#if !list.length}
      <p class="my-1 flex items-start gap-1.5 text-[13px] text-warn-foreground">
        <Icon name="missing" class="mt-0.5 size-3.5 shrink-0" />
        <span>No code carries the mark of this {typeWord}.</span>
      </p>
    {:else}
      {#each list as x}
        <div class="border-b border-border py-1.5 last:border-b-0">
          <span class="mb-px block text-[11.5px] text-muted-foreground">{kindWord(String(x.kind ?? ""))}</span>
          <FileLink path={String(x.path ?? "")} line={typeof x.line === "number" ? x.line : null} />
        </div>
      {/each}
    {/if}
  </section>
{/if}
