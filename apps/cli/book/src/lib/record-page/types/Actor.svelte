<script lang="ts">
  import { book, capitalize, esc } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import BulletList from "../blocks/BulletList.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { inlineRefHtml, tdHtml } from "../page-html";
  import { stringField, stringList } from "../page-values";

  // An actor: what it is responsible for, what it knows, and the roles it holds.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const responsibilities = $derived(stringList(r.fields ?? {}, "responsibilities"));
  const knowledge = $derived(stringList(r.fields ?? {}, "knowledge"));
  const holds = $derived(stringList(r.fields ?? {}, "holds"));
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
