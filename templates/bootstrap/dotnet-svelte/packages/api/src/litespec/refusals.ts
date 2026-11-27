// Written by litespec sync from the records. Edit the records, then run litespec sync.
export const refusals = [
  "health.check-in-unknown",
] as const;
export type RefusalCode = (typeof refusals)[number];
