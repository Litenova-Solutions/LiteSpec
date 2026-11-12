// Runtime parsers for the book shards: each shard shape is checked once at load,
// so the store and the views read typed data without casts.

import type {
  BookEdgeJson,
  BookFinding,
  BookRecord,
  BookSection,
  BookSections,
  BookTest,
  CaptureState,
  IntentCoverage,
  IntentCoverageEntry,
  LayerClaim,
  Lexicon,
  LexiconLayer,
  LexiconLink,
  LexiconSection,
  LexiconType,
  Manifest,
  OutlineKind,
  OutlineNode,
  ProofState,
  SearchRow,
  SecurityItem,
  SourceRef,
} from "./shard-types";

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && Array.isArray(value) === false;
}

function fail(what: string): never {
  throw new Error("The book data is invalid: " + what + ".");
}

function reqString(obj: Record<string, unknown>, key: string, what: string): string {
  const value = obj[key];
  if (typeof value !== "string") fail(what + " needs a " + key + " string");
  return value;
}

function optString(obj: Record<string, unknown>, key: string, what: string): string | undefined {
  const value = obj[key];
  if (value === undefined) return undefined;
  if (typeof value !== "string") fail(what + " needs a " + key + " string");
  return value;
}

function optNullableString(obj: Record<string, unknown>, key: string, what: string): string | null | undefined {
  const value = obj[key];
  if (value === undefined) return undefined;
  if (value === null) return null;
  if (typeof value !== "string") fail(what + " needs a " + key + " string");
  return value;
}

function optNumber(obj: Record<string, unknown>, key: string, what: string): number | undefined {
  const value = obj[key];
  if (value === undefined) return undefined;
  if (typeof value !== "number") fail(what + " needs a " + key + " number");
  return value;
}

function optNullableNumber(obj: Record<string, unknown>, key: string, what: string): number | null | undefined {
  const value = obj[key];
  if (value === undefined) return undefined;
  if (value === null) return null;
  if (typeof value !== "number") fail(what + " needs a " + key + " number");
  return value;
}

function optBoolean(obj: Record<string, unknown>, key: string, what: string): boolean | undefined {
  const value = obj[key];
  if (value === undefined) return undefined;
  if (typeof value !== "boolean") fail(what + " needs a " + key + " boolean");
  return value;
}

function optNullableBoolean(obj: Record<string, unknown>, key: string, what: string): boolean | null | undefined {
  const value = obj[key];
  if (value === undefined) return undefined;
  if (value === null) return null;
  if (typeof value !== "boolean") fail(what + " needs a " + key + " boolean");
  return value;
}

function reqStringArray(value: unknown, what: string): string[] {
  if (Array.isArray(value) === false) fail(what + " needs a string array");
  const out: string[] = [];
  for (const item of value) {
    if (typeof item !== "string") fail(what + " needs string items");
    out.push(item);
  }
  return out;
}

function optStringArray(obj: Record<string, unknown>, key: string, what: string): string[] | undefined {
  const value = obj[key];
  if (value === undefined) return undefined;
  return reqStringArray(value, what + "." + key);
}

function reqStringRecord(value: unknown, what: string): Record<string, string> {
  if (isRecord(value) === false) fail(what + " needs a string map");
  const out: Record<string, string> = {};
  for (const [key, item] of Object.entries(value)) {
    if (typeof item !== "string") fail(what + " needs string values");
    out[key] = item;
  }
  return out;
}

function reqNumberRecord(value: unknown, what: string): Record<string, number> {
  if (isRecord(value) === false) fail(what + " needs a number map");
  const out: Record<string, number> = {};
  for (const [key, item] of Object.entries(value)) {
    if (typeof item !== "number") fail(what + " needs number values");
    out[key] = item;
  }
  return out;
}

function optStringArrayRecord(obj: Record<string, unknown>, key: string, what: string): Record<string, string[]> | undefined {
  const value = obj[key];
  if (value === undefined) return undefined;
  if (isRecord(value) === false) fail(what + " needs a " + key + " map");
  const out: Record<string, string[]> = {};
  for (const [entryKey, item] of Object.entries(value)) {
    out[entryKey] = reqStringArray(item, what + "." + key);
  }
  return out;
}

