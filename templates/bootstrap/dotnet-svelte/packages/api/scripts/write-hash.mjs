// Records the contract the generated client was generated from, so
// litespec model check proves the client current. Runs in the web check
// before svelte-check, and in api:codegen after the generator.
import { createHash } from "node:crypto";
import { readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const contract = join(root, "openapi", "openapi.json");
const hashFile = join(root, "src", "generated", "contract.sha256");

const bytes = readFileSync(contract);
const normalized = bytes.toString("utf8").replace(/\r\n?/g, "\n");
const digest = createHash("sha256").update(normalized, "utf8").digest("hex");
writeFileSync(hashFile, `${digest}\n`);
console.log(`Wrote ${hashFile}.`);
