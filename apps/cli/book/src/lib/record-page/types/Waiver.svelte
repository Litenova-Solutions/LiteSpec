<script lang="ts">
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { book, capitalize, esc } from "$lib/frame";
  import { RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import Note from "../blocks/Note.svelte";
  import { codeCls, inlineRefHtml, provisionHtml } from "../page-html";
  import { proseOf } from "../page-prose";
  import { stringField, stringList } from "../page-values";

  // A waiver: its kind, exit and consequences, what it sets aside, and why it holds.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f: RecordFields = $derived(r.fields ?? {});
  const kind = $derived(stringField(f, "kind"));
  const exit = $derived(proseOf(r, "exitWhen"));
  const consequences = $derived(proseOf(r, "consequences"));
  const closedBy = $derived(stringList(f, "closedBy"));
  const paths = $derived(stringList(f, "paths"));
  const reason = $derived(stringField(f, "reason"));
  const waives = $derived(
    Array.isArray(f.waives) ? f.waives.map((c) => `<span class="${codeCls}">${esc(c)}</span>`).join(" ") : provisionHtml(f.waives),
  );
</script>


{#if kind}
  <PageSection blockKey="kind" heading="kind">{capitalize(kind)}</PageSection>
{/if}
{#if exit}
  <PageSection blockKey="exit" heading="exitWhen"><RenderedProse html={exit} /></PageSection>
{/if}
{#if consequences}
  <PageSection blockKey="cons" heading="consequences"><RenderedProse html={consequences} /></PageSection>
{/if}
<PageSection blockKey="cov" heading="waives">{@html waives}</PageSection>
{#if closedBy.length}
  <PageSection blockKey="closed" heading="closedBy" count={closedBy.length}>
    <div class="flex flex-wrap gap-1">
      {#each closedBy as c (c)}{@html inlineRefHtml(book, c)}{/each}
    </div>
  </PageSection>
{/if}
{#if paths.length}
  <PageSection blockKey="paths" heading="paths" count={paths.length}>
    <div class="flex flex-wrap gap-1">
      {#each paths as p (p)}<span class={codeCls}>{p}</span>{/each}
    </div>
  </PageSection>
{/if}
{#if reason}
  <PageSection blockKey="why" heading="reason">
    <Note kind="warn" icon="alert">Because {reason}</Note>
  </PageSection>
{/if}
