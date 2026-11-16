<script lang="ts">
  import { book, esc, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import BulletList from "../blocks/BulletList.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { inlineRefHtml, tdHtml } from "../page-html";
  import { stringField, stringList } from "../page-values";

  // A role: the permissions it allows with the limits on its scope.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const allows = $derived(stringList(r.fields ?? {}, "allows"));
  const limits = $derived(stringList(r.fields ?? {}, "limits"));
  const scope = $derived(stringField(r.fields ?? {}, "scope") ?? "");
</script>

{#if scope}
  <PageSection blockKey="scope" heading="role.scope"><p class="m-0 text-sm">{scope}</p></PageSection>
{/if}
{#if allows.length}
  <PageSection blockKey="allows" heading="allows" count={allows.length}>
    <DataTable
      head={["Permission", "Meaning"]}
      rows={allows.map((p) => [tdHtml(inlineRefHtml(book, p)), tdHtml(esc(String(book.get(stripStep(p))?.fields?.meaning ?? "")))])}
    />
  </PageSection>
{/if}
{#if limits.length}
  <PageSection blockKey="limits" heading="limits" count={limits.length}>
    <BulletList items={limits} />
  </PageSection>
{/if}