function isProofState(value: unknown): value is ProofState {
  return (
    value === "passing" ||
    value === "failing" ||
    value === "awaiting" ||
    value === "unconfirmed" ||
    value === "unproved" ||
    value === "indirect" ||
    value === "norun"
  );
}

function isCaptureState(value: unknown): value is CaptureState {
  return value === "captured" || value === "route-not-captured" || value === "no-route";
}

function isOutlineKind(value: unknown): value is OutlineKind {
  return value === "record" || value === "group" || value === "code" || value === "test" || value === "finding";
}

function isLayerKey(value: unknown): value is "intent" | "behavior" | "implementation" | "proof" {
  return value === "intent" || value === "behavior" || value === "implementation" || value === "proof";
}

/** One edge as the records packs carry it: the other end and the field key. */
export function parseBookEdgeJson(raw: unknown): BookEdgeJson {
  if (isRecord(raw) === false) fail("an edge needs an object");
  const id = raw["id"];
  const key = raw["key"];
  if (typeof id !== "string" || id === "") fail("an edge needs an id string");
  if (typeof key !== "string" || key === "") fail("an edge needs a key string");
  return { id, key };
}

function parseEdgeList(value: unknown, what: string): BookEdgeJson[] | undefined {
  if (value === undefined) return undefined;
  if (Array.isArray(value) === false) fail(what + " needs an edge list");
  return value.map((item) => parseBookEdgeJson(item));
}

function parseSourceRef(value: unknown, what: string): SourceRef | null | undefined {
  if (value === undefined) return undefined;
  if (value === null) return null;
  if (isRecord(value) === false) fail(what + " needs a source object");
  return {
    path: optNullableString(value, "path", what),
    line: optNullableNumber(value, "line", what),
    litespec: optBoolean(value, "litespec", what),
  };
}

function parseBookTest(raw: unknown): BookTest {
  if (isRecord(raw) === false) fail("a test needs an object");
  return {
    name: optString(raw, "name", "a test"),
    path: optNullableString(raw, "path", "a test"),
    file: optString(raw, "file", "a test"),
    line: optNullableNumber(raw, "line", "a test"),
    confirmed: optBoolean(raw, "confirmed", "a test"),
    passing: optNullableBoolean(raw, "passing", "a test"),
    browser: optBoolean(raw, "browser", "a test"),
  };
}

function parseBookFinding(raw: unknown): BookFinding {
  if (isRecord(raw) === false) fail("a finding needs an object");
  return {
    code: optNullableString(raw, "code", "a finding"),
    tier: optNullableString(raw, "tier", "a finding"),
    level: optNullableString(raw, "level", "a finding"),
    message: optString(raw, "message", "a finding"),
    messageHtml: optString(raw, "messageHtml", "a finding"),
    covered: optBoolean(raw, "covered", "a finding"),
    outcome: raw["outcome"],
    reason: raw["reason"],
    path: optNullableString(raw, "path", "a finding"),
    line: optNullableNumber(raw, "line", "a finding"),
  };
}

function parseBookSection(raw: unknown): BookSection {
  if (isRecord(raw) === false) fail("a section needs an object");
  return {
    h: optString(raw, "h", "a section"),
    html: optString(raw, "html", "a section"),
  };
}

function parseBookSections(value: unknown): BookSections | null | undefined {
  if (value === undefined) return undefined;
  if (value === null) return null;
  if (isRecord(value) === false) fail("sections need an object");
  const listValue = value["list"];
  let list: BookSection[] | undefined;
  if (listValue !== undefined) {
    if (Array.isArray(listValue) === false) fail("sections need a list");
    list = listValue.map((item) => parseBookSection(item));
  }
  return {
    intro: optString(value, "intro", "sections"),
    list,
  };
}

