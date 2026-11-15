<script lang="ts">
  import { book, capitalize, esc, formatLabel } from "$lib/frame";
  import type { RecordFields } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { EFFECT_VERBS, effectCondHtml, effectVerbOf, objectList } from "../page-values";
  import { muted, refLink, tag, td, thRow } from "./record-cells";

  // What a behavior does beyond its own record: runs, sends and calls.
  type Props = { f: RecordFields; caption?: string | null };
  let { f, caption = null }: Props = $props();
  const list = $derived(objectList(f, "effects"));
  const rows = $derived(
    list.flatMap((e) => {
      const k = effectVerbOf(e);
      if (!k) return [];
      const tgt = String(e[k]);
      const op = tgt.split("#")[1];
      const cond = effectCondHtml(e);
      const target =
        refLink(book, tgt) +
        (op ? ` <span class="font-mono text-xs text-text-helper">${esc(formatLabel(op))}</span>` : "") +
        (e.timing ? ` ${tag(capitalize(e.timing), "outline")}` : "");
      return [[thRow(EFFECT_VERBS[k].label), td(target), td(cond ? cond : muted("None"))]];
    }),
  );
</script>

{#if list.length}
  <PageSection blockKey="eff" heading="effects" count={list.length}>
    <DataTable head={["Effect", "Target", "Condition"]} {rows} {caption} />
  </PageSection>
{/if}
