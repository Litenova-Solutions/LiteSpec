<script lang="ts">
  import { book } from "$lib/frame";
  import { SourceFileLink, RecordIcon } from "$lib/record";
  import { Separator } from "$lib/ui/separator";
  import { implKey, kindWord } from "./trace-model";

  // The implementation section: the code that carries the record's mark, or a
  // plain statement that none was found.
  type Props = { id: string | null; topRule?: boolean };
  let { id, topRule = true }: Props = $props();
  const rec = $derived(id ? book.get(id) : undefined);
  const show = $derived(!!rec && !!rec.expect && (!!rec.expect.impl || (rec.impl ?? []).length > 0));
  const list = $derived(rec?.impl ?? []);
</script>

{#if show}
  {#if topRule}<Separator class="h-0.5 bg-slate-900" />{/if}
  <section aria-label="Implementation" class={topRule ? "mt-3 mb-8" : "mb-8"}>
    <h3 class="mb-2 flex items-center gap-2 text-xs font-semibold tracking-[0.06em] uppercase">
      <RecordIcon name="code" class="size-3.5" />
      <button type="button" data-explain="coded" class="uppercase">Implementation</button>
      <span class="ml-auto font-mono text-xs font-normal tracking-normal text-slate-600">{list.length}</span>
    </h3>
    {#if !list.length}
      <p class="my-1 flex items-start gap-2 text-sm text-warn-foreground">
        <RecordIcon name="missing" class="mt-0.5 size-3.5 shrink-0" />
        <span>No code carries this mark.</span>
      </p>
    {:else}
      {#each list as x (implKey(x))}
        <div class="border-b border-slate-200 py-2.5 last:border-b-0">
          <span class="mb-px block text-xs text-slate-600">{kindWord(String(x.kind ?? ""))}</span>
          <SourceFileLink path={String(x.path ?? "")} line={typeof x.line === "number" ? x.line : null} />
        </div>
      {/each}
    {/if}
  </section>
{/if}
