<script lang="ts">
  import { onMount } from "svelte";
  import { book, capitalize, pluralize, stripStep } from "$lib/frame";
  import { RecordIcon } from "$lib/record";
  import { Button } from "$lib/ui/button";
  import { breaksIntentChain, FEATURE_GAP_TEXT, GAP_TEXT, GOAL_GAP_TEXT, keptBySentence } from "./trace-model";

  // The Intent tree: the product down through goals, features, requirements and
  // flows, and through its visions to the decisions and requirements naming them, one level per click. A box opens the level below it; the ext icon
  // opens the record's page. Escape closes the tree and returns focus to the
  // control that opened it.
  type Props = { onClose: () => void };
  let { onClose }: Props = $props();
  let loaded = $state(false);
  let open = $state<Record<string, boolean>>({});
  let headingEl = $state<HTMLHeadingElement | null>(null);
  let opener = $state<HTMLElement | null>(null);

  onMount(() => {
    opener = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    void (async () => {
      await book.ensureLayer("intent");
      await book.ensureRecords(
        book.order.filter((x) => {
          const t = book.typeOf(x);
          return t === "goal" || t === "vision" || t === "feature";
        }),
      );
      const pid = book.productId();
      if (pid && !open[pid]) open[pid] = true;
      loaded = true;
    })();
  });

  // The heading takes focus once the tree renders, so keyboard readers land in it.
  $effect(() => {
    if (loaded) headingEl?.focus();
  });

  function close(): void {
    onClose();
    // Focus returns to the control that opened the tree, or to the Intent
    // tree button when the opener is gone.
    if (opener) opener.focus();
    else document.getElementById("intent-tree-open")?.focus();
  }

  function onKey(e: KeyboardEvent): void {
    if (e.key !== "Escape") return;
    e.stopPropagation();
    e.preventDefault();
    close();
  }

  const pid = $derived(book.productId());

  // The root lists are read once per data revision, not once per rendered box.
  const roots = $derived.by(() => {
    void book.rev;
    return {
      goals: book.order.filter((x) => book.typeOf(x) === "goal"),
      visions: book.order.filter((x) => book.typeOf(x) === "vision"),
      orphans: Object.keys(book.rec).filter((x) => {
        if (book.rec[x]?.type !== "feature") return false;
        const advances = book.rec[x]?.fields?.advances;
        return !(Array.isArray(advances) ? advances : []).length;
      }),
    };
  });

  function tvKids(id: string): string[] {
    void book.rev;
    const t = id.startsWith("tv/") ? id : book.typeOf(id);
    if (t === "product") {
      return [...roots.goals, ...roots.visions, "tv/no-goal"];
    }
    if (t === "tv/no-goal") {
      return roots.orphans;
    }
    if (t === "goal") return book.ins(id, "advances").filter((x) => book.typeOf(x) === "feature");
    if (t === "vision") return [...book.ins(id, "basedOn"), ...book.ins(id, "cites")];
    if (t === "feature") return book.kids(id).filter((x) => book.typeOf(x) === "requirement");
    if (t === "requirement") return book.ins(id, "cites").filter((x) => book.typeOf(x) === "flow");
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
    if (t === "vision") {
      const decisions = kids.filter((x) => book.typeOf(x) === "decision").length;
      const requirements = kids.length - decisions;
      const parts = [
        ...(decisions ? [pluralize(decisions, "decision") + (decisions === 1 ? " is" : " are") + " based on it"] : []),
        ...(requirements ? [pluralize(requirements, "requirement") + (requirements === 1 ? " cites" : " cite") + " it"] : []),
      ];
      return { text: parts.length ? capitalize(parts.join(", ")) : "", gap: false };
    }
    if (t === "product") {
      const goals = kids.filter((x) => book.typeOf(x) === "goal").length;
      const visions = kids.filter((x) => book.typeOf(x) === "vision").length;
      return { text: pluralize(goals, "goal") + (visions ? ", " + pluralize(visions, "vision") : ""), gap: false };
    }
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

<svelte:window onkeydowncapture={onKey} />

{#snippet tvnode(id: string, path: string[])}
  {@const group = id.startsWith("tv/")}
  {@const r = book.get(stripStep(id))}
  {@const kids = path.includes(id) ? [] : tvKids(id)}
  {@const isOpen = !!open[id] && kids.length > 0}
  {@const t = book.typeOf(id)}
  {@const gap = !group && !!r && breaksIntentChain(book, id, kids.length)}
  {@const meta = tvMeta(id, kids)}
  {@const tone = gap ? "border-dashed border-warn-border" : isOpen ? "border-slate-900" : "border-slate-300"}
  <li>
    <!-- A box toggles with click, Enter and Space. It is not a native button because it holds the nested Open link, which a button forbids. -->
    <div
      role="button"
      tabindex="0"
      aria-expanded={isOpen}
      onclick={(e) => onBox(e, id)}
      onkeydown={(e) => onBox(e, id)}
      class="grid w-[270px] flex-none cursor-pointer grid-cols-[18px_minmax(0,1fr)_auto] items-start gap-x-2 rounded-lg border bg-white p-3 text-left text-slate-900 shadow-sm hover:border-slate-900 {tone}"
    >
      <RecordIcon name={group ? "folder" : t} class="mt-0.5 size-3.5" />
      <span class="text-sm leading-snug font-semibold break-words">{group ? "Features that serve no goal" : book.title(id)}</span>
      {#if group}
        <span></span>
      {:else}
        <a
          href={"#" + encodeURIComponent(id)}
          data-go={id}
          title="Open the page"
          aria-label="Open the page"
          class="grid size-6 place-items-center rounded-md text-slate-600 hover:bg-slate-100 hover:text-slate-900"
        >
          <RecordIcon name="ext" class="size-3.5" />
        </a>
      {/if}
      <span class="col-start-2 col-end-4 font-mono text-xs font-medium text-slate-600">{group ? "group" : book.typeTag(t)}</span>
      {#if meta.text}
        <span class="col-start-2 col-end-4 text-xs {meta.gap ? 'font-medium text-warn-foreground' : 'text-slate-600'}">{meta.text}</span>
      {/if}
    </div>
    {#if isOpen}
      <ul class="mt-1 mb-1 ml-3 space-y-2 border-l border-slate-200 py-1 pl-4">
        {#each kids.slice(0, 40) as k (k)}
          {@render tvnode(k, [...path, id])}
        {/each}
        {#if kids.length > 40}
          <li><span class="py-1 text-xs text-slate-600">and {kids.length - 40} more</span></li>
        {/if}
      </ul>
    {/if}
  </li>
{/snippet}

{#if !loaded}
  <p class="p-6 text-sm text-slate-600">Loading the Intent tree.</p>
{:else if !pid}
  <p class="p-6 text-sm text-slate-600">This book holds no records.</p>
{:else}
  <div class="mx-auto w-full max-w-[1100px] px-8 py-8">
    <div class="mb-4 flex flex-col items-start gap-3 border-b-2 border-slate-900 pb-4">
      <div>
        <h1 bind:this={headingEl} tabindex="-1" class="text-2xl font-semibold outline-none">Intent, top down</h1>
      </div>
      <div class="flex gap-2">
        <Button variant="outline" onclick={openTwo}>Open two levels</Button>
        <Button
          variant="outline"
          onclick={() => {
            open = {};
          }}>Collapse</Button
        >
        <Button onclick={close}>Close</Button>
      </div>
    </div>
    <div class="overflow-x-auto pt-2 pb-8">
      <ul class="flex flex-col">
        {@render tvnode(pid, [])}
      </ul>
    </div>
  </div>
{/if}