/** One record of a records pack, checked once at load. */
export function parseBookRecord(id: string, raw: unknown): BookRecord {
  if (isRecord(raw) === false) fail("record " + id + " needs an object");
  const what = "record " + id;
  const fieldsValue = raw["fields"];
  if (isRecord(fieldsValue) === false) fail(what + " needs a fields object");

  const layerValue = raw["layer"];
  let layer: "intent" | "behavior" | "implementation" | "proof" | null | undefined;
  if (layerValue === undefined) layer = undefined;
  else if (layerValue === null) layer = null;
  else if (isLayerKey(layerValue)) layer = layerValue;
  else layer = null;

  const lacksValue = raw["lacks"];
  let lacks: string[] | undefined;
  if (lacksValue === undefined) lacks = undefined;
  else if (typeof lacksValue === "string") lacks = [lacksValue];
  else lacks = reqStringArray(lacksValue, what + ".lacks");

  const captureValue = raw["captureState"];
  let captureState: CaptureState | undefined;
  if (captureValue !== undefined) {
    if (isCaptureState(captureValue) === false) fail(what + " needs a captureState word");
    captureState = captureValue;
  }

  const proofValue = raw["proof"];
  let proof: BookRecord["proof"];
  if (proofValue !== undefined) {
    if (isRecord(proofValue) === false) fail(what + " needs a proof object");
    const stateValue = proofValue["state"];
    let state: ProofState | null | undefined;
    if (stateValue === undefined) state = undefined;
    else if (stateValue === null) state = null;
    else if (isProofState(stateValue)) state = stateValue;
    else fail(what + " needs a proof state word");
    const testValue = proofValue["test"];
    let test: { name?: string; path?: string | null; line?: number | null } | null | undefined;
    if (testValue === undefined) test = undefined;
    else if (testValue === null) test = null;
    else {
      if (isRecord(testValue) === false) fail(what + " needs a proof test object");
      test = {
        name: optString(testValue, "name", what),
        path: optNullableString(testValue, "path", what),
        line: optNullableNumber(testValue, "line", what),
      };
    }
    proof = {
      state,
      tests: optNumber(proofValue, "tests", what),
      test,
    };
  }

  const completionValue = raw["completion"];
  let completion: BookRecord["completion"];
  if (completionValue === undefined) completion = undefined;
  else if (completionValue === null) completion = null;
  else {
    if (isRecord(completionValue) === false) fail(what + " needs a completion object");
    const scenariosValue = completionValue["scenarios"];
    let scenarios: { pass?: number; of?: number } | null | undefined;
    if (scenariosValue === undefined) scenarios = undefined;
    else if (scenariosValue === null) scenarios = null;
    else {
      if (isRecord(scenariosValue) === false) fail(what + " needs a completion scenarios object");
      scenarios = {
        pass: optNumber(scenariosValue, "pass", what),
        of: optNumber(scenariosValue, "of", what),
      };
    }
    completion = {
      hasCode: optNullableBoolean(completionValue, "hasCode", what),
      hasTests: optNullableBoolean(completionValue, "hasTests", what),
      scenarios,
    };
  }

  const machineValue = raw["machine"];
  let machine: BookRecord["machine"];
  if (machineValue !== undefined) {
    if (isRecord(machineValue) === false) fail(what + " needs a machine object");
    machine = {
      coded: optBoolean(machineValue, "coded", what),
      verified: optBoolean(machineValue, "verified", what),
      met: optBoolean(machineValue, "met", what),
      measurePassed: optNullableBoolean(machineValue, "measurePassed", what),
      tested: optBoolean(machineValue, "tested", what),
    };
  }

  const implValue = raw["impl"];
  let impl: BookRecord["impl"];
  if (implValue !== undefined) {
    if (Array.isArray(implValue) === false) fail(what + " needs an impl list");
    impl = [];
    for (const item of implValue) {
      if (isRecord(item) === false) fail(what + " needs impl objects");
      impl.push({
        path: optString(item, "path", what),
        line: optNullableNumber(item, "line", what),
        kind: optString(item, "kind", what),
      });
    }
  }

  const testsValue = raw["tests"];
  let tests: BookTest[] | undefined;
  if (testsValue !== undefined) {
    if (Array.isArray(testsValue) === false) fail(what + " needs a tests list");
    tests = testsValue.map((item) => parseBookTest(item));
  }

  const expectValue = raw["expect"];
  let expect: BookRecord["expect"];
  if (expectValue !== undefined) {
    if (isRecord(expectValue) === false) fail(what + " needs an expect object");
    expect = {
      impl: optBoolean(expectValue, "impl", what),
      proof: optBoolean(expectValue, "proof", what),
    };
  }

  const findingsValue = raw["findings"];
  let findings: BookFinding[] | undefined;
  if (findingsValue !== undefined) {
    if (Array.isArray(findingsValue) === false) fail(what + " needs a findings list");
    findings = findingsValue.map((item) => parseBookFinding(item));
  }

  const keptByValue = raw["keptBy"];
  let keptBy: Record<string, number> | undefined;
  if (keptByValue !== undefined) keptBy = reqNumberRecord(keptByValue, what + ".keptBy");

  const fieldsHtmlValue = raw["fieldsHtml"];
  let fieldsHtml: Record<string, string> | undefined;
  if (fieldsHtmlValue !== undefined) fieldsHtml = reqStringRecord(fieldsHtmlValue, what + ".fieldsHtml");

  const routeValue = raw["route"];
  let route: BookRecord["route"];
  if (routeValue === undefined) route = undefined;
  else if (routeValue === null) route = null;
  else {
    if (isRecord(routeValue) === false) fail(what + " needs a route object");
    const surfacesValue = routeValue["surfaces"];
    let surfaces: string[] | undefined;
    if (surfacesValue !== undefined) surfaces = reqStringArray(surfacesValue, what + ".route.surfaces");
    route = {
      path: optString(routeValue, "path", what),
      surfaces,
      frontend: optString(routeValue, "frontend", what),
      route: optString(routeValue, "route", what),
      file: optString(routeValue, "file", what),
      reader: optString(routeValue, "reader", what),
    };
  }

  const stepsValue = raw["steps"];
  let steps: BookRecord["steps"];
  if (stepsValue !== undefined) {
    if (Array.isArray(stepsValue) === false) fail(what + " needs a steps list");
    steps = [];
    for (const item of stepsValue) {
      if (isRecord(item) === false) fail(what + " needs step objects");
      steps.push({
        text: optString(item, "text", what),
        serves: optStringArray(item, "serves", what),
      });
    }
  }

  const capturesValue = raw["captures"];
  let captures: BookRecord["captures"];
  if (capturesValue !== undefined) {
    if (Array.isArray(capturesValue) === false) fail(what + " needs a captures list");
    captures = [];
    for (const item of capturesValue) {
      if (isRecord(item) === false) fail(what + " needs capture objects");
      const pathValue = item["path"];
      if (typeof pathValue !== "string") fail(what + " needs a capture path string");
      captures.push({
        path: pathValue,
        bookPath: optString(item, "bookPath", what),
        theme: optString(item, "theme", what),
        width: optNumber(item, "width", what),
        capturedOn: optString(item, "capturedOn", what),
      });
    }
  }

  const intentCoverageValue = raw["intentCoverage"];
  let intentCoverage: BookRecord["intentCoverage"];
  if (intentCoverageValue !== undefined) {
    if (isRecord(intentCoverageValue) === false) fail(what + " needs an intentCoverage object");
    const reachedValue = intentCoverageValue["reached"];
    let reached: boolean | number | undefined;
    if (reachedValue === undefined) reached = undefined;
    else if (typeof reachedValue === "boolean" || typeof reachedValue === "number") reached = reachedValue;
    else fail(what + " needs an intentCoverage reached boolean or number");
    const coverageStepsValue = intentCoverageValue["steps"];
    let coverageSteps: { id?: string; reached?: boolean; reachedBy?: string[] }[] | undefined;
    if (coverageStepsValue !== undefined) {
      if (Array.isArray(coverageStepsValue) === false) fail(what + " needs intentCoverage steps");
      coverageSteps = [];
      for (const item of coverageStepsValue) {
        if (isRecord(item) === false) fail(what + " needs intentCoverage step objects");
        coverageSteps.push({
          id: optString(item, "id", what),
          reached: optBoolean(item, "reached", what),
          reachedBy: optStringArray(item, "reachedBy", what),
        });
      }
    }
    intentCoverage = {
      reached,
      total: optNumber(intentCoverageValue, "total", what),
      reachedBy: optStringArray(intentCoverageValue, "reachedBy", what),
      steps: coverageSteps,
    };
  }

  const whyValue = raw["why"];
  let why: BookRecord["why"];
  if (whyValue !== undefined) {
    if (Array.isArray(whyValue) === false) fail(what + " needs a why list");
    why = [];
    for (const item of whyValue) {
      if (isRecord(item) === false) fail(what + " needs why objects");
      const linksValue = item["links"];
      let links: { id?: string; kind?: string }[] | undefined;
      if (linksValue !== undefined) {
        if (Array.isArray(linksValue) === false) fail(what + " needs why links");
        links = [];
        for (const link of linksValue) {
          if (isRecord(link) === false) fail(what + " needs why link objects");
          links.push({
            id: optString(link, "id", what),
            kind: optString(link, "kind", what),
          });
        }
      }
      why.push({
        text: optString(item, "text", what),
        links,
      });
    }
  }

  return {
    type: reqString(raw, "type", what),
    title: reqString(raw, "title", what),
    folder: optString(raw, "folder", what),
    module: optNullableString(raw, "module", what),
    fields: fieldsValue,
    layer,
    src: parseSourceRef(raw["src"], what),
    impl,
    tests,
    expect,
    lacks,
    completion,
    findings,
    proof,
    machine,
    reachable: optStringArray(raw, "reachable", what),
    keptBy,
    intentCoverage,
    why,
    out: parseEdgeList(raw["out"], what + ".out"),
    in: parseEdgeList(raw["in"], what + ".in"),
    bodyHtml: optString(raw, "bodyHtml", what),
    sections: parseBookSections(raw["sections"]),
    fieldsHtml,
    requires: optStringArray(raw, "requires", what),
    calls: optStringArray(raw, "calls", what),
    route,
    steps,
    stepServers: optStringArrayRecord(raw, "stepServers", what),
    stepScreens: optStringArrayRecord(raw, "stepScreens", what),
    captures,
    captureState,
    evidenceStrength: optString(raw, "evidenceStrength", what),
    parent: optNullableString(raw, "parent", what),
    results: optStringArray(raw, "results", what),
    scenarios: optStringArray(raw, "scenarios", what),
    rules: optStringArray(raw, "rules", what),
    events: optStringArray(raw, "events", what),
    screens: optStringArray(raw, "screens", what),
    shownBy: optStringArray(raw, "shownBy", what),
  };
}

