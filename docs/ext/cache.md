# Caching

**Stack:** any

**Concern:** infrastructure

A cache answers repeated expensive reads from stored results instead of recomputing them, trading lower latency and source load for stale reads and invalidation work.

This page states when that trade is justified, what each cache's staleness contract covers, how keys separate results, and how the system behaves when the cache fails.

A team applies it once traces or load tests name a repeated expensive read, so each cache carries a staleness bound the team chose instead of one it discovers during an incident.

## Activation

Activation scope: `local`.

Applicable specification kinds: `use-case`.

Select `cache` for a use case after traces or load tests show a repeated expensive read whose use case states how stale its answer may be. Expected future traffic alone leaves the extension unselected.

## Baseline Relationship

This extension replaces no baseline provision. It selects no provider package: a project that adds one records the decision and pins its version in the manifest.

It switches on the `setting` record type. Each cache expiry is a setting record, and the accepted staleness is the served query's `freshness`.

## Standards

### Record Measured Cache Need

**ID:** `litespec/ext/cache#record-measured-cache-need`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A cache proposal MUST record its observed query, representative load, latency, source cost, and target.

**Rationale:** A cache costs invalidation, capacity, and recovery work for as long as it lives. The proposal records the observed query, representative load, latency, source cost, and target so a reviewer weighs that cost against measured gain. A cache added without measurements stays until someone pays to remove it.

**Reviewed for:** Does every cache proposal record its observed query, load, latency, cost, and target? Read the proposal behind each cache.

### State the Staleness Each Cached Query Accepts

**ID:** `litespec/ext/cache#state-the-staleness-each-cached-query-accepts`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A cached query MUST state its accepted staleness as a `freshness` object with `mode` of `eventual` and its duration in `within`.

**Rationale:** Accepted staleness is the contract each caller relies on: it states how stale a read may be before it counts as a defect. The served query states it on its output as an eventual freshness object such as `{"mode": "eventual", "within": "PT5M"}`, so the cache and the query share one bound that is reviewed with the use case.

**Reviewed for:** Does every cached query carry an eventual freshness object with its duration on its output? Check the served query beside each cache.

### Remove Unjustified Caches

**ID:** `litespec/ext/cache#remove-unjustified-caches`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A cache owner MUST remove a cache when measurements do not justify its operating cost.

**Rationale:** A cache earns its place only while the measurements justify its operating cost. Traffic shifts, sources get faster, and a cache that paid for itself last year can cost more than it saves today. The owner rechecks the measurements and removes the cache when they do not justify its operating cost. A cache nobody owns stays forever, and its invalidation bugs stay with it.

**Reviewed for:** Has any cache outlived the measurements that justified its operating cost? Read the owner reviews beside the operating cost.

### Compose Cache Keys from Result Inputs

**ID:** `litespec/ext/cache#compose-cache-keys-from-result-inputs`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A cache key MUST include its data-shape version, every result-affecting input, and applicable tenant or actor partition.

**Rationale:** A key without the data-shape version serves old shapes to new callers after a deploy. A key without a result-affecting input serves one caller's answer to a different request. Tenant or actor partition keeps one party's results away from another's.

**Example:** The key `post-summary:v2:tenant-42:post-17` names the data-shape version, the tenant, and the post, so a new shape, another tenant, or another post reads a different entry.

**Reviewed for:** Does every key carry the data-shape version, each result-affecting input, and the tenant or actor partition? Read the key construction.

### Exclude Unsafe Cache Key Material

**ID:** `litespec/ext/cache#exclude-unsafe-cache-key-material`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A cache key MUST NOT contain a secret, unbounded raw input, or personal data that is not already a hash.

**Rationale:** Keys appear in diagnostics, metrics, and provider administration tools. That copies the value into a store no one classified. An email address in a key is a personal-data record in the cache provider's key space. It sits outside every retention and erasure path the project defined. [Data minimization](https://gdpr-info.eu/art-5-gdpr/) applies to an identifier as much as to a field.

**Example:** A per-account cache key carries the account's opaque identifier. Where a key would carry the address, it carries a hash of it instead, and the entry stays bound to the account that owns it.

**Reviewed for:** Does any key carry a secret, unbounded input, or unhashed personal data? Read the key construction.

### Define Cache Invalidation

**ID:** `litespec/ext/cache#define-cache-invalidation`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A cached value MUST define expiry as a setting record, invalidating write events, an invalidation owner, and behavior after invalidation failure.

**Rationale:** Every cached value goes stale, and the only choice is whether the project decides the behavior up front or discovers it during an incident. Expiry, invalidating write events, an owner, and failure behavior state how long staleness lasts and who fixes it when invalidation fails. A cache without that contract serves stale data on a schedule nobody chose.

**Reviewed for:** Does every cached value define expiry, invalidating events, an owner, and failure behavior? Read the invalidation contract of each cache.

### Record Each Manual Cache Invalidation

**ID:** `litespec/ext/cache#record-each-manual-cache-invalidation`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A manual invalidation MUST produce a record naming actor, invalidation scope, and reason, as an audit record in its category when the audit extension is selected.

