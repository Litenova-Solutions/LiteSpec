<script lang="ts">
  import { book } from "$lib/book";
  import type { BookRecord, Fields } from "$lib/book/types";
  import Icon from "$lib/components/book/Icon.svelte";
  import Prose from "$lib/components/book/Prose.svelte";
  import Block from "../blocks/Block.svelte";
  import Callout from "../blocks/Callout.svelte";
  import FactBadge from "../blocks/FactBadge.svelte";
  import { proseOf } from "../helpers";

  // A decision: the choice, the options weighed, and when it reopens.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const f = $derived(r.fields ?? {});
  const choice = $derived(f.choice ? String(f.choice) : "");
  const opts = $derived(((f.options as Fields[] | undefined) ?? []) as Fields[]);
  const consequences = $derived(proseOf(r, "consequences"));
</script>

{#if choice}
  <Callout kind="info" icon="passing">
    <b>Choice:</b>{" "}
    {choice}{f.approvedOn ? " on " + book.fmtDate(String(f.approvedOn)) : ""}
  </Callout>
{:else}
  <Callout kind="warn" icon="clock">
    <b>Not decided yet.</b>{" "}
    {r.decider || "The decider"}{r.decideBy
      ? " decides by " + book.fmtDate(String(r.decideBy)) + " (" + book.dateHint(String(r.decideBy)) + ")."
      : "."}
  </Callout>
{/if}
{#if consequences}
  <Block blockKey="cons" heading="consequences"><Prose html={consequences} /></Block>
{/if}
{#if opts.length}
  <Block blockKey="opts" heading="options" count={opts.length}>
    <div class="border-t border-border">
      {#each opts as o}
        {@const chosen = !!choice && String(o.name) === choice}
        <div class="border-b border-border py-2.5 {chosen ? 'font-medium' : ''}">
          <h3 class="m-0 mb-1.5 text-[14.5px] leading-snug font-semibold">
            {String(o.name ?? "")}
            {#if chosen}{" "}<FactBadge text="Choice" />{/if}
          </h3>
          <div class="grid max-w-[72ch] grid-cols-[18px_1fr] gap-x-1.5 gap-y-1 text-[13.5px] leading-snug text-subtle-foreground">
            <span class="text-convention-foreground"><Icon name="plus" class="size-3.5" /></span>
            <span>{String(o.pros ?? "")}</span>
            <span class="text-warn-foreground"><Icon name="minus" class="size-3.5" /></span>
            <span>{String(o.cons ?? "")}</span>
          </div>
        </div>
      {/each}
    </div>
  </Block>
{/if}
{#if f.reopenedWhen}
  <Block blockKey="rev" heading="reopenedWhen">
    <Callout kind="quiet" icon="clock">{String(f.reopenedWhen)}</Callout>
  </Block>
{/if}
