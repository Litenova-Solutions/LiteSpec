<script lang="ts">
  import { book, cn, hoverState } from "$lib/frame";
  import type { BookRecord } from "$lib/frame";
  import { Hover } from "$lib/record";
  import PageSection from "./blocks/PageSection.svelte";
  import { checkLevelText } from "./page-values";

  // What the checks found on this record: each finding with its meaning, its
  // message, and its check level as a state tag.
  type Props = { r: BookRecord };
  let { r }: Props = $props();
  const list = $derived(r.findings ?? []);

  const TAG = "inline-flex h-[18px] items-center rounded-full px-2 text-xs tracking-[0.32px]";

  /** A finding level as a state tag: what the level blocks, or Covered. */
  function tagClass(level: string, covered: boolean): string {
    if (covered) return "bg-tag-teal text-tag-teal-foreground";
    if (level === "G") return "bg-tag-red text-tag-red-foreground";
    if (level === "S") return "bg-tag-warm text-tag-warm-foreground";
    return "bg-tag-cool text-tag-cool-foreground";
  }
</script>

{#if list.length}
  <PageSection blockKey="checks" heading="Findings" count={list.length}>
    {#each list as x, i (i)}
      {@const level = String(x.level ?? x.tier ?? "")}
      {@const label = x.covered ? "Covered" : checkLevelText(x.level ?? x.tier)}
      {@const explainKey = x.covered ? "covered" : level}
      <div id={"finding-" + (i + 1)} class={"finding-anchor scroll-mt-20" + (i > 0 ? " mt-6 border-t border-border-subtle pt-6" : "")}>
        <h3 class="text-sm leading-6 font-semibold">{book.findingMeaning(String(x.code ?? ""))}</h3>
        <p class="mt-2">
          <Hover hover={{ ...hoverState(book, explainKey), title: label }}>
            <button type="button" class={cn(TAG, tagClass(level, !!x.covered))} data-explain={explainKey}>
              {label}
            </button>
          </Hover>
        </p>
        <div class="mt-2 text-sm [&_a]:text-link [&_a]:no-underline [&_a:hover]:underline [&_code]:bg-layer [&_code]:px-1 [&_code]:font-mono [&_code]:text-xs">
          {@html String(x.messageHtml ?? "")}
        </div>
        <p class="mt-2 flex flex-wrap gap-2 font-mono text-xs">
          <code class="bg-layer px-1">{String(x.code ?? "")}</code>
          {#if x.path}
            <code class="bg-layer px-1">{String(x.path)}{x.line ? ":" + String(x.line) : ""}</code>
          {/if}
        </p>
      </div>
    {/each}
  </PageSection>
{/if}