**Rationale:** A manual clear changes what every later caller sees, and the data itself shows no trace of it. When a stale-read report arrives later, the investigation has to know whether someone already cleared the entry. [Article 30 of the GDPR](https://gdpr-info.eu/art-30-gdpr/) treats an administrative action over personal data as processing that has a record.

**Example:** An operator clearing one tenant's cached price list during an incident leaves a record naming the tenant, the operator, and the incident.

**Reviewed for:** Does every manual clear leave a record naming actor, scope, and reason? Read the invalidation runs.

### Prefer Bounded Staleness

**ID:** `litespec/ext/cache#prefer-bounded-staleness`

**Strength:** advisory

**Applies to:** artifact: any

**Statement:** A cache owner SHOULD use a short expiry when precise invalidation costs more than recomputation.

**Deviation:** A measured staleness and operating-cost decision permits a longer expiry.

**Rationale:** Precise invalidation costs design and code, while a short expiry costs one recomputation per window. When recomputation is cheaper, a short expiry bounds staleness without the machinery. A longer expiry then needs a measured decision showing the extra staleness is worth what it saves.

**Reviewed for:** Does every long expiry rest on a measured staleness and operating-cost decision? Compare the expiry with the invalidation cost.

### Keep Source Data Authoritative

**ID:** `litespec/ext/cache#keep-source-data-authoritative`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A cache implementation MUST keep required business data authoritative in its source of record.

**Rationale:** The cache holds a derived copy that the system can drop at any time. Required business data stays authoritative in its source of record, so losing the cache loses speed and nothing else. A cache that owns the only copy turns every cache outage into data loss.

**Reviewed for:** Does any required business data live authoritatively in the cache instead of its source of record? Compare the source of record with the cached copy.

### Define Cache Outage Behavior

**ID:** `litespec/ext/cache#define-cache-outage-behavior`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A cache outage MUST use the source when capacity permits or return a documented degraded response.

**Rationale:** Cache infrastructure fails like any other dependency. The outage behavior states whether callers fall back to the source or receive a documented degraded response, so an outage degrades the product on purpose instead of by accident.

**Reviewed for:** Does every cache document a source fallback or a degraded response? Read the outage behavior of each cache.

### Distinguish Cache Misses, Errors, and Degraded Responses

**ID:** `litespec/ext/cache#distinguish-cache-misses-errors-and-degraded-responses`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A cache path MUST emit a metric distinguishing a cache miss, a cache error, and a degraded response.

**Rationale:** A fallback that works is invisible. An outage runs until someone notices the source load instead. The three cases need different responses: a miss is normal, an error is an infrastructure fault, and a degraded response is a product behavior change. One counter for all three answers none of them.

**Example:** The metric carries the cache name and the outcome, following [the OpenTelemetry semantic conventions](https://opentelemetry.io/docs/specs/semconv/) for the client it wraps.

**Reviewed for:** Does any cache path share one counter across misses, errors, and degraded responses? Read the emitted metrics.

### Bound Cache Refresh Work

**ID:** `litespec/ext/cache#bound-cache-refresh-work`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A cache implementation MUST bound simultaneous refresh work when concurrent misses could overload its source.

**Rationale:** When a popular entry expires, every concurrent miss can trigger its own refresh and flood the source the cache was protecting. Bounding simultaneous refresh work holds that flood to what the source tolerates. Request coalescing and jittered expiry are two ways to meet the bound.

**Example:** One request refreshes a key while concurrent callers await the same bounded work.

**Reviewed for:** Does any cache refresh without bound on concurrent misses? Read the refresh path under concurrent load.

### Avoid Unbounded Key Locks

**ID:** `litespec/ext/cache#avoid-unbounded-key-locks`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A cache implementation MUST NOT create an unbounded per-key lock inventory.

**Rationale:** Per-key locks trade duplicate refreshes for memory, and that trade needs a ceiling. Untrusted or high-cardinality keys create one lock entry each, which exhausts memory under attacker-chosen or runaway keys. A bounded lock scheme sheds that state instead of growing it.

**Reviewed for:** Does any lock scheme grow one entry per key without bound? Read the lock state under high-cardinality keys.

## Conventions

### Place Cache Access at an Outer Boundary

**ID:** `litespec/ext/cache#place-cache-access-at-an-outer-boundary`

**Strength:** default

**Applies to:** artifact: any

**Default:** Place cache access in Infrastructure or a frontend data boundary.

**Replacement:** A consumer records a cache-placement convention naming where its cache access lives.

**Rationale:** Cache access lives in Infrastructure or a frontend data boundary, behind a port the use case owns when Application needs one. Domain code takes no cache dependency, so business rules read the same with or without a cache. A cache concern inside Domain would couple every rule to the cache's presence.

**Reviewed for:** Does any cache access live inside Domain, or outside the recorded local convention? Read the cache call sites.

## Dependencies

Packages: None.

Related extensions: `audit` for manual invalidation records.

Guides: None.
