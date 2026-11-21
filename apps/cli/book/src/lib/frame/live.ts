// The live rebuild channel. A book served over http or https listens for the
// server's named events and tells the store when the data is rebuilt. A book
// opened from file:// has no server, so nothing connects. EventSource reconnects
// by itself; nothing here polls.

/** One event from the server: a build started, finished, or failed with one line of error. */
export type LiveEvent = { kind: "building" } | { kind: "built"; generatedOn: string } | { kind: "failed"; error: string };

/** The text an event carries, or nothing when it carries none. */
function dataOf(event: Event): string {
  return "data" in event && typeof event.data === "string" ? event.data : "";
}

/** Opens the channel when the book is served, and passes each event to onEvent. A closed server or a missing EventSource leaves the book as it is. */
export function startLive(onEvent: (event: LiveEvent) => void): void {
  if (location.protocol !== "http:" && location.protocol !== "https:") return;
  try {
    const source = new EventSource("events");
    source.addEventListener("building", () => onEvent({ kind: "building" }));
    source.addEventListener("built", (event) => onEvent({ kind: "built", generatedOn: dataOf(event) }));
    source.addEventListener("failed", (event) => onEvent({ kind: "failed", error: dataOf(event) }));
  } catch {
    // No EventSource in this browser: the book stays as it was opened.
  }
}
