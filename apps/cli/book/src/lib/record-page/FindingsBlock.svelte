<script lang="ts">
  import { book } from "$lib/frame";
  import type { BookRecord, RecordFields } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import PageSection from "./blocks/PageSection.svelte";
  import Fact from "./blocks/Fact.svelte";
  import { codeCls } from "./page-html";

  // What the checks found on this record: each finding with its meaning, its
  // message, and its check level.
  let { r }: { r: BookRecord } = $props();
  const list = $derived(((r.findings as RecordFields[] | undefined) ?? []) as RecordFields[]);
  const TL: Record<string, string> = { G: "Blocks a release", S: "Blocks when qualified", A: "Advice" };
</script>

{#if list.length}
  <PageSection blockKey="checks" heading="Findings" count={list.length} aside="what the checks found on this record">
    <div class="border-t border-border">
      {#each list as x}
        {@const level = String(x.level ?? x.tier ?? "")}
        {@const label = x.covered ? "Covered" : (TL[level] ?? "")}
        <div class="grid grid-cols-[18px_minmax(0,1fr)_auto] gap-2 border-b border-border py-2.5 text-[13.5px]">
          <RecordIcon name={x.covered ? "departure" : "alert"} class="mt-1 size-3.5 {x.covered ? 'text-muted-foreground' : 'text-warn-foreground'}" />
          <div class="min-w-0">
            <div class="font-medium text-foreground">{book.findingMeaning(String(x.code ?? ""))}</div>
            <div class="mt-0.5 text-[13px] text-subtle-foreground">
              {@html String(x.messageHtml ?? "")}
            </div>
            <div class="mt-1 flex flex-wrap gap-2">
              <span class="font-mono text-[11.5px] text-subtle-foreground">{String(x.code ?? "")}</span>
              {#if x.path}
                <span class={codeCls}>{String(x.path)}{x.line ? ":" + String(x.line) : ""}</span>
              {/if}
            </div>
          </div>
          <button type="button" class="cursor-help rounded-sm" data-explain={x.covered ? "covered" : level}>
            <Fact text={label} />
          </button>
        </div>
      {/each}
    </div>
  </PageSection>
{/if}
