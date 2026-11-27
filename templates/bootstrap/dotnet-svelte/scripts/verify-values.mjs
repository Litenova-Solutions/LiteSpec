// The values command of `litespec verify` for __PROJECT__: signs Ava in through
// the development sign-in, records one check-in through the API the probes
// run against, and prints its identifier as recordedCheckIn, so the read probe
// names a check-in that exists. verify passes the API address in
// LITESPEC_BASE_URL; diagnostics go to stderr, the JSON object to stdout.
const baseUrl = process.env.LITESPEC_BASE_URL;
if (!baseUrl) {
  console.error("LITESPEC_BASE_URL is not set; run this through litespec verify.");
  process.exit(1);
}

const signIn = await fetch(new URL("/auth/development/ava", baseUrl), { redirect: "manual" });
const cookie = signIn.headers
  .getSetCookie()
  .map((header) => header.split(";")[0])
  .join("; ");
if (signIn.status >= 400 || cookie === "") {
  console.error(`The development sign-in answered ${signIn.status} with no session cookie.`);
  process.exit(1);
}

const recorded = await fetch(new URL("/api/health/check-ins", baseUrl), {
  method: "POST",
  headers: { "content-type": "application/json", cookie },
  body: JSON.stringify({ note: "Check-in recorded for the read probe." }),
});
if (recorded.status !== 201) {
  console.error(`Recording the check-in answered ${recorded.status}: ${await recorded.text()}`);
  process.exit(1);
}

const { checkInId } = await recorded.json();
process.stdout.write(JSON.stringify({ recordedCheckIn: checkInId }));
