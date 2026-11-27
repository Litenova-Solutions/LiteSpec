// Written by litespec sync from the records. Edit the records, then run litespec sync.
import type { PermissionId } from "./permissions";
export const screenCapabilities: Record<string, readonly PermissionId[]> = {
  "/": [],
  "/check-ins": [],
  "/check-ins/{checkInId}": [],
};
