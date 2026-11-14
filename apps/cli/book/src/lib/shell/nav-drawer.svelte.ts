// The navigation drawer below the lg breakpoint. It opens on the record selected
// at the time and stays open only while that record stays selected, so a choice
// of any record closes it, the record already selected included. Escape, the
// backdrop and a widen past the breakpoint close it too; a narrow again later
// starts closed.

export class NavDrawer {
  #openedOn = $state<string | null | undefined>(undefined);
  #narrow = $state(false);
  readonly #selected: () => string | null | undefined;

  constructor(selected: () => string | null | undefined, narrow: boolean) {
    this.#selected = selected;
    this.#narrow = narrow;
  }

  /** Whether the navigation is open: it is open only while the record it opened on stays selected. */
  get open(): boolean {
    return this.#openedOn !== undefined && this.#openedOn === this.#selected();
  }

  /** Whether the drawer covers the page: the layout is narrow and the navigation is open. */
  get drawer(): boolean {
    return this.#narrow && this.open;
  }

  /** Follows the breakpoint. Leaving the narrow layout closes the drawer. */
  setNarrow(narrow: boolean): void {
    this.#narrow = narrow;
    if (!narrow) this.#openedOn = undefined;
  }

  /** Opens the navigation on the record selected now. */
  openOn(): void {
    this.#openedOn = this.#selected();
  }

  /** Closes the navigation. Returns whether it was open, so the caller returns focus only then. */
  close(): boolean {
    const wasOpen = this.open;
    this.#openedOn = undefined;
    return wasOpen;
  }

  /** Escape closes the drawer when it covers the page. Returns whether it closed. */
  escape(): boolean {
    return this.drawer && this.close();
  }
}
