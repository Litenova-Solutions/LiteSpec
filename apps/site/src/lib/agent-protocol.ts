// The consumer agent protocol the site renders: the generated region of
// templates/consumer/project-agents.md, filled for one selection. The select
// page and /llms.txt share this module, so the pasted block, the index and
// `litespec init` cannot disagree: all three fill the same template with the
// same placeholders, replaced verbatim.
import fs from "node:fs";
import path from "node:path";
import { backendStacks, frontendStacks, root, version } from "./spec.ts";

/** The opening fence of the generated region, on its own line. */
export const AGENT_PROTOCOL_OPEN = "<!-- generated:agent-protocol -->";

/** The closing fence of the generated region, on its own line. */
export const AGENT_PROTOCOL_CLOSE = "<!-- /generated:agent-protocol -->";

/** The values one filled agent protocol block takes, one per template placeholder. */
export interface AgentProtocolValues {
  /** The release, such as 5.2.0. */
  version: string;
  /** small or large. */
  size: string;
  /** server or client-only. */
  shape: string;
  /** The selected stack keys, comma and space separated, or none. */
  stacks: string;
  /** The selected extension keys, comma and space separated, or none. */
  extensions: string;
  /** The claimed layers, such as intent and behavior: litespec, implementation and proof: own. */
  layers: string;
  /** The selection key of the shape and stacks, such as server.dotnet.react. */
  selectionKey: string;
  /** dotnet litespec or litespec. */
  command: string;
}

/** Every placeholder the region may hold, in contract order, with the values field that fills it and what it takes. */
export const AGENT_PROTOCOL_PLACEHOLDERS: { placeholder: string; field: keyof AgentProtocolValues; takes: string }[] = [
  { placeholder: "__LITESPEC_VERSION__", field: "version", takes: "the release the project follows" },
  { placeholder: "__SIZE__", field: "size", takes: "small or large" },
  { placeholder: "__SHAPE__", field: "shape", takes: "server or client-only" },
  { placeholder: "__STACKS__", field: "stacks", takes: "the selected stack keys, comma and space separated, or none" },
  { placeholder: "__EXTENSIONS__", field: "extensions", takes: "the selected extension keys, comma and space separated, or none" },
  { placeholder: "__LAYERS__", field: "layers", takes: "the claimed layers, such as intent and behavior: litespec, implementation and proof: own" },
  { placeholder: "__SELECTION_KEY__", field: "selectionKey", takes: "the selection key of the shape and stacks, such as server.dotnet.react" },
  { placeholder: "__LITESPEC_COMMAND__", field: "command", takes: "dotnet litespec or litespec" },
];

/** The first line of the region, with the command that runs the CLI filled in. */
export function precedenceSentence(command: string): string {
  return `When this file disagrees with \`${command} rules\`, \`${command} model query\` or a record, the tool output and the record win; this file only points to them.`;
}

/** The generated region of the consumer protocol template, fences included. */
export function loadAgentProtocolRegion(): string {
  const file = path.join(root, "templates/consumer/project-agents.md");
  const text = fs.readFileSync(file, "utf8").replace(/\r\n/g, "\n");
  const open = text.indexOf(AGENT_PROTOCOL_OPEN);
  const close = text.indexOf(AGENT_PROTOCOL_CLOSE);
  if (open < 0 || close < 0 || close < open) {
    throw new Error(`templates/consumer/project-agents.md holds no ${AGENT_PROTOCOL_OPEN} region.`);
  }
  return text.slice(open, close + AGENT_PROTOCOL_CLOSE.length);
}

/** Fill every placeholder of the region with its value, verbatim. */
export function renderAgentProtocol(region: string, values: AgentProtocolValues): string {
  let out = region;
  for (const entry of AGENT_PROTOCOL_PLACEHOLDERS) {
    out = out.split(entry.placeholder).join(values[entry.field]);
  }
  return out;
}

/** The size the site fills where it has no project: the small default of the select page. */
export const SITE_DEFAULT_SIZE = "small";

/** The shape the site fills where it has no project. */
export const SITE_DEFAULT_SHAPE = "server";

/** The command the site fills: the `litespec init` default, a local .NET tool. */
export const SITE_DEFAULT_COMMAND = "dotnet litespec";

/** The layers the site fills: the consumer template claims every layer for LiteSpec. */
export const SITE_DEFAULT_LAYERS = "intent and behavior: litespec, implementation and proof: litespec";

/** The stacks of the site's default selection: the first backend stack with React when published. */
export function siteDefaultStacks(): string[] {
  const backend = backendStacks[0] ?? "";
  const frontend = frontendStacks.includes("react") ? "react" : (frontendStacks[0] ?? "");
  return frontend ? [backend, frontend] : [backend];
}

/** The selection key for a shape and stacks pair: the shape and the stacks joined with dots. */
export function formatSelectionKey(shape: string, stacks: string[]): string {
  return stacks.length === 0 ? shape : `${shape}.${stacks.join(".")}`;
}

/** The filled values for one select-page answer set. */
export function selectionValues(input: {
  size: string;
  shape: string;
  stacks: string[];
  extensions: string[];
  selectionKey: string;
}): AgentProtocolValues {
  return {
    version,
    size: input.size,
    shape: input.shape,
    stacks: input.stacks.length === 0 ? "none" : input.stacks.join(", "),
    extensions: input.extensions.length === 0 ? "none" : input.extensions.join(", "),
    layers: SITE_DEFAULT_LAYERS,
    selectionKey: input.selectionKey,
    command: SITE_DEFAULT_COMMAND,
  };
}

/** The filled values /llms.txt shows: the default selection with no extensions. */
export function siteDefaultValues(): AgentProtocolValues {
  const stacks = siteDefaultStacks();
  return selectionValues({
    size: SITE_DEFAULT_SIZE,
    shape: SITE_DEFAULT_SHAPE,
    stacks,
    extensions: [],
    selectionKey: formatSelectionKey(SITE_DEFAULT_SHAPE, stacks),
  });
}
