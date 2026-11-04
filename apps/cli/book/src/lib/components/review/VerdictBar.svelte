<script lang="ts">
  import { book, cap, HLABEL, plural, type Verdict } from "$lib/book";
  import Icon from "$lib/components/book/Icon.svelte";
  import { Button } from "$lib/components/ui/button";
  import { Textarea } from "$lib/components/ui/textarea";

  // The verdict bar under the record page: the settled line or the Approve and
  // question buttons with the hint, Next draft, and the note editor.
  const m = $derived.by(() => {
    const sid = book.selected;
    const r = sid ? book.get(sid) : undefined;
    if (!sid || !r || !book.isReal(sid)) return null;
    const hh = book.human(sid);
    const isBeh = book.isBeh(sid);
    const my: Verdict = isBeh ? "mineR" : "mine";
    const open = book.openQs(sid).length;
    const block = open ? "Answer " + plural(open, "open question") + " first" : "";
    const batch = book.batchOf(sid)?.batch ?? null;
    const words = book.riskWords(sid).join(", ");
    const approver = book.hasRisk(sid) ? book.approverFor(book.riskClassesOf(sid)[0], sid) : "";
    let hint = "";
    if (block) hint = block;
    else if (hh === my) hint = cap(HLABEL[my]);
    else if (batch && (batch.n ?? 0) > 1) {
      hint = "Approves the whole batch: " + plural(batch.n ?? 0, "record");
      if (book.hasRisk(sid)) hint += ", risk: " + words + (approver ? ", approved by " + approver : "");
    } else if (book.hasRisk(sid)) {
      hint = "Risk: " + words + (approver ? ", approved by " + approver : "");
    }
    const fields = book.fields(sid);
    const settledText =
      cap(HLABEL[hh] || hh) +
      (fields.approvedBy
        ? " by " + String(fields.approvedBy) + (fields.approvedOn ? " on " + book.fmtDate(String(fields.approvedOn)) : "")
        : "");
    return {
      sid,
      settled: !book.needs(sid) && hh !== my,
      settledText,
      approveLabel: isBeh ? "Review" : "Approve",
      my,
      approveDisabled: hh === my || open > 0,
      block,
      hint,
      next: book.nextNeeding(sid),
      editing: book.noteFor === sid,
      aria: book.noteKind === "question" ? "Your question" : "What should change",
      placeholder: book.noteKind === "question" ? "Your question" : book.noteKind === "answer" ? "Your answer" : "What should change",
    };
  });

  let text = $state("");
  let area: HTMLElement | null = $state(null);

  // A fresh editor starts from the saved note and takes the focus; typing never
  // resets it, since the effect reads no keystroke.
  $effect(() => {
    const nf = book.noteFor;
    void book.noteKind;
    const sid = book.selected;
    text = nf ? (book.notes[nf] ?? "") : "";
    if (nf && nf === sid) area?.focus();
  });

  function goNext() {
    const n = book.nextNeeding(book.selected);
    if (n) void book.select(n);
  }
</script>

{#if m}
  <div class="mx-auto flex w-full max-w-[800px] flex-wrap items-center gap-2 px-6 py-3 md:px-12">
    {#if m.settled}
      <span class="inline-flex items-center gap-2 text-sm font-medium text-convention-foreground">
        <Icon name="approved" class="size-4" />{m.settledText}
      </span>
      <span class="flex-1"></span>
      <Button variant="outline" onclick={() => book.setVerdict("changes")}>Request changes</Button>
      {#if m.next}
        <Button variant="ghost" title={book.title(m.next)} onclick={goNext}>
          Next draft<Icon name="arrow" class="size-4" />
        </Button>
      {/if}
    {:else}
      <Button disabled={m.approveDisabled} title={m.block ? m.block : undefined} onclick={() => book.setVerdict(m.my)}>
        <Icon name="approved" class="size-4" />{m.approveLabel}
      </Button>
      <Button variant="outline" onclick={() => book.setVerdict("changes")}>Request changes</Button>
      <Button variant="outline" onclick={() => book.setVerdict("question")}>Ask a question</Button>
      <span class="text-xs text-muted-foreground">{m.hint}</span>
      <span class="flex-1"></span>
      <Button variant="ghost" disabled={!m.next} title={m.next ? book.title(m.next) : undefined} onclick={goNext}>
        <span>Next draft</span><Icon name="arrow" class="size-4" />
      </Button>
    {/if}
    {#if m.editing}
      <div class="flex w-full gap-2">
        <Textarea bind:ref={area} bind:value={text} aria-label={m.aria} placeholder={m.placeholder} class="min-h-16 flex-1" />
        <div class="flex flex-col gap-1.5">
          <Button onclick={() => book.saveNote(text)}>Save</Button>
          <Button variant="ghost" onclick={() => book.cancelNote()}>Cancel</Button>
        </div>
      </div>
    {/if}
  </div>
{/if}
