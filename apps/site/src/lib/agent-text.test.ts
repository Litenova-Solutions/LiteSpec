// Tests for the agent-facing text: page descriptions keep the first sentence
// of the Intent with page links rewritten, the agent protocol region loads
// from the consumer template and fills every placeholder, the root index
// embeds the filled block with each placeholder explained, no llms.txt
// output keeps a relative link, and every selection index leaves out the
// maintainer page, lists the tool pages, groups pages by artifact kind, and
// stays under 25 KB.
import { describe, it } from "node:test";
import assert from "node:assert";
import { describe as describePage, rootLlmsText, selectionLlmsText, versionLlmsText } from "./agent-text.ts";
import {
  AGENT_PROTOCOL_CLOSE,
  AGENT_PROTOCOL_OPEN,
  AGENT_PROTOCOL_PLACEHOLDERS,
  SITE_DEFAULT_COMMAND,
  formatSelectionKey,
  loadAgentProtocolRegion,
  precedenceSentence,
  renderAgentProtocol,
  selectionValues,
  siteDefaultValues,
} from "./agent-protocol.ts";
import { SITE_ORIGIN } from "./origin.ts";
import { pageByFile, selectionKey, selections, version, type Page } from "./spec.ts";

describe("page description", () => {
  it("keeps only the first sentence of the Intent", () => {
    const page = pageByFile("docs/core/authoring.md")!;
    assert.strictEqual(
      describePage(page),
      "Every page in LiteSpec shares one shape: a fixed style, testable rules called provisions, stable identifiers for those rules, and a table naming the evidence each rule needs.",
    );
  });

  it("cuts a synthetic intent at its first sentence and rewrites its links", () => {
    const page = {
      file: "docs/core/sample.md",
      title: "Sample",
      style: "standard",
      intent: "First [scope](scope.md)\nline. Second stays out.",
    } as Page;
    assert.strictEqual(describePage(page), `First [scope](${SITE_ORIGIN}/${version}/core/scope.md) line.`);
  });

  it("rewrites relative page links to absolute raw URLs", () => {
    const page = pageByFile("docs/frontend/web/patterns.md")!;
    assert.strictEqual(
      describePage(page),
      `The web mechanism for UI patterns lives in [Web UI](${SITE_ORIGIN}/${version}/frontend/web/ui.md), which states the pattern implementation for the [React Stack](${SITE_ORIGIN}/${version}/frontend/web/react/stack.md) and [Svelte Stack](${SITE_ORIGIN}/${version}/frontend/web/svelte/stack.md) together with the other shared UI rules.`,
    );
  });

  it("leaves external links alone", () => {
    const page = pageByFile("docs/guide/install-the-cli.md")!;
    assert.ok(describePage(page).includes("https://github.com/Litenova-Solutions/LiteSpec/releases"));
  });

  it("falls back for the glossary", () => {
    assert.strictEqual(describePage(pageByFile("docs/reference/glossary.md")!), "One heading per term, in alphabetical order.");
  });

  it("falls back for a page with no intent", () => {
    const page = pageByFile("docs/intent/decisions/0001-keep-release-reasoning-as-records/0001-keep-release-reasoning-as-records.md")!;
    assert.strictEqual(describePage(page), `The ${page.title} page of the LiteSpec ${version} release.`);
  });
});

describe("agent protocol region", () => {
  it("loads the fenced region from the consumer template", () => {
    const region = loadAgentProtocolRegion();
    assert.ok(region.startsWith(AGENT_PROTOCOL_OPEN), region.slice(0, 60));
    assert.ok(region.endsWith(AGENT_PROTOCOL_CLOSE), region.slice(-60));
  });

  it("holds 40 lines or fewer", () => {
    assert.ok(loadAgentProtocolRegion().split("\n").length <= 40);
  });

  it("opens with the precedence sentence", () => {
    const lines = loadAgentProtocolRegion().split("\n");
    assert.strictEqual(lines[1], precedenceSentence("__LITESPEC_COMMAND__"));
  });

  it("holds exactly the nine known placeholders", () => {
    const found = [...new Set(loadAgentProtocolRegion().match(/__[A-Z0-9_]+__/g) ?? [])].sort();
    assert.deepStrictEqual(
      found,
      AGENT_PROTOCOL_PLACEHOLDERS.map((entry) => entry.placeholder).sort(),
    );
  });

  it("replaces every placeholder verbatim", () => {
    const region = AGENT_PROTOCOL_PLACEHOLDERS.map((entry) => `${entry.placeholder}=${entry.placeholder}`).join("\n");
    const out = renderAgentProtocol(region, {
      version: "$&1.0",
      packageVersion: "$&1.0-rc.1",
      size: "large",
      shape: "server",
      stacks: "dotnet, react",
      extensions: "audit",
      layers: "intent and behavior: litespec, implementation and proof: own",
      selectionKey: "server.dotnet.react",
      command: "litespec",
    });
    assert.ok(out.includes("$&1.0=$&1.0"), out);
    assert.ok(!/__[A-Z0-9_]+__/.test(out), out);
  });

  it("fills the real region with no placeholder left", () => {
    const out = renderAgentProtocol(loadAgentProtocolRegion(), siteDefaultValues());
    assert.ok(!/__[A-Z0-9_]+__/.test(out), out);
    assert.ok(out.includes(precedenceSentence(SITE_DEFAULT_COMMAND)), out);
  });

  it("joins stacks and extensions with comma and space, or none", () => {
    const filled = selectionValues({
      size: "large",
      shape: "server",
      stacks: ["dotnet", "react"],
      extensions: ["audit", "cache"],
      selectionKey: "server.dotnet.react",
    });
    assert.strictEqual(filled.stacks, "dotnet, react");
    assert.strictEqual(filled.extensions, "audit, cache");
    const bare = selectionValues({ size: "small", shape: "server", stacks: ["dotnet"], extensions: [], selectionKey: "server.dotnet" });
    assert.strictEqual(bare.extensions, "none");
  });

  it("renders none for an empty stack list", () => {
    const bare = selectionValues({ size: "small", shape: "client-only", stacks: [], extensions: [], selectionKey: "client-only" });
    assert.strictEqual(bare.stacks, "none");
  });

  it("joins a selection key from its shape and stacks", () => {
    assert.strictEqual(formatSelectionKey("server", ["dotnet", "react"]), "server.dotnet.react");
    assert.strictEqual(formatSelectionKey("server", []), "server");
  });
});

