// Every shard arrives as data/<key>.js calling BookData.put once. The book opens
// from file://, where fetch is blocked, so shards load through script elements.
// A rebuild forgets every shard and asks for the new ones under a version query,
// so the browser never runs an old script again.
type Resolve = (value: unknown) => void;

export interface ShardLoader {
  put(key: string, value: unknown): void;
  get(key: string): unknown;
  load(key: string): Promise<unknown>;
  /** Forgets every shard put so far and sets the version the next script requests carry. */
  reset(version: string): void;
}

export function createShardLoader(doc: Document, dir = "data/"): ShardLoader {
  let data = new Map<string, unknown>();
  let pending = new Map<string, Resolve[]>();
  let version = "";
  return {
    put(key, value) {
      data.set(key, value);
      const waiting = pending.get(key) ?? [];
      pending.delete(key);
      for (const resolve of waiting) resolve(value);
    },
    get(key: string) {
      return data.get(key);
    },
    load(key: string) {
      if (data.has(key)) return Promise.resolve(data.get(key));
      return new Promise<unknown>((resolve, reject) => {
        const waiting = pending.get(key);
        if (waiting) {
          waiting.push(resolve);
          return;
        }
        pending.set(key, [resolve]);
        for (const old of doc.querySelectorAll<HTMLScriptElement>("script[data-shard]")) {
          if (old.dataset.shard === key) old.remove();
        }
        const script = doc.createElement("script");
        script.dataset.shard = key;
        script.src = dir + key + ".js" + (version ? "?v=" + encodeURIComponent(version) : "");
        script.async = true;
        script.onerror = () => {
          pending.delete(key);
          reject(new Error("Cannot load " + key));
        };
        doc.head.appendChild(script);
      });
    },
    reset(next: string) {
      data = new Map<string, unknown>();
      pending = new Map<string, Resolve[]>();
      version = next;
    },
  };
}
