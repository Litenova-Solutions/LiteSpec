// The book theme: dark or light, persisted as book-theme and applied to the document.

function readStored(): boolean | null {
  try {
    if (typeof localStorage === "undefined") return null;
    const value = localStorage.getItem("book-theme");
    if (value === "dark") return true;
    if (value === "light") return false;
    return null;
  } catch {
    return null;
  }
}

function readDocument(): boolean {
  if (typeof document === "undefined") return false;
  return document.documentElement.getAttribute("data-theme") === "dark";
}

function apply(dark: boolean): void {
  if (typeof document !== "undefined") {
    document.documentElement.setAttribute("data-theme", dark ? "dark" : "light");
  }
  try {
    if (typeof localStorage !== "undefined") localStorage.setItem("book-theme", dark ? "dark" : "light");
  } catch {
    // The store is unavailable; the theme still applies for this view.
  }
}

const stored = readStored();
let darkState = $state(stored ?? readDocument());

if (typeof document !== "undefined" && stored !== null && stored !== readDocument()) {
  document.documentElement.setAttribute("data-theme", stored ? "dark" : "light");
}

/** The book theme: whether it is dark, and a toggle that persists the choice. */
export const theme = {
  get dark(): boolean {
    return darkState;
  },
  toggle(): void {
    darkState = !darkState;
    apply(darkState);
  },
};
