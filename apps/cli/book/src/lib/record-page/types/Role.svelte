<script lang="ts">
  import { book, dedupe, esc, stripStep } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import PageSection from "../blocks/PageSection.svelte";
  import BulletList from "../blocks/BulletList.svelte";
  import DataTable from "../blocks/DataTable.svelte";
  import { inlineRefHtml, tdHtml } from "../page-html";
  import { stringField, stringList } from "../page-values";

  // A role: the permissions granted to it with what its scope excludes.
  type Props = { id: string; r: BookRecord };
  let { id, r }: Props = $props();
  const granted = $derived(dedupe(book.ins(id, "grantedTo").filter((p) => p.startsWith("permission/"))).sort());
  const excludes = $derived(stringList(r.fields ?? {}, "excludes"));
  const scope = $derived(stringField(r.fields ?? {}, "scope") ?? "");
</script>

{#if scope}
  <PageSection blockKey="scope" heading="role.scope"><p class="m-0 text-sm">{scope}</p></PageSection>
{/if}
{#if granted.length}
  <PageSection blockKey="grantedTo" heading="grantedTo" count={granted.length}>
    <DataTable
      head={["Permission", "Meaning"]}
      rows={granted.map((p) => [tdHtml(inlineRefHtml(book, p)), tdHtml(esc(String(book.get(stripStep(p))?.fields?.meaning ?? "")))])}
    />
  </PageSection>
{/if}
{#if excludes.length}
  <PageSection blockKey="limits" heading="excludes" count={excludes.length}>
    <BulletList items={excludes} />
  </PageSection>
{/if}
