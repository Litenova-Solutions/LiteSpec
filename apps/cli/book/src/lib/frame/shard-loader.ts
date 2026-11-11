// Every shard arrives as data/<key>.js calling BookData.put once. The book opens
// from file://, where fetch is blocked, so shards load through script elements.
type Resolve = (value: unknown) => void;

export interface ShardLoader {
  put(key: string, value: unknown): void;
  get(key: string): unknown;
  load(key: string): Promise<unknown>;
}

export function createShardLoader(doc: Document, dir = "data/"): ShardLoader {
  const data = new Map<string, unknown>();
  const pending = new Map<string, Resolve[]>();
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
        const script = doc.createElement("script");
        script.dataset.shard = key;
        script.src = dir + key + ".js";
        script.async = true;
        script.onerror = () => {
          pending.delete(key);
          reject(new Error("Cannot load " + key));
        };
        doc.head.appendChild(script);
      });
    },
  };
}
