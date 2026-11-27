// The book's theme: light by default, with the reader's choice kept across
// visits. The document theme drives the palette through the color scheme.

/** The book's themes: light by default, dark by reader choice. */
export type BookTheme = "light" | "dark";

const STORAGE_KEY = "book-theme";

/** The theme the page shows now: the document theme, defaulting to light. */
export function currentTheme(): BookTheme {
  return document.documentElement.dataset.theme === "dark" ? "dark" : "light";
}

/** Shows a theme and keeps the reader's choice for the next visit. */
export function applyTheme(theme: BookTheme): void {
  document.documentElement.dataset.theme = theme;
  try {
    localStorage.setItem(STORAGE_KEY, theme);
  } catch {
    // A reader without storage still reads; the choice lasts the visit.
  }
}

/** Shows the kept theme, or light when the reader kept none. */
export function initTheme(): void {
  let kept: string | null = null;
  try {
    kept = localStorage.getItem(STORAGE_KEY);
  } catch {
    kept = null;
  }
  document.documentElement.dataset.theme = kept === "dark" || kept === "light" ? kept : "light";
}
