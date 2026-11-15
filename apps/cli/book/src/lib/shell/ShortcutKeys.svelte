<script lang="ts">
  import { book } from "$lib/frame";
  import { openRecord } from "./record-navigation";

  // The global keys. Typing, Ctrl, Cmd and Alt keep their own meaning, except
  // Ctrl or Cmd K; shadcn closes its own overlays on Escape. An open dialog
  // keeps every key. Moves land with focus in the outline on the opened row.
  type Props = { onFocusSearchRequest: (select: boolean) => void; onOpenShortcuts: () => void; onRevealRequest: () => void };
  let { onFocusSearchRequest, onOpenShortcuts, onRevealRequest }: Props = $props();

  const views = ["outline", "security"] as const;

  // Reveal now, and again on the next frame: the record page focuses the title
  // in the same flush, and the second reveal lands after it, on the row.
  function revealInOutline(): void {
    onRevealRequest();
    requestAnimationFrame(() => onRevealRequest());
  }

  let lastMoveAt = 0;

  async function move(delta: number): Promise<void> {
    const list = book.navOrder();
    const sel = book.selected;
    const i = list.indexOf(sel ?? "");
    const n = list[i + delta] ?? (i < 0 ? list[0] : undefined);
    if (!n) return;
    // Rapid j and k moves coalesce into one history entry; a pause starts a new one.
    const now = Date.now();
    const replace = now - lastMoveAt < 800;
    lastMoveAt = now;
    await openRecord(n, { replace });
    revealInOutline();
  }

  async function jumpTo(edge: "first" | "last"): Promise<void> {
    const list = book.navOrder();
    const n = edge === "first" ? list[0] : list[list.length - 1];
    if (!n) return;
    await openRecord(n);
    revealInOutline();
  }

  async function keyed(e: KeyboardEvent): Promise<void> {
    const k = e.key;
    const t = e.target as HTMLElement | null;
    if (t?.closest?.('[role="dialog"]')) return;
    const typing = !!t?.tagName && /^(INPUT|TEXTAREA|SELECT)$/.test(t.tagName);
    if ((e.ctrlKey || e.metaKey) && (k === "k" || k === "K")) {
      e.preventDefault();
      onFocusSearchRequest(true);
      return;
    }
    if (k === "Escape") {
      if (book.anyFilter() && !typing) book.clearFilters();
      return;
    }
    if (typing || e.ctrlKey || e.metaKey || e.altKey) return;
    const inTree = !!t?.closest?.("[role=treeitem]");
    const sel = book.selected;
    if (k === "/") {
      e.preventDefault();
      onFocusSearchRequest(false);
      return;
    }
    if (k === "?") {
      onOpenShortcuts();
      return;
    }
    if (k === "j" || (inTree && k === "ArrowDown")) {
      await move(1);
      e.preventDefault();
      return;
    }
    if (k === "k" || (inTree && k === "ArrowUp")) {
      await move(-1);
      e.preventDefault();
      return;
    }
    if (inTree && k === "Home") {
      await jumpTo("first");
      e.preventDefault();
      return;
    }
    if (inTree && k === "End") {
      await jumpTo("last");
      e.preventDefault();
      return;
    }
    const grown = !!sel && !!book.expanded[sel] && book.kids(sel).length > 0;
    if (k === "u" || (inTree && k === "ArrowLeft" && !grown)) {
      const p = sel ? book.parent(sel) : null;
      if (p) {
        await openRecord(p);
        revealInOutline();
      }
      e.preventDefault();
      return;
    }
    if (inTree && k === "ArrowLeft") {
      if (sel) book.expanded[sel] = false;
      onRevealRequest();
      e.preventDefault();
      return;
    }
    if (inTree && k === "ArrowRight") {
      if (sel && book.kids(sel).length) {
        book.expanded[sel] = true;
        onRevealRequest();
      }
      e.preventDefault();
      return;
    }
    if (k === "[" || k === "]") {
      if (sel) {
        const sib = book.siblingsOf(sel).filter((x) => book.typeOf(x) === book.typeOf(sel));
        const n = sib[sib.indexOf(sel) + (k === "]" ? 1 : -1)];
        if (n) {
          await openRecord(n);
          revealInOutline();
        }
      }
      return;
    }
    if (k === "e") {
      book.expandAll();
      return;
    }
    if (k === "c") {
      book.collapseAll();
      return;
    }
    if (k >= "1" && k <= "2") {
      const next = views[+k - 1];
      if (next) void book.setView(next);
    }
  }
</script>

<svelte:window onkeydown={keyed} />
