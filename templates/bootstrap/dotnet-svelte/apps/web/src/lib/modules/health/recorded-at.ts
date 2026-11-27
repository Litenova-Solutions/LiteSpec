// The moment a check-in was recorded, as a reader reads it: date and time in
// UTC with a fixed locale, so the server render and the browser render agree.
const format = new Intl.DateTimeFormat("en-GB", {
  dateStyle: "medium",
  timeStyle: "short",
  timeZone: "UTC",
});

export function formatRecordedAt(recordedAt: string): string {
  return `${format.format(new Date(recordedAt))} UTC`;
}
