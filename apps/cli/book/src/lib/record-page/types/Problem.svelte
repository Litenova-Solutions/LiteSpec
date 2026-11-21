<script lang="ts">
  import { book, inline } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import BulletList from "../blocks/BulletList.svelte";
  import { proseOf } from "../page-prose";
  import { stringField, stringList } from "../page-values";

  // A problem: the situation it describes, then what fails, the current workaround, its cost, and when it is resolved.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const situation = $derived(proseOf(r, "situation"));
  const failureCases = $derived(stringList(r.fields ?? {}, "failureCases"));
  const workaround = $derived(stringField(r.fields ?? {}, "workaround") ?? "");
  const cost = $derived(stringField(r.fields ?? {}, "cost") ?? "");
  const resolutionCriteria = $derived(stringList(r.fields ?? {}, "resolutionCriteria"));
  const workaroundHtml = $derived(workaround ? inline(book, workaround, r.src ?? null) : "");
  const costHtml = $derived(cost ? inline(book, cost, r.src ?? null) : "");
</script>

{#if situation}
  <PageSection blockKey="sit" heading="situation"><RenderedProse html={situation} /></PageSection>
{/if}
{#if failureCases.length}
  <PageSection blockKey="fail" heading="failureCases" count={failureCases.length}>
    <BulletList items={failureCases} />
  </PageSection>
{/if}
{#if workaround}
  <PageSection blockKey="work" heading="workaround"><RenderedProse html={workaroundHtml} /></PageSection>
{/if}
{#if cost}
  <PageSection blockKey="cost" heading="cost"><RenderedProse html={costHtml} /></PageSection>
{/if}
{#if resolutionCriteria.length}
  <PageSection blockKey="reso" heading="resolutionCriteria" count={resolutionCriteria.length}>
    <BulletList items={resolutionCriteria} />
  </PageSection>
{/if}
