<script lang="ts">
  import { onMount } from "svelte";
  import { book, capitalize, pluralize, stripStep } from "$lib/frame";
  import { RecordIcon, TypeTag } from "$lib/record";
  import { FEATURE_GAP_TEXT, GAP_TEXT, GOAL_GAP_TEXT, keptBySentence } from "./trace-model";

  // The Intent tree: the product down through goals, features, requirements and
  // flows, and through its directions to the decisions and requirements naming them, one level per click. A row opens the level below it; the title
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
          return t === "goal" || t === "direction" || t === "feature";
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

  // The root lists are read once per data revision, not once per rendered row.
  const roots = $derived.by(() => {
    void book.rev;
    return {
      goals: book.order.filter((x) => book.typeOf(x) === "goal"),
      directions: book.order.filter((x) => book.typeOf(x) === "direction"),
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
      return [...roots.goals, ...roots.directions, "tv/no-goal"];
    }
    if (t === "tv/no-goal") {
      return roots.orphans;
    }
    if (t === "goal") return book.ins(id, "advances").filter((x) => book.typeOf(x) === "feature");
    if (t === "direction") return [...book.ins(id, "basedOn"), ...book.ins(id, "cites")];
    if (t === "feature") return book.kids(id).filter((x) => book.typeOf(x) === "requirement");
    if (t === "requirement") return book.ins(id, "basedOn").filter((x) => book.typeOf(x) === "flow");
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
    if (t === "direction") {
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
      const directions = kids.filter((x) => book.typeOf(x) === "direction").length;
      return { text: pluralize(goals, "goal") + (directions ? ", " + pluralize(directions, "direction") : ""), gap: false };
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

{#snippet tvnode(id: string, path: string[], depth: number)}
  {@const group = id.startsWith("tv/")}
  {@const kids = path.includes(id) ? [] : tvKids(id)}
  {@const isOpen = !!open[id] && kids.length > 0}
  {@const t = book.typeOf(id)}
  {@const meta = tvMeta(id, kids)}
  <li>
    <!-- A row toggles with click, Enter and Space. It is not a native button because it holds the nested title link, which a button forbids. -->
    <div
      role="button"
      tabindex="0"
      aria-expanded={isOpen}
      onclick={(e) => onBox(e, id)}
      onkeydown={(e) => onBox(e, id)}
      class="flex min-h-10 flex-wrap items-center gap-x-3 gap-y-0.5 border-b border-border-subtle {kids.length ? 'cursor-pointer' : ''}"
    >
      {#if kids.length}
        <RecordIcon name="chev" class="size-4 shrink-0 transition-transform {isOpen ? 'rotate-90' : ''}" />
      {:else}
        <span class="size-4 shrink-0"></span>
      {/if}
      {#if !group}<TypeTag type={t} />{/if}
      {#if group}
        <span class="font-semibold">Features that serve no goal</span>
      {:else}
        <a
          href={"#" + encodeURIComponent(id)}
          data-go={id}
          class="text-link hover:underline {depth < 2 ? 'font-semibold' : ''}"
        >{book.title(id)}</a>
      {/if}
      {#if meta.text}
        {#if meta.gap}
          <span class="inline-flex items-center gap-2 text-xs text-warning-ink">
            <RecordIcon name="alert" class="size-4 shrink-0" />{meta.text}
          </span>
        {:else}
          <span class="text-xs text-text-secondary">{meta.text}</span>
        {/if}
      {/if}
    </div>
    {#if isOpen}
      <ul class="m-0 ml-3 list-none border-l border-border-subtle p-0 pl-5">
        {#each kids.slice(0, 40) as k (k)}
          {@render tvnode(k, [...path, id], depth + 1)}
        {/each}
        {#if kids.length > 40}
          <li><span class="block py-1 text-xs text-text-secondary">and {kids.length - 40} more</span></li>
        {/if}
      </ul>
    {/if}
  </li>
{/snippet}

{#if !loaded}
  <p class="p-6 text-sm text-text-secondary">Loading the Intent tree.</p>
{:else if !pid}
  <p class="p-6 text-sm text-text-secondary">This book holds no records.</p>
{:else}
  <div class="mx-auto w-full max-w-[60rem] px-8 py-8">
    <h1 bind:this={headingEl} tabindex="-1" class="text-[32px] leading-10 font-normal outline-none">Intent, top down</h1>
    <div class="mt-6 flex flex-wrap gap-2">
      <button
        type="button"
        onclick={openTwo}
        class="inline-flex min-h-8 items-center justify-center border border-interactive px-4 text-sm text-link hover:bg-hover-wash"
      >Open two levels</button>
      <button
        type="button"
        onclick={() => {
          open = {};
        }}
        class="inline-flex min-h-8 items-center px-4 text-sm text-link hover:bg-hover-wash"
      >Collapse</button>
      <button
        type="button"
        onclick={close}
        aria-label="Close"
        class="grid size-8 place-items-center hover:bg-hover-wash"
      >
        <RecordIcon name="x" class="size-4" />
      </button>
    </div>
    <ul class="m-0 mt-6 list-none p-0">
      {@render tvnode(pid, [], 0)}
    </ul>
  </div>
{/if}
