import { createShardLoader, type ShardLoader } from "$lib/data/shards";
import { Book } from "./book.svelte";

declare global {
  interface Window { BookData: ShardLoader }
}

// Each shard script calls BookData.put, so the loader is global before any shard loads.
window.BookData = createShardLoader(document);

/** The one book the page shows; components read and change it directly. */
export const book = new Book(window.BookData);
export * from "./book.svelte";
export type * from "./types";