/** One records pack: the records by id. */
export function parseRecordPack(raw: unknown): Record<string, BookRecord> {
  if (isRecord(raw) === false) fail("a records pack needs an object");
  const out: Record<string, BookRecord> = {};
  for (const [id, item] of Object.entries(raw)) {
    out[id] = parseBookRecord(id, item);
  }
  return out;
}

function parseOutlineNode(raw: unknown): OutlineNode {
  if (isRecord(raw) === false) fail("an outline node needs an object");
  const kindValue = raw["kind"];
  if (isOutlineKind(kindValue) === false) fail("an outline node needs a kind word");
  const childrenValue = raw["children"];
  if (Array.isArray(childrenValue) === false) fail("an outline node needs children");
  return {
    id: reqString(raw, "id", "an outline node"),
    kind: kindValue,
    label: reqString(raw, "label", "an outline node"),
    type: optNullableString(raw, "type", "an outline node"),
    children: childrenValue.map((item) => parseOutlineNode(item)),
    shard: optString(raw, "shard", "an outline node"),
  };
}

/** One outline shard: the nodes of one layer. */
export function parseOutlineNodes(raw: unknown): OutlineNode[] {
  if (Array.isArray(raw) === false) fail("an outline shard needs a list");
  return raw.map((item) => parseOutlineNode(item));
}