describe("root index", () => {
  const block = renderAgentProtocol(loadAgentProtocolRegion(), siteDefaultValues());
  const text = rootLlmsText(block);

  it("embeds the filled default block", () => {
    assert.ok(text.includes("default selection"), text);
    assert.ok(text.includes(AGENT_PROTOCOL_OPEN), text);
    assert.ok(text.includes(precedenceSentence(SITE_DEFAULT_COMMAND)), text);
  });

  it("explains every placeholder", () => {
    for (const entry of AGENT_PROTOCOL_PLACEHOLDERS) {
      assert.ok(text.includes(entry.placeholder), entry.placeholder);
      assert.ok(text.includes(entry.takes), entry.placeholder);
    }
  });
});

/** Every Markdown link target in the text, outside inline code spans. */
function linkTargets(text: string): string[] {
  const withoutCode = text
    .split("`")
    .filter((_, index) => index % 2 === 0)
    .join("");
  return [...withoutCode.matchAll(/\]\(\s*([^)\s]+)/g)].map((match) => match[1]);
}

/** Asserts no target is relative: each names a scheme, an absolute path, or an anchor. */
function assertNoRelativeLinks(text: string, name: string): void {
  for (let target of linkTargets(text)) {
    if (target.startsWith("<") && target.endsWith(">")) target = target.slice(1, -1);
    assert.ok(/^(?:[a-zA-Z][a-zA-Z0-9+.-]*:|\/\/|\/|#)/.test(target) || target === "", `${name} keeps relative link ${target}`);
  }
}

describe("selection index", () => {
  it("lists no maintainer page in any selection", () => {
    for (const selection of selections()) {
      const text = selectionLlmsText(selection);
      assert.ok(!text.includes("/core/maintaining.md"), selectionKey(selection));
      assert.ok(!text.includes("Maintaining the Standards"), selectionKey(selection));
    }
  });

  it("keeps the authoring page in every selection", () => {
    for (const selection of selections()) {
      assert.ok(selectionLlmsText(selection).includes("Authoring Standard"), selectionKey(selection));
    }
  });

  it("lists the tool pages and the published urls page in every selection", () => {
    const files = ["tools/rules", "tools/model", "tools/new", "tools/skill", "reference/published-urls"];
    for (const selection of selections()) {
      const text = selectionLlmsText(selection);
      for (const file of files) {
        assert.ok(text.includes(`${SITE_ORIGIN}/${version}/${file}.md`), `${selectionKey(selection)} misses ${file}`);
      }
    }
  });

  it("groups the selection pages by artifact kind", () => {
    const selection = selections().find((entry) => selectionKey(entry) === "server.dotnet.react")!;
    const text = selectionLlmsText(selection);
    assert.ok(text.includes("## By Artifact"), text);
    assert.ok(!text.includes("- any:"), "an any line is listed");
    const line = text.split("\n").find((entry) => entry.startsWith("- endpoint:"));
    assert.ok(line, "no endpoint line");
    assert.ok(line.includes("HTTP API"), line);
    assert.ok(!line.includes("Maintaining the Standards"), line);
  });

  it("keeps every selection index under 25 KB", () => {
    for (const selection of selections()) {
      const bytes = Buffer.byteLength(selectionLlmsText(selection), "utf8");
      assert.ok(bytes < 25 * 1024, `${selectionKey(selection)} is ${bytes} bytes`);
    }
  });
});

describe("relative links", () => {
  it("keeps no relative link in the release index", () => {
    assertNoRelativeLinks(versionLlmsText(), "release index");
  });

  it("keeps no relative link in any selection index", () => {
    for (const selection of selections()) {
      assertNoRelativeLinks(selectionLlmsText(selection), selectionKey(selection));
    }
  });

  it("keeps no relative link in the root index", () => {
    const block = renderAgentProtocol(loadAgentProtocolRegion(), siteDefaultValues());
    assertNoRelativeLinks(rootLlmsText(block), "root index");
  });
});
