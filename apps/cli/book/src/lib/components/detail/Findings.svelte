<script lang="ts">
  import { book } from "$lib/book";
  import type { BookRecord, Fields } from "$lib/book/types";
  import { inline } from "$lib/book/html";
  import Icon from "$lib/components/book/Icon.svelte";
  import Block from "./blocks/Block.svelte";
  import FactBadge from "./blocks/FactBadge.svelte";
  import { codeCls } from "./helpers";

  // What the checks found on this record: each finding with its meaning, its
  // message, and its check level.
  let { r }: { r: BookRecord } = $props();
  const list = $derived(((r.findings as Fields[] | undefined) ?? []) as Fields[]);
  const TL: Record<string, string> = { G: "Blocks a release", S: "Blocks once approved", A: "Advice" };
</script>

{#if list.length}
  <Block blockKey="checks" heading="Findings" count={list.length} aside="what the checks found on this record">
    <div class="border-t border-border">
      {#each list as x}
        {@const level = String(x.level ?? x.tier ?? "")}
        {@const label = x.known ? "Excepted" : (TL[level] ?? "")}
        <div class="grid grid-cols-[18px_minmax(0,1fr)_auto] gap-2 border-b border-border py-2.5 text-[13.5px]">
          <Icon name={x.known ? "exception" : "alert"} class="mt-1 size-3.5 {x.known ? 'text-muted-foreground' : 'text-warn-foreground'}" />
          <div class="min-w-0">
            <div class="font-medium text-foreground">{book.findingMeaning(String(x.code ?? ""))}</div>
            <div class="mt-0.5 text-[13px] text-subtle-foreground">
              {@html inline(book, String(x.message ?? "").replace(/'([a-z-]+\/[^']+)'/g, "`$1`"), r.src ?? null)}
            </div>
            <div class="mt-1 flex flex-wrap gap-2">
              <span class="font-mono text-[11.5px] text-subtle-foreground">{String(x.code ?? "")}</span>
              {#if x.path}
                <span class={codeCls}>{String(x.path)}{x.line ? ":" + String(x.line) : ""}</span>
              {/if}
            </div>
          </div>
          <button type="button" class="cursor-help rounded-sm" data-explain={x.known ? "known" : level}>
            <FactBadge text={label} />
          </button>
        </div>
      {/each}
    </div>
  </Block>
{/if}
