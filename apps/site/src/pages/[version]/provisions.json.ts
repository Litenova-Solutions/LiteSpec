import type { APIRoute, GetStaticPaths } from "astro";
import fs from "node:fs";
import path from "node:path";
import { root, version } from "../../lib/spec";

export const getStaticPaths = (() => [{ params: { version } }]) satisfies GetStaticPaths;

export const GET: APIRoute = () => {
  const bytes = fs.readFileSync(path.join(root, "docs/reference/provisions.json"));
  return new Response(new Uint8Array(bytes), {
    headers: { "Content-Type": "application/json" },
  });
};
