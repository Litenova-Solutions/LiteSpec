<script lang="ts">
  import { book } from "$lib/book";

  // The global keys. Typing, Ctrl, Cmd and Alt keep their own meaning, except
  // Ctrl or Cmd K; shadcn closes its own overlays on Escape.
  let { focusSearch, openKeys }: { focusSearch: (select: boolean) => void; openKeys: () => void } = $props();

  const views = ["outline", "security"] as const;

  async function move(delta: number) {
    const list = book.navIds;
    const sel = book.selected;
    const i = list.indexOf(sel ?? "");
    const n = list[i + delta] ?? (i < 0 ? list[0] : undefined);
    if (!n) return;
    await book.select(n);
    book.focusNavTick++;
  }

  async function keyed(e: KeyboardEvent) {
    const k = e.key;
    const t = e.target as HTMLElement | null;
    const typing = !!t?.tagName && /^(INPUT|TEXTAREA|SELECT)$/.test(t.tagName);
    if ((e.ctrlKey || e.metaKey) && (k === "k" || k === "K")) {
      e.preventDefault();
      focusSearch(true);
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
      focusSearch(false);
      return;
    }
    if (k === "?") {
      openKeys();
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
    const grown = !!sel && !!book.expanded[sel] && book.kids(sel).length > 0;
    if (k === "u" || (inTree && k === "ArrowLeft" && !grown)) {
      const p = sel ? book.parent(sel) : null;
      if (p) {
        await book.select(p);
        book.focusNavTick++;
      }
      e.preventDefault();
      return;
    }
    if (inTree && k === "ArrowLeft") {
      if (sel) book.expanded[sel] = false;
      book.focusNavTick++;
      e.preventDefault();
      return;
    }
    if (inTree && k === "ArrowRight") {
      if (sel && book.kids(sel).length) {
        book.expanded[sel] = true;
        book.focusNavTick++;
      }
      e.preventDefault();
      return;
    }
    if (k === "[" || k === "]") {
      if (sel) {
        const sib = book.siblingsOf(sel).filter((x) => book.typeOf(x) === book.typeOf(sel));
        const n = sib[sib.indexOf(sel) + (k === "]" ? 1 : -1)];
        if (n) {
          await book.select(n);
          book.focusNavTick++;
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
      void book.setView(views[+k - 1]);
    }
  }
</script>

<svelte:window onkeydown={keyed} />