/** One search shard: the rows with id, title, type and excerpt. */
export function parseSearchRows(raw: unknown): SearchRow[] {
  if (Array.isArray(raw) === false) fail("a search shard needs a list");
  const out: SearchRow[] = [];
  for (const item of raw) {
    if (Array.isArray(item) === false) fail("a search row needs a list");
    const row: string[] = [];
    for (const cell of item) {
      if (typeof cell !== "string") fail("a search row needs string cells");
      row.push(cell);
    }
    out.push(row);
  }
  return out;
}

/** The security shard: the items by kind. */
export function parseSecurityItems(raw: unknown): SecurityItem[] {
  if (Array.isArray(raw) === false) fail("the security shard needs a list");
  return raw.map((item) => {
    if (isRecord(item) === false) fail("a security item needs an object");
    const kind = item["kind"];
    if (kind !== "cross" && kind !== "calls" && kind !== "data") fail("a security item needs a kind word");
    return {
      kind,
      id: reqString(item, "id", "a security item"),
      text: reqString(item, "text", "a security item"),
    };
  });
}

function parseLexiconType(raw: unknown, key: string): LexiconType {
  if (isRecord(raw) === false) fail("lexicon type " + key + " needs an object");
  return {
    label: reqString(raw, "label", "lexicon type " + key),
    plural: reqString(raw, "plural", "lexicon type " + key),
    layer: reqString(raw, "layer", "lexicon type " + key),
    question: reqString(raw, "question", "lexicon type " + key),
  };
}

