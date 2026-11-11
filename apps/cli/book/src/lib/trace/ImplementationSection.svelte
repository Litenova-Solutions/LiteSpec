<script lang="ts">
  import { book } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import { Separator } from "$lib/ui/separator";
  import EditorFileLink from "./EditorFileLink.svelte";
  import { kindWord } from "./trace-model";

  // The implementation section: the code that carries the record's mark, or a
  // plain statement that none was found.
  type Props = { id: string | null };
  let { id }: Props = $props();
  const rec = $derived(id ? book.get(id) : undefined);
  const show = $derived(!!rec && !!rec.expect && (!!rec.expect.impl || (rec.impl ?? []).length > 0));
  const list = $derived(rec?.impl ?? []);
  const typeWord = $derived(rec ? book.typeName(rec.type).toLowerCase() : "");
</script>

{#if show}
  <Separator class="h-0.5 bg-foreground" />
  <section aria-label="Implementation" class="mt-3 mb-6">
    <h3 class="mb-2.5 flex items-center gap-2 text-xs font-semibold tracking-[0.06em] uppercase">
      <RecordIcon name="code" class="size-3.5" />
      <button type="button" data-explain="coded" class="uppercase">Implementation</button>
      <span class="ml-auto font-mono text-[0.6875rem] font-normal tracking-normal text-muted-foreground">{list.length}</span>
    </h3>
    {#if !list.length}
      <p class="my-1 flex items-start gap-1.5 text-[0.8125rem] text-warn-foreground">
        <RecordIcon name="missing" class="mt-0.5 size-3.5 shrink-0" />
        <span>No code carries the mark of this {typeWord}.</span>
      </p>
    {:else}
      {#each list as x}
        <div class="border-b border-border py-1.5 last:border-b-0">
          <span class="mb-px block text-[0.71875rem] text-muted-foreground">{kindWord(String(x.kind ?? ""))}</span>
          <EditorFileLink path={String(x.path ?? "")} line={typeof x.line === "number" ? x.line : null} />
        </div>
      {/each}
    {/if}
  </section>
{/if}
