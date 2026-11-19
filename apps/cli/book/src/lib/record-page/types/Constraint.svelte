<script lang="ts">
  import { book, inline, safeHref } from "$lib/frame";
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { RecordIcon, RenderedProse } from "$lib/record";
  import PageSection from "../blocks/PageSection.svelte";
  import BulletList from "../blocks/BulletList.svelte";
  import RecordList from "../blocks/RecordList.svelte";
  import SourceList from "../SourceList.svelte";
  import { objectList, stringField, stringList } from "../page-values";

  // A constraint: the authority behind it, its coverage, the rules it states, the effect on the product, and what it requires.
  type Props = { id: string; r: BookRecord };
  let { r }: Props = $props();
  const f = $derived(r.fields ?? {});
  const authority = $derived(stringField(f, "authority") ?? "");
  const coverage = $derived(stringField(f, "coverage") ?? "");
  const coverageHtml = $derived(coverage ? inline(book, coverage, r.src ?? null) : "");
  const obligations = $derived(objectList(f, "obligations"));
  const productEffect = $derived(stringField(f, "productEffect") ?? "");
  const productEffectHtml = $derived(productEffect ? inline(book, productEffect, r.src ?? null) : "");
  const openPoints = $derived(stringList(f, "openPoints"));
  const sources = $derived(objectList(f, "sources"));
  const requires = $derived(r.requires ?? []);

  function sourceOf(key: string): RecordFields | undefined {
    return sources.find((x) => String(x.name ?? "") === key);
  }

  function sourceHref(key: string): string | null {
    const x = sourceOf(key);
    if (!x || x.path) return null;
    return safeHref(book, x.url);
  }
</script>

<PageSection blockKey="src" heading="authority">
  <RenderedProse html={inline(book, authority, r.src ?? null)} />
</PageSection>
{#if coverage}
  <PageSection blockKey="coverage" heading="constraint.coverage"><RenderedProse html={coverageHtml} /></PageSection>
{/if}
{#if obligations.length}
  <PageSection blockKey="obl" heading="obligations" count={obligations.length}>
    <ul class="m-0 grid list-none gap-4 p-0">
      {#each obligations as o, i (String(o.statement ?? "") + "#" + i)}
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
{#if productEffect}
  <PageSection blockKey="prod" heading="productEffect"><RenderedProse html={productEffectHtml} /></PageSection>
{/if}
{#if openPoints.length}
  <PageSection blockKey="open" heading="openPoints" count={openPoints.length}>
    <BulletList items={openPoints} />
  </PageSection>
{/if}
<SourceList {sources} />
{#if requires.length}
  <PageSection blockKey="req" heading="What It Requires" count={requires.length}><RecordList ids={requires} /></PageSection>
{/if}