function parseLexiconLayer(raw: unknown, key: string): LexiconLayer {
  if (isRecord(raw) === false) fail("lexicon layer " + key + " needs an object");
  return {
    label: reqString(raw, "label", "lexicon layer " + key),
    question: optString(raw, "question", "lexicon layer " + key),
    writes: reqString(raw, "writes", "lexicon layer " + key),
  };
}

function parseLexiconLink(raw: unknown): LexiconLink {
  if (isRecord(raw) === false) fail("a lexicon link needs an object");
  const carriers = raw["carriers"];
  if (typeof carriers !== "string" && Array.isArray(carriers) === false) fail("a lexicon link needs carriers");
  if (Array.isArray(carriers)) {
    for (const item of carriers) {
      if (typeof item !== "string") fail("a lexicon link needs carrier strings");
    }
  }
  const carrierList: string[] | string = typeof carriers === "string" ? carriers : reqStringArray(carriers, "a lexicon link");
  return {
    field: reqString(raw, "field", "a lexicon link"),
    carriers: carrierList,
    forward: reqString(raw, "forward", "a lexicon link"),
    inverse: reqString(raw, "inverse", "a lexicon link"),
  };
}

function parseLexiconSection(raw: unknown, key: string): LexiconSection {
  if (isRecord(raw) === false) fail("lexicon section " + key + " needs an object");
  return {
    text: reqString(raw, "text", "lexicon section " + key),
    source: reqString(raw, "source", "lexicon section " + key),
  };
}

