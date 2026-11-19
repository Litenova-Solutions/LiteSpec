<script lang="ts">
  import { book, capitalize, dedupe, esc } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import BulletList from "../blocks/BulletList.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import { inlineRefHtml, tdHtml } from "../page-html";
  import { stringField, stringList } from "../page-values";

  // An actor: what it is responsible for, what it knows, the roles it holds,
  // and the outside system standing for it when it is a system actor.
  type Props = { id: string; r: BookRecord };
  let { id, r }: Props = $props();
  const responsibilities = $derived(stringList(r.fields ?? {}, "responsibilities"));
  const knowledge = $derived(stringList(r.fields ?? {}, "knowledge"));
  const holds = $derived(stringList(r.fields ?? {}, "holds"));
  const systems = $derived(dedupe(book.ins(id, "represents").filter((x) => x.startsWith("external-system/"))).sort());
</script>

{#if responsibilities.length}
  <PageSection blockKey="resp" heading="responsibilities" count={responsibilities.length}>
    <BulletList items={responsibilities} />
  </PageSection>
{/if}
{#if knowledge.length}
  <PageSection blockKey="know" heading="knowledge" count={knowledge.length}>
    <BulletList items={knowledge} />
  </PageSection>
{/if}
{#if holds.length}
  <PageSection blockKey="holds" heading="holds" count={holds.length}>
    <DataTable
      head={["Role", "Scope"]}
      rows={holds.map((role) => [tdHtml(inlineRefHtml(book, role)), tdHtml(esc(capitalize(stringField(book.get(role)?.fields, "scope") ?? "")))])}
    />
  </PageSection>
{/if}
{#if systems.length}
  <PageSection blockKey="system" heading="Outside system" count={systems.length}>
    <RecordList ids={systems} />
  </PageSection>
{/if}
