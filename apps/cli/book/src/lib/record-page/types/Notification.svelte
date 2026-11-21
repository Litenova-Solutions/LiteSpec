<script lang="ts">
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { esc } from "$lib/frame";
  import { RecordLink, RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { codeOf, mutedInline, td, thRow } from "../shared/record-cells";
  import { proseOf } from "../page-prose";
  import { isRecord, objectList, stringField } from "../page-values";

  // A notification: its parameters, and the claims each message makes.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f: RecordFields = $derived(r.fields ?? {});
  const whatHappens = $derived(proseOf(r, "whatHappens"));
  const rawParams: unknown[] = $derived(Array.isArray(f.parameters) ? f.parameters : f.parameters ? [f.parameters] : []);
  const stringParams = $derived(rawParams.length > 0 && typeof rawParams[0] === "string");
  const params = $derived(rawParams.filter(isRecord));
  const claims = $derived(objectList(f, "claims"));
</script>

{#if whatHappens}
  <PageSection blockKey="wh" heading="whatHappens"><RenderedProse html={whatHappens} /></PageSection>
{/if}
{#if rawParams.length}
  <PageSection blockKey="params" heading="parameters" count={rawParams.length}>
    {#if stringParams}
      <div class="flex flex-wrap gap-2">
        {#each rawParams as p, i (i)}<code class="bg-layer px-1 font-mono text-xs">{String(p)}</code>{/each}
      </div>
    {:else}
      <DataTable
        head={["Parameter", "Meaning", "Source"]}
        caption={`Parameters of ${r.title}`}
        rows={params.map((p: RecordFields) => [
          thRow(codeOf(stringField(p, "name") ?? "")),
          td(esc(stringField(p, "meaning") ?? "")),
          td(mutedInline(esc(stringField(p, "source") ?? ""))),
        ])}
      />
    {/if}
  </PageSection>
{/if}
{#if claims.length}
  <PageSection blockKey="claims" heading="claims" count={claims.length}>
    <ul class="grid gap-2">
      {#each claims as c, i (i)}
        {@const requirement = stringField(c, "requirement")}
        <li>
          {stringField(c, "claim") ?? ""}
          {#if requirement}
            Backed by <RecordLink id={requirement} />
          {:else}
            <span class="text-sm font-medium text-warning-ink">No requirement backs this claim</span>
          {/if}
        </li>
      {/each}
    </ul>
  </PageSection>
{/if}