/** The lexicon shard: types, layers, links, sections, glossary, fields and findings. */
export function parseLexicon(raw: unknown): Lexicon {
  if (isRecord(raw) === false) fail("the lexicon needs an object");
  const typesValue = raw["types"];
  const layersValue = raw["layers"];
  const linksValue = raw["links"];
  const sectionsValue = raw["sections"];
  const glossaryValue = raw["glossary"];
  const fieldsValue = raw["fields"];
  const findingsValue = raw["findings"];
  if (isRecord(typesValue) === false) fail("the lexicon needs types");
  if (isRecord(layersValue) === false) fail("the lexicon needs layers");
  if (Array.isArray(linksValue) === false) fail("the lexicon needs links");
  if (isRecord(sectionsValue) === false) fail("the lexicon needs sections");
  if (isRecord(glossaryValue) === false) fail("the lexicon needs a glossary");
  if (isRecord(fieldsValue) === false) fail("the lexicon needs fields");
  if (isRecord(findingsValue) === false) fail("the lexicon needs findings");

  const types: Record<string, LexiconType> = {};
  for (const [key, item] of Object.entries(typesValue)) {
    types[key] = parseLexiconType(item, key);
  }
  const layers: Record<string, LexiconLayer> = {};
  for (const [key, item] of Object.entries(layersValue)) {
    layers[key] = parseLexiconLayer(item, key);
  }
  const sections: Record<string, LexiconSection> = {};
  for (const [key, item] of Object.entries(sectionsValue)) {
    sections[key] = parseLexiconSection(item, key);
  }
  const glossary: Record<string, { term: string; definition: string }> = {};
  for (const [key, item] of Object.entries(glossaryValue)) {
    if (isRecord(item) === false) fail("glossary term " + key + " needs an object");
    glossary[key] = {
      term: reqString(item, "term", "glossary term " + key),
      definition: reqString(item, "definition", "glossary term " + key),
    };
  }
  const fields: Record<string, string> = {};
  for (const [key, item] of Object.entries(fieldsValue)) {
    if (typeof item !== "string") fail("lexicon field " + key + " needs a label");
    fields[key] = item;
  }
  const findings: Record<string, { level: string; meaning: string }> = {};
  for (const [key, item] of Object.entries(findingsValue)) {
    if (isRecord(item) === false) fail("finding code " + key + " needs an object");
    findings[key] = {
      level: reqString(item, "level", "finding code " + key),
      meaning: reqString(item, "meaning", "finding code " + key),
    };
  }
  return {
    types,
    layers,
    links: linksValue.map((item) => parseLexiconLink(item)),
    sections,
    glossary,
    fields,
    findings,
  };
}

function parseIntentCoverageEntry(raw: unknown): IntentCoverageEntry {
  if (isRecord(raw) === false) fail("an Intent coverage entry needs an object");
  const kind = raw["kind"];
  if (kind !== "requirement" && kind !== "step") fail("an Intent coverage entry needs a kind word");
  const reached = raw["reached"];
  if (typeof reached !== "boolean") fail("an Intent coverage entry needs a reached boolean");
  return {
    id: reqString(raw, "id", "an Intent coverage entry"),
    kind,
    reached,
    reachedBy: reqStringArray(raw["reachedBy"], "an Intent coverage entry"),
  };
}

function parseIntentCoverage(raw: unknown): IntentCoverage {
  if (isRecord(raw) === false) fail("the Intent coverage needs an object");
  const requirementsValue = raw["requirements"];
  const stepsValue = raw["steps"];
  const entriesValue = raw["entries"];
  if (isRecord(requirementsValue) === false) fail("the Intent coverage needs requirements");
  if (isRecord(stepsValue) === false) fail("the Intent coverage needs steps");
  if (Array.isArray(entriesValue) === false) fail("the Intent coverage needs entries");
  const requirementsReached = requirementsValue["reached"];
  const requirementsTotal = requirementsValue["total"];
  const stepsReached = stepsValue["reached"];
  const stepsTotal = stepsValue["total"];
  const reached = raw["reached"];
  const total = raw["total"];
  const share = raw["share"];
  if (typeof reached !== "number") fail("the Intent coverage needs a reached number");
  if (typeof total !== "number") fail("the Intent coverage needs a total number");
  if (typeof share !== "number") fail("the Intent coverage needs a share number");
  if (typeof requirementsReached !== "number") fail("the Intent coverage needs requirement counts");
  if (typeof requirementsTotal !== "number") fail("the Intent coverage needs requirement counts");
  if (typeof stepsReached !== "number") fail("the Intent coverage needs step counts");
  if (typeof stepsTotal !== "number") fail("the Intent coverage needs step counts");
  return {
    reached,
    total,
    share,
    requirements: { reached: requirementsReached, total: requirementsTotal },
    steps: { reached: stepsReached, total: stepsTotal },
    entries: entriesValue.map((item) => parseIntentCoverageEntry(item)),
  };
}

