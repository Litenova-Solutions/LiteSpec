// Request and response types of the frontend facts protocol, version 1. Field
// names and shapes follow the protocol exactly; see the corpus request files.

export const PROTOCOL_VERSION = 1;
export const READER_VERSION = "1.0.0";

export interface FrontendRequest {
  name: string;
  platform: string;
  root: string;
  routes: string;
  modules: string;
  frame: string;
  testRoots: string[];
  aliases: Record<string, string>;
  apiClient: string;
}

export interface ReaderRequest {
  protocol: number;
  frontends: FrontendRequest[];
  cache: string | null;
}

export interface ModuleFacts {
  folder: string;
  file: string;
  line: number;
  marks: string[];
}

export interface CallFacts {
  operation: string;
  file: string;
  line: number;
}

export interface RouteFacts {
  file: string;
  line: number;
  address: string;
  marks: string[];
  calls: CallFacts[];
}

export interface TestFacts {
  file: string;
  line: number;
  title: string;
  proves: string[];
  journey: string | null;
  gotos: string[];
  codes: string[];
  texts: string[];
}

export interface Diagnostic {
  file: string;
  line: number;
  message: string;
}

export interface FrontendFacts {
  name: string;
  modules: ModuleFacts[];
  routes: RouteFacts[];
  tests: TestFacts[];
  diagnostics: Diagnostic[];
}

export interface ReaderResponse {
  protocol: number;
  reader: string;
  typescript: string;
  svelte: string;
  frontends: FrontendFacts[];
}

// Shared run state for one frontend. diagnose records a parse diagnostic for a
// file once; repeated attempts at the same file stay silent.
export interface ReaderContext {
  root: string;
  cacheDir: string | null;
  diagnose(absPath: string, kind: "typescript" | "svelte"): void;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function asString(value: unknown): string | null {
  return typeof value === "string" ? value : null;
}

function asStringArray(value: unknown): string[] {
  if (!Array.isArray(value)) return [];
  return value.filter((entry): entry is string => typeof entry === "string");
}

function asStringMap(value: unknown): Record<string, string> {
  if (!isRecord(value)) return {};
  const result: Record<string, string> = {};
  for (const key of Object.keys(value)) {
    if (typeof value[key] === "string") result[key] = value[key] as string;
  }
  return result;
}

function readFrontend(value: unknown): FrontendRequest {
  const item = isRecord(value) ? value : {};
  return {
    name: asString(item["name"]) ?? "",
    platform: asString(item["platform"]) ?? "",
    root: asString(item["root"]) ?? "",
    routes: asString(item["routes"]) ?? "",
    modules: asString(item["modules"]) ?? "",
    frame: asString(item["frame"]) ?? "",
    testRoots: asStringArray(item["testRoots"]),
    aliases: asStringMap(item["aliases"]),
    apiClient: asString(item["apiClient"]) ?? "",
  };
}

// Parses and validates a request document. Unknown fields are ignored; a wrong
// protocol version or a missing frontend list fails the run, since no complete
// response can be produced for them.
export function parseRequest(text: string): ReaderRequest {
  const document: unknown = JSON.parse(text);
  if (!isRecord(document)) throw new Error("invalid request: expected a JSON object");
  if (document["protocol"] !== PROTOCOL_VERSION) {
    throw new Error(`unsupported protocol ${String(document["protocol"])}`);
  }
  if (!Array.isArray(document["frontends"])) throw new Error("invalid request: frontends must be an array");
  return {
    protocol: PROTOCOL_VERSION,
    frontends: document["frontends"].map(readFrontend),
    cache: asString(document["cache"]),
  };
}
