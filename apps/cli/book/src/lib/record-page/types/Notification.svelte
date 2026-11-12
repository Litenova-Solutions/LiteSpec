<script lang="ts">
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { esc } from "$lib/frame";
  import { RecordIcon, RecordLink, RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { codeCls, tdHtml } from "../page-html";
  import { proseOf } from "../page-prose";
  import { isRecord, objectList, stringField } from "../page-values";

  // A notification: its parameters, and the claims each message makes.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f: RecordFields = $derived(r.fields ?? {});
  const whatHappens = $derived(proseOf(r, "whatHappens"));
  const rawParams: unknown[] = $derived(Array.isArray(f.params) ? f.params : f.params ? [f.params] : []);
  const stringParams = $derived(rawParams.length > 0 && typeof rawParams[0] === "string");
  const params = $derived(rawParams.filter(isRecord));
  const claims = $derived(objectList(f, "claims"));
</script>

{#if whatHappens}
  <PageSection blockKey="wh" heading="whatHappens"><RenderedProse html={whatHappens} /></PageSection>
{/if}
{#if rawParams.length}
  <PageSection blockKey="params" heading="params" count={rawParams.length}>
    {#if stringParams}
      <div class="flex flex-wrap gap-1">
        {#each rawParams as p, i (i)}<span class={codeCls}>{String(p)}</span>{/each}
      </div>
    {:else}
      <DataTable
        head={["Parameter", "Meaning", "Source"]}
        rows={params.map((p: RecordFields) => [
          tdHtml(esc(stringField(p, "name") ?? ""), "nm"),
          tdHtml(esc(stringField(p, "meaning") ?? "")),
          tdHtml(esc(stringField(p, "source") ?? ""), "mut"),
        ])}
      />
    {/if}
  </PageSection>
{/if}
{#if claims.length}
  <PageSection blockKey="claims" heading="claims" count={claims.length}>
    {#each claims as c, i (i)}
      {@const requirement = stringField(c, "requirement")}
      <div class="border-b border-border py-2.5">
        <div class="flex items-center gap-2 font-medium">
          <RecordIcon name="quote" class="size-3.5" />{stringField(c, "claim") ?? ""}
        </div>
        {#if requirement}
          <p class="mt-1 mb-0 text-sm text-subtle-foreground">Backed by <RecordLink id={requirement} /></p>
        {:else}
          <p class="mt-1 mb-0 text-xs font-medium text-warn-foreground">No requirement backs this claim</p>
        {/if}
      </div>
    {/each}
  </PageSection>
{/if}