function parseLayerClaim(raw: unknown): LayerClaim {
  if (isRecord(raw) === false) fail("a layer claim needs an object");
  return {
    layer: reqString(raw, "layer", "a layer claim"),
    practice: reqString(raw, "practice", "a layer claim"),
  };
}

/** The manifest shard: project, roots, editor, claims, coverage, layers and shard map. */
export function parseManifest(raw: unknown): Manifest {
  if (isRecord(raw) === false) fail("the manifest needs an object");
  const projectValue = raw["project"];
  const rootsValue = raw["roots"];
  const layersValue = raw["layers"];
  const totalsValue = raw["totals"];
  const shardOfValue = raw["shardOf"];
  if (isRecord(projectValue) === false) fail("the manifest needs a project");
  if (isRecord(rootsValue) === false) fail("the manifest needs roots");
  if (Array.isArray(layersValue) === false) fail("the manifest needs layers");
  if (isRecord(totalsValue) === false) fail("the manifest needs totals");
  if (isRecord(shardOfValue) === false) fail("the manifest needs a shard map");

  const layers: { key: string; label: string; outline: string[] }[] = [];
  for (const item of layersValue) {
    if (isRecord(item) === false) fail("a manifest layer needs an object");
    layers.push({
      key: reqString(item, "key", "a manifest layer"),
      label: reqString(item, "label", "a manifest layer"),
      outline: reqStringArray(item["outline"], "a manifest layer"),
    });
  }

  const totals: Record<string, number> = {};
  for (const [key, item] of Object.entries(totalsValue)) {
    if (typeof item !== "number") fail("manifest total " + key + " needs a number");
    totals[key] = item;
  }

  const shardOf: Record<string, string> = {};
  for (const [key, item] of Object.entries(shardOfValue)) {
    if (typeof item !== "string") fail("shard map entry " + key + " needs a shard key");
    shardOf[key] = item;
  }

  const claimsValue = raw["claims"];
  let claims: LayerClaim[] | undefined;
  if (claimsValue !== undefined) {
    if (Array.isArray(claimsValue) === false) fail("the manifest needs a claims list");
    claims = claimsValue.map((item) => parseLayerClaim(item));
  }

  const coverageValue = raw["intentCoverage"];
  let intentCoverage: IntentCoverage | undefined;
  if (coverageValue !== undefined) intentCoverage = parseIntentCoverage(coverageValue);

  const testRunValue = raw["testRun"];
  let testRun: { date?: string; passed?: number; failed?: number; total?: number } | null | undefined;
  if (testRunValue === undefined) testRun = undefined;
  else if (testRunValue === null) testRun = null;
  else {
    if (isRecord(testRunValue) === false) fail("the manifest needs a test run object");
    testRun = {
      date: optString(testRunValue, "date", "the test run"),
      passed: optNumber(testRunValue, "passed", "the test run"),
      failed: optNumber(testRunValue, "failed", "the test run"),
      total: optNumber(testRunValue, "total", "the test run"),
    };
  }

  return {
    project: {
      id: reqString(projectValue, "id", "the manifest project"),
      title: reqString(projectValue, "title", "the manifest project"),
    },
    roots: {
      consumer: reqString(rootsValue, "consumer", "the manifest roots"),
      litespec: reqString(rootsValue, "litespec", "the manifest roots"),
    },
    editor: reqString(raw, "editor", "the manifest"),
    claims,
    intentCoverage,
    testRun,
    totals,
    layers,
    shardOf,
    search: reqStringArray(raw["search"], "the manifest search"),
    unplaced: reqStringArray(raw["unplaced"], "the manifest unplaced"),
    generatedOn: reqString(raw, "generatedOn", "the manifest"),
  };
}
