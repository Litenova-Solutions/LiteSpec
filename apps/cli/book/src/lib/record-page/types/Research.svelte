<script lang="ts">
  import { book, inline, safeHref } from "$lib/frame";
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { RecordIcon, RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import BulletList from "../blocks/BulletList.svelte";
  import SourceList from "../SourceList.svelte";
  import { objectList, stringField, stringList } from "../page-values";

  // Research: the conclusion, the findings with their sources, and the sources it rests on.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const conclusionText = $derived(stringField(f, "conclusion"));
  const conclusion = $derived(conclusionText ? inline(book, conclusionText, r.src ?? null) : "");
  const findings = $derived(objectList(f, "findings"));
  const openQuestions = $derived(stringList(f, "openQuestions"));
  const sources = $derived(objectList(f, "sources"));

  function sourceOf(key: string): RecordFields | undefined {
    return sources.find((x) => String(x.name ?? "") === key);
  }

  function sourceHref(key: string): string | null {
    const x = sourceOf(key);
    if (!x || x.path) return null;
    return safeHref(book, x.url);
  }
</script>

{#if conclusion}
  <PageSection blockKey="con" heading="conclusion"><RenderedProse html={conclusion} /></PageSection>
{/if}
{#if findings.length}
  <PageSection blockKey="find" heading="findings" count={findings.length}>
    <ul class="m-0 grid list-none gap-4 p-0">
      {#each findings as o, i (String(o.statement ?? "") + "#" + i)}
        <li>
          <p class="m-0 text-sm">{String(o.statement ?? "")}</p>
          {#if Array.isArray(o.sources) && o.sources.length}
            <p class="mt-1 mb-0 flex flex-wrap items-center gap-2 text-xs text-text-secondary">
              Sources:
              {#each o.sources as key, j (String(key) + "#" + j)}
                {@const href = sourceHref(String(key))}
                {@const x = sourceOf(String(key))}
                {#if href}
                  <a {href} target="_blank" rel="noopener" class="inline-flex items-center gap-1 text-link hover:underline">
                    {String(x?.title ?? key)}<RecordIcon name="ext" class="size-3.5" />
                  </a>
                {:else if x?.path}
                  <code class="bg-layer px-1 font-mono text-xs">{String(x.path)}</code>
                {:else}
                  <code class="bg-layer px-1 font-mono text-xs">{String(key)}</code>
                {/if}
              {/each}
            </p>
          {/if}
        </li>
      {/each}
    </ul>
  </PageSection>
{/if}
{#if openQuestions.length}
  <PageSection blockKey="open" heading="openQuestions" count={openQuestions.length}>
    <BulletList items={openQuestions} />
  </PageSection>
{/if}
<SourceList {sources} />
