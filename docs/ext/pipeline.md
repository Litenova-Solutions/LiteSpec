# Ingest Pipelines

## Intent

Some products ingest outside records through pipelines that drop what they cannot use, enrich what they keep, and buffer the flow between. This page states the ingest steps, the closed drop reasons, the buffer policy, and the freshness consequence readers observe. A project selects it for each job that ingests outside records.

## Activation

Activation scope: `local`.

Applicable specification kinds: `job`.

A project selects `pipeline` for a job when the job ingests outside records through drop and enrich steps. A job that drives product processes or sends notifications does not select it.

## Baseline Relationship

This extension replaces no baseline provision. It switches on no record type: it adds ingest detail to job records.

An ingest job lists its `steps` in run order, names its closed `dropReasons` set, and states its `buffer` policy. Each query reading ingested data states the freshness consequence its readers observe.

## Standards

### Declare Ingest Steps in Order

**ID:** `litespec/ext/pipeline#declare-ingest-steps-in-order`

**Statement:** An ingest job MUST list its steps in run order with each step marked drop or enrich.

**Rationale:** Order is the pipeline's meaning: enriching before dropping wastes work, and dropping before enriching loses context. Listing the steps in run order with each kind marked lets a reader replay the pipeline in their head.

**Example:** The catalog ingest lists `drop-duplicates`, then `enrich-prices`, then `drop-below-threshold`.

**Reviewed for:** The ingest job records list steps in run order with each marked drop or enrich.

### Define the Closed Drop Reason Set

**ID:** `litespec/ext/pipeline#define-the-closed-drop-reason-set`

**Statement:** An ingest job MUST name the closed set of reasons a step drops a record.

**Rationale:** A dropped record with no reason is indistinguishable from a lost one. A closed set forces each drop to say why in words the team chose, so operators count drops by reason instead of guessing.

**Example:** The catalog ingest names `duplicate`, `below-threshold`, and `poison` as its only drop reasons.

**Reviewed for:** The ingest job records name the closed drop reasons with no drop outside them.

### Record One Drop Reason per Dropped Record

**ID:** `litespec/ext/pipeline#record-one-drop-reason-per-dropped-record`

**Statement:** Each record a step drops MUST carry one reason from the closed set.

**Rationale:** The set alone promises nothing until each drop cites it. One reason on each dropped record turns the set into evidence: every drop answers why, in the team's own words.

**Example:** A feed row dropped for matching an existing SKU carries `duplicate`.

**Reviewed for:** The dropped records each carry one reason from the job's closed set.

### Read Enrichment Data from Declared Sources

**ID:** `litespec/ext/pipeline#read-enrichment-data-from-declared-sources`

**Statement:** An enrich step MUST read only the sources its job declares.

**Rationale:** Enrichment pulls outside data into product records: prices, categories, ratings. An undeclared source smuggles values no record explains, so each enrich step reads only the sources its job names.

**Example:** The `enrich-prices` step reads the vendor price list its job declares, and nothing else.

**Reviewed for:** The enrich steps read only the sources named on their job records.

### Bound the Ingest Buffer

**ID:** `litespec/ext/pipeline#bound-the-ingest-buffer`

**Statement:** An ingest job MUST state its buffer policy with capacity and overflow behavior.

**Rationale:** Feeds burst: a vendor resends a week of rows, a file lands twice. The buffer holds the surge, and the policy states how much it holds and what happens past that: block the feed, drop the oldest, or page an operator. Without the policy, the first surge picks the behavior.

**Example:** The catalog ingest buffers ten thousand rows and blocks the feed past that until the buffer drains.

**Reviewed for:** The ingest job records state buffer capacity with the overflow behavior past it.

### Expose Drop Counts per Reason

**ID:** `litespec/ext/pipeline#expose-drop-counts-per-reason`

**Statement:** An ingest job MUST expose a count of dropped records per reason.

**Rationale:** Drop counts are the pipeline's pulse: a spike in `poison` means a broken feed, a spike in `duplicate` means a double send. Per-reason counts turn each drop into a signal the team watches.

**Example:** The catalog dashboard shows twelve `poison` drops in the last hour beside zero `duplicate` drops.

**Reviewed for:** The pipeline dashboards show dropped-record counts split by reason.

### State the Freshness Consequence

**ID:** `litespec/ext/pipeline#state-the-freshness-consequence`

**Statement:** Each query reading ingested data MUST state the freshness its readers observe.

**Rationale:** Ingested data lags: the buffer, the batch window, and the enrich calls all take time. The query states what its readers observe, a bounded object with its window or an advisory object with its authority, so a stale price reads as declared behavior instead of a defect.

**Example:** The catalog search query states `{"mode": "bounded", "within": "PT15M"}`, matching the ingest job's quarter-hour window.

**Reviewed for:** The queries over ingested data state the freshness their readers observe.

## Conventions

### Discard Poison Records After Three Attempts

**ID:** `litespec/ext/pipeline#discard-poison-records-after-three-attempts`

**Default:** A record that fails parsing three times is dropped with the poison reason.

**Replacement:** A project names its poison threshold in its pipeline documentation.

**Rationale:** One bad row must not stall the feed forever, and one strict parse must not drop rows a retry would save. Three attempts split the difference, and the poison reason keeps the casualty visible.

**Reviewed for:** Unparsable rows drop as poison after three attempts, or the pipeline documentation names another threshold.

## Dependencies

Packages: None.

Related extensions: `jobs` for the lease, cursor, and per-item issues behind the ingest job.

Guides: None.
