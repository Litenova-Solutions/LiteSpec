<script lang="ts">
  import { base, book, cap } from "$lib/book";
  import type { BookRecord, Fields } from "$lib/book/types";
  import { inline } from "$lib/book/html";
  import Icon from "$lib/components/book/Icon.svelte";
  import { glossary } from "$lib/components/book/glossary";
  import { Button } from "$lib/components/ui/button";
  import Block from "./blocks/Block.svelte";
  import { ballotNameOf } from "./helpers";

  // The open questions on a record: each with its risk, its recommendation, and
  // the buttons to accept it or answer differently.
  let { id, r }: { id: string; r: BookRecord } = $props();
  const qs = $derived(((r.fields?.questions as Fields[] | undefined) ?? []) as Fields[]);

  $effect(() => {
    if (book.focusQ !== null && book.selected === id) {
      document.getElementById("q-" + book.focusQ)?.scrollIntoView({ block: "center" });
    }
  });
</script>

{#if qs.length}
  <Block blockKey="qs" heading="questions" count={qs.length}>
    {#each qs as q, i}
      {@const risk = book.riskClassesOf(id)}
      {@const has = risk.length > 0}
      {@const done = book.answered[base(id) + "#" + i]}
      {@const auto = !has && q.recommendation && q.decideBy ? book.addDays(String(q.decideBy), book.vetoDays) : null}
      {@const nm = q.ballot ? ballotNameOf(String(q.ballot)) : ""}
      {@const approver = has ? book.approverFor(risk[0], id) : ""}
      {@const riskText = has ? "Risk: " + book.riskWords(id).join(", ") + (approver ? ", approved by " + approver : "") : "No risk"}
      {@const qhtml = inline(book, String(q.question ?? ""), r.src ?? null)}
      <div class="mb-2.5 overflow-hidden rounded-lg border border-border {book.focusQ === i ? 'ring-[3px] ring-highlight-soft' : ''}" id={"q-" + i}>
        <div class="flex items-center gap-2 bg-muted px-3.5 py-2.5 text-xs text-subtle-foreground">
          <Icon name={has ? "alert" : "question"} class="size-3.5" />
          <b>{riskText}</b>
          {#if nm}<span>&middot; Ballot: {cap(nm)}</span>{/if}
          <span class="flex-1"></span>
          {#if q.decideBy}
            <span>{String(q.decider ?? "")} by {book.fmtDate(String(q.decideBy))} ({book.dateHint(String(q.decideBy))})</span>
          {/if}
        </div>
        <div class="px-3.5 py-3">
          <p class="m-0 mb-2.5 text-[15px] leading-snug font-medium" use:glossary={qhtml}>{@html qhtml}</p>
          {#if q.recommendation}
            <div class="grid grid-cols-[18px_1fr] gap-2 rounded-md bg-muted px-3 py-2.5 text-sm text-subtle-foreground">
              <Icon name="approved" class="mt-1 size-3.5 text-convention-foreground" />
              <div><b>Recommended.</b> {@html inline(book, String(q.recommendation), r.src ?? null)}</div>
            </div>
          {/if}
        </div>
        <div class="flex flex-wrap items-center gap-2 border-t border-border px-3.5 py-2.5 text-xs text-muted-foreground">
          {#if done}
            <span class="inline-flex items-center gap-1.5 font-medium text-convention-foreground">
              <Icon name="approved" class="size-3.5" />{done === "accept" ? "You accepted the recommendation" : "You answered: " + done}
            </span>
          {:else}
            {#if q.recommendation}
              <Button size="sm" onclick={() => book.answer(i, "accept")}>Accept recommendation</Button>
            {/if}
            <Button variant="outline" size="sm" onclick={() => book.startAnswer(i)}>Answer differently</Button>
          {/if}
          <span class="flex-1"></span>
          {#if auto && !done}
            <span>Auto-approves on {book.fmtDate(auto)}</span>
          {:else if has && !done}
            <span>Needs an explicit answer</span>
          {/if}
        </div>
      </div>
    {/each}
  </Block>
{/if}
