<script lang="ts">
  import { onMount } from "svelte";
  import { book, capitalize, pluralize, stripStep } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import { Button } from "$lib/ui/button";
  import { breaksIntentChain, FEATURE_GAP_TEXT, GAP_TEXT, GOAL_GAP_TEXT, keptBySentence } from "./trace-model";

  // The Intent tree: the product down through goals, features, requirements and
  // journeys, one level per click. A box opens the level below it; the ext icon
  // opens the record's page.
  type Props = { onClose: () => void };
  let { onClose }: Props = $props();
  let loaded = $state(false);
  let open = $state<Record<string, boolean>>({});

  onMount(() => {
    void (async () => {
      await book.ensureLayer("intent");
      await book.ensureRecords(
        book.order.filter((x) => {
          const t = book.typeOf(x);
          return t === "goal" || t === "feature";
        }),
      );
      const pid = book.productId();
      if (pid && !open[pid]) open[pid] = true;
      loaded = true;
    })();
  });

  const pid = $derived(book.productId());
  const question = $derived(book.layerByKey.intent?.question ?? "");

  function tvKids(id: string): string[] {
    void book.rev;
    const t = id.startsWith("tv/") ? id : book.typeOf(id);
    if (t === "product") return [...book.order.filter((x) => book.typeOf(x) === "goal"), "tv/no-goal"];
    if (t === "tv/no-goal") {
      return Object.keys(book.rec).filter((x) => {
        if (book.rec[x]?.type !== "feature") return false;
        const advances = book.rec[x]?.fields?.advances;
        return !(Array.isArray(advances) ? advances : []).length;
      });
    }
    if (t === "goal") return book.ins(id, "advances").filter((x) => book.typeOf(x) === "feature");
    if (t === "feature") return book.ins(id, "cites");
    if (t === "requirement") return book.ins(id, "cites").filter((x) => book.typeOf(x) === "journey");
    return [];
  }

  function tvMeta(id: string, kids: string[]): { text: string; gap: boolean } {
    if (id === "tv/no-goal") return { text: pluralize(kids.length, "feature"), gap: false };
    const r = book.get(stripStep(id));
    const t = book.typeOf(id);
    if (t === "requirement") {
      if (!r) return { text: "", gap: false };
      const sentence = keptBySentence(book, id);
      return sentence ? { text: capitalize(sentence), gap: false } : { text: GAP_TEXT, gap: true };
    }
    if (t === "goal") {
      return kids.length
        ? { text: pluralize(kids.length, "feature") + (kids.length === 1 ? " serves it" : " serve it"), gap: false }
        : { text: GOAL_GAP_TEXT, gap: true };
    }
    if (t === "feature") {
      return kids.length
        ? { text: pluralize(kids.length, "requirement"), gap: false }
        : { text: FEATURE_GAP_TEXT, gap: true };
    }
    if (t === "product") return { text: pluralize(Math.max(0, kids.length - 1), "goal"), gap: false };
    return { text: "", gap: false };
  }

  function toggle(tid: string): void {
    open[tid] = !open[tid];
    if (!tid.startsWith("tv/") && !book.get(stripStep(tid))) void book.ensureRecords([tid]);
  }

  function openTwo(): void {
    const next: Record<string, boolean> = {};
    const p = book.productId();
    if (p) {
      next[p] = true;
      for (const g of tvKids(p)) next[g] = true;
    }
    open = next;
  }

  function onBox(e: MouseEvent | KeyboardEvent, tid: string): void {
    if ((e.target as Element | null)?.closest?.("[data-go]")) return;
    if (e instanceof KeyboardEvent) {
      if (e.key !== "Enter" && e.key !== " ") return;
      e.preventDefault();
    }
    toggle(tid);
  }
</script>

