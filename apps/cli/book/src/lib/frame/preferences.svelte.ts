// The book preferences: the intro banner dismissal, persisted as book-intro.

function readDismissed(): boolean {
  try {
    if (typeof localStorage === "undefined") return false;
    return localStorage.getItem("book-intro") === "closed";
  } catch {
    return false;
  }
}

let dismissed = $state(readDismissed());

/** The book preferences: whether the intro is dismissed, and a dismissal that persists. */
export const preferences = {
  get introDismissed(): boolean {
    return dismissed;
  },
  dismissIntro(): void {
    dismissed = true;
    try {
      if (typeof localStorage !== "undefined") localStorage.setItem("book-intro", "closed");
    } catch {
      // The store is unavailable; the banner still closes for this view.
    }
  },
};
