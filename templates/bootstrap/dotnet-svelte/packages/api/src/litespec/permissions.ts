// Written by litespec sync from the records. Edit the records, then run litespec sync.
export const permissions = {
  HealthRecordCheckIn: "permission/record-check-in",
} as const;
export type PermissionId = (typeof permissions)[keyof typeof permissions];