{#snippet tvnode(id: string, path: string[])}
  {@const group = id.startsWith("tv/")}
  {@const r = book.get(stripStep(id))}
  {@const kids = path.includes(id) ? [] : tvKids(id)}
  {@const isOpen = !!open[id] && kids.length > 0}
  {@const t = book.typeOf(id)}
  {@const gap = !group && !!r && breaksIntentChain(book, id, kids.length)}
  {@const meta = tvMeta(id, kids)}
  {@const tone = gap ? "border-dashed border-warn-border" : isOpen ? "border-foreground" : "border-border"}
  <li>
    <!-- A box toggles with click, Enter and Space. It is not a native button because it holds the nested Open button, which a button forbids. -->
    <div
      role="button"
      tabindex="0"
      aria-expanded={isOpen}
      onclick={(e) => onBox(e, id)}
      onkeydown={(e) => onBox(e, id)}
      class="grid w-[270px] flex-none cursor-pointer grid-cols-[18px_minmax(0,1fr)_auto] items-start gap-x-2 rounded-lg border-[1.5px] bg-background p-2.5 text-left text-foreground hover:border-foreground {tone}"
    >
      <RecordIcon name={group ? "folder" : t} class="mt-0.5 size-3.5" />
      <span class="text-[0.84375rem] leading-snug font-semibold break-words">{group ? "Features that serve no goal" : book.title(id)}</span>
      {#if group}
        <span></span>
      {:else}
        <button
          type="button"
          data-go={id}
          title="Open the page"
          aria-label="Open the page"
          class="grid size-6 place-items-center rounded-sm text-muted-foreground hover:bg-muted hover:text-foreground"
        >
          <RecordIcon name="ext" class="size-3.5" />
        </button>
      {/if}
      <span class="col-start-2 col-end-4 font-mono text-[0.65625rem] font-medium text-muted-foreground">{group ? "group" : book.typeTag(t)}</span>
      {#if meta.text}
        <span class="col-start-2 col-end-4 text-xs {meta.gap ? 'font-medium text-warn-foreground' : 'text-muted-foreground'}">{meta.text}</span>
      {/if}
    </div>
    {#if isOpen}
      <ul class="mt-1 mb-1 ml-3 space-y-2 border-l border-border py-1 pl-4">
        {#each kids.slice(0, 40) as k (k)}
          {@render tvnode(k, [...path, id])}
        {/each}
        {#if kids.length > 40}
          <li><span class="py-1 text-[0.78125rem] text-muted-foreground">and {kids.length - 40} more</span></li>
        {/if}
      </ul>
    {/if}
  </li>
{/snippet}

{#if !loaded}
  <p class="p-6 text-sm text-muted-foreground">Loading the Intent tree.</p>
{:else if !pid}
  <p class="p-6 text-sm text-muted-foreground">This book holds no records.</p>
{:else}
  <div class="mx-auto w-full max-w-[1100px] px-6 py-7 md:px-10">
    <div class="mb-3.5 flex flex-col items-start gap-3 border-b-2 border-foreground pb-4">
      <div>
        <h1 class="text-2xl font-semibold">Intent, top down</h1>
        {#if question}
          <p class="mt-1.5 mb-1.5 text-[0.9375rem] leading-relaxed">{question}</p>
        {/if}
        <div class="mt-1.5 flex flex-wrap gap-x-4 gap-y-1 text-[0.78125rem] text-muted-foreground">
          <span>Click a box to open the level below it.</span>
          <span>A dashed red box is where the chain breaks.</span>
          <span class="inline-flex items-center gap-1"><RecordIcon name="ext" class="size-3.5" /> opens the page.</span>
        </div>
      </div>
      <div class="flex gap-2">
        <Button variant="outline" onclick={openTwo}>Open two levels</Button>
        <Button
          variant="outline"
          onclick={() => {
            open = {};
          }}>Collapse</Button
        >
        <Button onclick={onClose}>Close</Button>
      </div>
    </div>
    <div class="overflow-x-auto py-2.5 pb-10">
      <ul class="flex flex-col">
        {@render tvnode(pid, [])}
      </ul>
    </div>
  </div>
{/if}
