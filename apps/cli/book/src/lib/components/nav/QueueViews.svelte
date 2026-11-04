<script lang="ts">
  import { book, cap } from "$lib/book";
  import Icon from "$lib/components/book/Icon.svelte";
  import { cn } from "$lib/utils";
  import TreeRow from "./TreeRow.svelte";
  import { ballotRisk, type BallotGroup, type QueueRiskGroup, type SecurityGroup } from "./rows";

  // The review queue, security and ballot lists. They hold at most hundreds of
  // rows, so they render directly instead of through the virtualizer.
  let { view, qgroups, sgroups, bgroups }: { view: "queue" | "security" | "ballots"; qgroups: QueueRiskGroup[]; sgroups: SecurityGroup[]; bgroups: BallotGroup[] } = $props();
  const loading = $derived.by(() => {
    void book.rev;
    return book.queue === null;
  });
</script>

{#if view === "queue"}
  {#if loading}
    <p class="mx-2 my-2 text-xs text-muted-foreground">Loading the review queue.</p>
  {:else}
    <p class="mx-2 my-2 text-xs leading-relaxed text-muted-foreground">
      Batches in the order <span class="inline-block rounded border border-border bg-muted px-1.5 py-[1px] font-mono text-[12px] font-medium whitespace-nowrap text-subtle-foreground">litespec model review</span> prints them, grouped by risk class with the approver beside each group.
      <button type="button" data-explain="batch" class="cursor-help">What is a batch?</button>
    </p>
    {#each qgroups as g (g.key)}
      <div class="flex items-center gap-2 px-1.5 pt-1.5 pb-1 text-[13px] font-semibold">
        <span class="grid flex-none text-muted-foreground"><Icon name="shield" class="size-4" /></span>
        <span class="min-w-0">Risk: {g.key}{g.approver ? `, approved by ${g.approver}` : ""}</span>
        <span class="ml-auto flex-none font-mono text-[11px] font-normal text-muted-foreground">{g.batches.length}</span>
      </div>
      {#each g.batches as b (b.key)}
        <TreeRow id={b.id} variant="queue" sub={b.sub} icon={b.icon} tail={false} titleText={b.title} />
      {/each}
    {/each}
  {/if}
{:else if view === "security"}
  {#if loading}
    <p class="mx-2 my-2 text-xs text-muted-foreground">Loading the security items.</p>
  {:else}
    <p class="mx-2 my-2 text-xs leading-relaxed text-muted-foreground">For the security reviewer, {book.securityReviewer()}. Security review stands apart from reviewed status.</p>
    {#each sgroups as g (g.kind)}
      <div class="mx-1.5 mt-3 mb-1.5 flex items-center gap-2 text-[13px] font-semibold">
        <span class="grid flex-none text-muted-foreground"><Icon name={g.icon} class="size-4" /></span>
        <span>{g.label}</span>
        <span class="ml-auto font-mono text-[11px] font-normal text-muted-foreground">{g.items.length}</span>
      </div>
      <p class="mx-1.5 mb-2 text-xs text-muted-foreground">{g.text}</p>
      {#each g.items as item (item.kind + "|" + item.id + "|" + item.text)}
        <TreeRow id={item.id} variant="security" sub={item.text} />
      {/each}
    {/each}
  {/if}
{:else if loading}
  <p class="mx-2 my-2 text-xs text-muted-foreground">Loading the ballots.</p>
{:else if !bgroups.length}
  <p class="mx-2 my-2 text-xs text-muted-foreground">No open question matches.</p>
{:else}
  {#each bgroups as g (g.key)}
    <section>
      <div class="flex items-center gap-2 px-1.5 pt-1.5 pb-1 text-[13px] font-semibold">
        <span class="grid flex-none text-muted-foreground"><Icon name="shield" class="size-4" /></span>
        <span class="min-w-0">Risk: {g.key}{g.approver ? `, approved by ${g.approver}` : ""}</span>
        <span class="ml-auto flex-none font-mono text-[11px] font-normal text-muted-foreground">{g.total}</span>
      </div>
      {#each g.ballots as bal (g.key + "|" + bal.key)}
        <section>
          <div class="flex items-center gap-2 px-1.5 pt-1 pb-1 text-[13px] font-semibold">
            <span class="grid flex-none text-muted-foreground"><Icon name="ballot" class="size-4" /></span>
            <span class="min-w-0">{bal.label}</span>
            <span class="ml-auto flex-none font-mono text-[11px] font-normal text-muted-foreground">{bal.items.length}</span>
          </div>
          {#each bal.items as x (x.id + "#" + x.i)}
            {@const q = x.q}
            {@const risk = ballotRisk(book, x)}
            {@const done = book.answered[x.id + "#" + x.i]}
            {@const auto = !risk.length && q.recommendation && q.decideBy ? book.addDays(q.decideBy, book.vetoDays) : null}
            {@const sel = book.selected === x.id && book.focusQ === x.i}
            {@const approver = risk.length ? book.approverFor(risk[0], x.id) : ""}
            <button
              type="button"
              data-rk="{x.id}|{x.i}"
              onclick={() => book.select(x.id, { q: x.i })}
              class={cn("mb-1.5 block w-full cursor-pointer rounded-md border bg-background px-2.5 py-2 text-left", sel ? "border-highlight bg-highlight-subtle" : "border-border hover:bg-muted")}
            >
              <span class="line-clamp-3 block text-[13px] leading-[1.45] text-foreground">{q.question}</span>
              <span class="mt-1.5 flex flex-wrap items-center gap-x-2.5 gap-y-1.5 text-[11.5px] text-muted-foreground">
                <span>{risk.length ? "Risk: " + risk.map((v) => cap(String(v).replace(/-/g, " "))).join(", ") + (approver ? ", approved by " + approver : "") : "No risk"}</span>
                {#if q.decider}<span>{q.decider}</span>{/if}
                {#if q.decideBy}<span class={cn("font-mono text-[11px]", (book.days(q.decideBy) ?? 0) < 0 && "text-warn-foreground")}>by {book.fmtDate(q.decideBy)}</span>{/if}
                {#if auto}<span>auto {book.fmtDate(auto)}</span>{/if}
                {#if done}<span class="inline-flex items-center gap-1.5 font-medium text-convention-foreground"><Icon name="approved" class="size-3.5" />Answered</span>{/if}
                <span class="inline-flex items-center gap-1"><Icon name={book.typeOf(x.id)} class="size-3.5" /> {book.title(x.id)}</span>
              </span>
            </button>
          {/each}
        </section>
      {/each}
    </section>
  {/each}
{/if}
