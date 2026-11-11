<script lang="ts">
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { esc } from "$lib/frame";
  import { RecordIcon, RecordLink, RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { codeCls, tdHtml } from "../page-html";
  import { proseOf } from "../page-prose";

  // A notification: its parameters, and the claims each message makes.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const whatHappens = $derived(proseOf(r, "whatHappens"));
  const arr = (v: unknown) => (Array.isArray(v) ? v : v ? [v] : []) as RecordFields[];
  const params = $derived(arr(f.params));
  const claims = $derived(arr(f.claims));
</script>

{#if whatHappens}
  <PageSection blockKey="wh" heading="whatHappens"><RenderedProse html={whatHappens} /></PageSection>
{/if}
{#if params.length}
  <PageSection blockKey="params" heading="params" count={params.length}>
    {#if typeof params[0] === "string"}
      <div class="flex flex-wrap gap-1">
        {#each params as p}<span class={codeCls}>{String(p)}</span>{/each}
      </div>
    {:else}
      <DataTable
        head={["Parameter", "Meaning", "Source"]}
        rows={params.map((p) => [
          tdHtml(esc(String(p.name ?? "")), "nm"),
          tdHtml(esc(String(p.meaning ?? ""))),
          tdHtml(esc(String(p.source ?? "")), "mut"),
        ])}
      />
    {/if}
  </PageSection>
{/if}
{#if claims.length}
  <PageSection blockKey="claims" heading="claims" count={claims.length}>
    {#each claims as c}
      <div class="border-b border-border py-2.5">
        <div class="flex items-center gap-2 font-medium">
          <RecordIcon name="quote" class="size-3.5" />{String(c.claim ?? "")}
        </div>
        {#if c.requirement}
          <p class="mt-1 mb-0 text-sm text-subtle-foreground">Backed by <RecordLink id={String(c.requirement)} /></p>
        {:else}
          <p class="mt-1 mb-0 text-xs font-medium text-warn-foreground">No requirement backs this claim</p>
        {/if}
      </div>
    {/each}
  </PageSection>
{/if}
