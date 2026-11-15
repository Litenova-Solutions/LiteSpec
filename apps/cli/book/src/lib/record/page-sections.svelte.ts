import { untrack } from "svelte";

// The sections of the open record page, for the rail's "On this page" list.
// Each page section registers itself while it is shown. Registration never
// reads the list reactively, so a section's effect does not rerun on its own
// write, and the list keeps page order by each section's place in the document.
export interface PageSectionEntry {
  key: string;
  label: string;
  count: number | null;
}

/** Page order: a section earlier in the document comes first. */
function byPosition(a: PageSectionEntry, b: PageSectionEntry): number {
  const ea = document.getElementById("b-" + a.key);
  const eb = document.getElementById("b-" + b.key);
  if (!ea || !eb) return 0;
  return ea.compareDocumentPosition(eb) & Node.DOCUMENT_POSITION_FOLLOWING ? -1 : 1;
}

class PageSections {
  items = $state<PageSectionEntry[]>([]);

  /** Register a shown section; the returned function removes it again. */
  add(entry: PageSectionEntry): () => void {
    untrack(() => {
      this.items = [...this.items.filter((x) => x.key !== entry.key), entry].sort(byPosition);
    });
    return () => {
      untrack(() => {
        this.items = this.items.filter((x) => x.key !== entry.key);
      });
    };
  }
}

export const pageSections = new PageSections();
