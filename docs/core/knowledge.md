# Knowledge

**Stack:** any
**Concern:** domain

This page states how the knowledge layer carries the rules that operational records consult: rates, limits, deadlines, and treatments varying by country or branch. It is for agents who model knowledge areas and for reviewers who check that sets carry values and models carry behavior. A rate read from the wrong place pays the wrong amount with no record of why.

## Concepts

### The Knowledge Level and the Operational Level

The operational level carries aggregates and the use cases changing them. The knowledge level carries the rules those use cases consult: a rate, a limit, a deadline, or a template. An expense line is operational. The per-diem rate applied to its travel date is knowledge. The two levels change for different reasons. An operational record changes through a use case, while a knowledge row changes through a release or through an editor of the values. Keeping them apart lets a rate change without rewriting the code reading it.

### Knowledge Area

A knowledge area is one subject of rules, such as travel rates or loan rules. Its records live at `.litespec/behavior/knowledge/<area>/`, and a reference-data table under it carries the id `reference-data/<area>.<table>`. Where the values differ by a dimension, the table states `variation` with that dimension. Claimwick's `travel` area carries the per-diem and mileage tables and varies by `country`. The library lending area varies by `branch`.

### Knowledge Model

The knowledge model defines the types of one area: rows, closed sets, roles, typed keys, policy methods, the set contract, the runtime indexing a set and answering lookups by key or role and date, the registry, and the startup validation. The model owns the behavior. A module depends on the model, leaving sets to the hosts registering them. In Claimwick the model is `Claimwick.Travel`: it defines a per-diem row, the `FullDay` and `PartialDay` roles, and a lookup returning a refusal where no row covers the date.

Rows are read through domain values carrying the behavior their values decide. A row of free strings reads two ways in two callers, and a third caller adds a third reading. A domain value types what the table holds and carries each reading, so every caller decides the same way. Infrastructure loads rows and parses them into those values, and decides nothing from them.

### Knowledge Set

A knowledge set carries the values for one variant of an area: the rows, effective dates, and sources for one country or one branch. It builds rows through the model constructors and carries no logic of its own. Claimwick ships one set per country, such as `Claimwick.Travel.Germany` and `Claimwick.Travel.Netherlands`. The library ships one set per branch. Hosts register the sets they run, and modules leave sets alone.

### Variant Dimension

The variant dimension is the value selecting a set, such as `country` or `branch`. An area varying by nothing keeps its one table in the model and needs no set. A table whose rows differ by variant states `variation`, so a reader and a check see which dimension selects the values.

### Effectivity and Versions

Each row states the date from which it applies and the source behind its value. A released row stays unchanged. A new value arrives as a new row, and the old row takes an end date. Each set carries a version. An operational record keeps the key, value, and version it used, so a later rate change leaves the recorded decision intact. A Claimwick expense submitted under the 2026 per-diem row still shows the figure it used after a 2027 row takes effect.

### Templates

A template is a starting set of values, such as a starting chart or a default policy. Copying a template into an operational record happens once. The copy is operational from then on and changes through the record's own use cases. It stops following the template. In the library, a new branch starts from a template of loan limits. The clerk copies it when the branch opens. Later edits to the branch limits go through the branch's own use case, and a change to the template leaves that branch alone.

### What Is Not Knowledge

A per-tenant override is a setting on an operational aggregate. It belongs to one tenant and changes through that tenant's use case. The settings an adapter needs to connect to an outside system (endpoints, credentials, the wire format it parses) are adapter configuration. Neither belongs in a knowledge model or a set. Facts about outside providers that use cases read or screens show, such as which providers the product accepts and what it offers through them, are knowledge.

### Values Edited at Runtime

Values people edit while the product runs live on aggregates, not in released tables. A published table changes by release: its rows are written with the product and read as written. An aggregate changes by use case: it guards each change with rules, refusals, and access. A published table taking edits at runtime leaves those edits unguarded, and an aggregate carrying a table nobody edits promises guarding nothing performs. The aggregate sits on the owner editing the values, such as a tenant's own overrides, instead of on the published table. Claimwick's receipt threshold lives on the policy aggregate because managers change it through `use-case/spending.change-receipt-threshold`, while the decision target stays a reference table nobody edits at runtime.

### Values in Code or in Data

Values written as code travel with the release, compile with the model, and need no loader or schema of their own. A data format earns its place where the people owning the values are not developers, where a value changes between releases, or where another reader needs the same table. There the project records the format, such as JSON, YAML, or a database, and its home in its backend documentation. Claimwick's travel rows with their dates and sources live in the backend language and ship with each release. A table editors outside the team maintain between releases is recorded as a data format.

## Standards

### Give Each Knowledge Area One Model

**ID:** `litespec/core/knowledge#give-each-knowledge-area-one-model`

**Strength:** required

**Applies to:** artifact: knowledge-model, knowledge-set, record:knowledge-area, record:reference-data

**Statement:** A knowledge area MUST have one knowledge model and one knowledge set per variant of its dimension, keeping its one table in the model when it varies by nothing.

**Rationale:** One model gives every caller the same types and lookups, and one set per variant keeps values for a country or a branch apart without copying the types into each set. An area varying by nothing needs no set. A second shape for one table leaves readers unsure which carries the values.

**Example:** Claimwick's `travel` area has the `Claimwick.Travel` model and a set for each country its rates cover, while its approval-target table, varying by nothing, stays in the model.

**Checked by:** `litespec model check` reports `knowledge-set-missing` for a variant an area names with no set, and `knowledge-variation-undeclared` for rows that differ by variant without a declared dimension.

### Read Knowledge Through Its Model

**ID:** `litespec/core/knowledge#read-knowledge-through-its-model`

**Strength:** required

**Applies to:** artifact: knowledge-model, knowledge-set, command-handler, query-handler, reaction, job

**Statement:** A module MUST reach a knowledge area through its knowledge model, leaving a set to be referenced only by a host that registers it.

**Rationale:** A module naming a set ties its code to one country or branch, so adding a variant means editing the module. The model gives modules one stable surface, and a host registers each set with that model at startup. The model owns its registry, its composition and its startup validation, so it references the dependency-injection, logging and options libraries those need.

**Example:** Claimwick's spending module asks `Claimwick.Travel` for a per-diem role on the expense date and leaves country projects alone. The API host registers `Claimwick.Travel.Germany` and `Claimwick.Travel.Netherlands` with the model at startup.

**Checked by:** `dotnet build` reports `LS2038` for a module assembly referencing a knowledge set assembly: a project under `Knowledge/` named `<Product>.<Area>.<Variant>` of the same product that references its model, so another vendor's packages are out of scope. It reports `LS1001` for an outer-layer reference in a model outside the dependency-injection, logging and options libraries.

### Keep Knowledge Sets Free of Logic

**ID:** `litespec/core/knowledge#keep-knowledge-sets-free-of-logic`

**Strength:** required

**Applies to:** artifact: knowledge-model, knowledge-set

**Statement:** A knowledge set MUST hold only rows built through its model's constructors, with any behavior a variant needs added as a case in the model's closed set of strategies.

**Rationale:** A set deciding something splits one rule into one copy per variant, and the copies drift. Rows built through model constructors carry the model types and validation. Where a country or a branch needs a treatment data alone states poorly, a case in the model strategy set carries the rule and the set selects it, so every variant shares the same behavior.

**Example:** Claimwick's Germany and Netherlands sets list dated per-diem rows and nothing else. Where a country needs a different rounding of a partial day, the rule becomes a case in the model rounding strategy set, selected by the set's data.

**Checked by:** `dotnet build` reports `LS2039` for a method or property body in a knowledge set that does more than return or construct rows; a static `Parse`, `From`, `FromCode`, `Of` or `Create` call on a named type with only literal arguments, such as a typed key built from its text, counts as constructing.

### Read Knowledge by Role and Date

**ID:** `litespec/core/knowledge#read-knowledge-by-role-and-date`

**Strength:** required

**Applies to:** artifact: knowledge-model, knowledge-set, command-handler, query-handler, reaction, job

**Statement:** Operational code MUST read knowledge by role and date through the model's typed keys, naming no set and building no key from a string literal.

**Rationale:** A data key names one row of one set, so code naming it breaks where another set keys its rows differently. A role states what the caller needs, and each set says which row plays it. A typed key built through the model keeps the caller from spelling a country or a date into a raw string.

**Example:** Claimwick's expense submission asks the travel model for the `FullDay` per-diem role on the travel date. It builds the key through the model and leaves country projects and row keys alone. A literal such as `new TravelKey("DE")` is reported.

**Checked by:** `dotnet build` reports `LS2040` for a knowledge key built from a string literal outside a knowledge set.

### Name a Role Not a Row

**ID:** `litespec/core/knowledge#name-a-role-not-a-row`

**Strength:** required

**Applies to:** artifact: record:reference-data

**Statement:** Each reference-data table MUST list in `requiredRoles` the roles its citing rules read, with each role played by a row in `role`.

**Rationale:** A data key names one row of one data set, so code naming the key breaks where another data set keys its rows differently or not at all. A role names what the product needs, such as the standard target, and each data set says which row plays it.

**Example:** Claimwick's `reference-data/spending.decision-target` lists `requiredRoles: [standard]`, and both its rows play `standard`, so the approval rules read the standard target without naming a row.

**Checked by:** `litespec model check` reports `reference-data-role-missing` (level G) for a required role played by no row.

### Validate Reference Data Before Serving

**ID:** `litespec/core/knowledge#validate-reference-data-before-serving`

**Strength:** required

**Applies to:** artifact: record:reference-data

**Statement:** Each reference-data table MUST hold only rows that resolve their cross-references, stay within its closed set where one is listed, and play its required roles.

**Rationale:** A broken cross-reference, an unlisted value, or a missing role each fails at read time, far from the row causing it. Validating the whole table before serving it moves each failure to startup, naming the file and row. The host refuses to start until the tables it loads validate.

**Example:** A Claimwick row referring to `reference-data/spending.decision-target#target-2030` is reported, naming the file and row, before the product serves the table.

**Checked by:** `litespec model check` reports `reference-data-key-unknown`, `reference-data-value-invalid`, and `reference-data-role-missing` (level G) for a cross-reference to a missing key, a value outside `choices`, and a required role played by no row.

### Refuse a Missing Rule with a Reason

**ID:** `litespec/core/knowledge#refuse-a-missing-rule-with-a-reason`

**Strength:** required

**Applies to:** artifact: knowledge-model, command-handler, query-handler, record:use-case

**Statement:** A lookup with no row in force MUST end its use case in a refusal naming the missing role, date, and variant.

**Rationale:** A null or a default value spends a value no rule supports, with no record of why. A refusal with a reason points the operator at the missing row, and the use case records the refusal beside its other endings. The model derives that ending as `result/<use-case>.rule-not-stated`, and throws the product rule-not-stated refusal type deriving the kernel domain exception.

**Example:** Where no German per-diem row covers a travel date, Claimwick's submission ends in a refusal naming the `FullDay` role, that date, and Germany, instead of paying zero. Claimwick's travel model throws `TravelRuleMissingException`, deriving the kernel `DomainException` and carrying the code `travel.rule-missing`.

**Reviewed for:** Does any knowledge lookup return a null or a default when no row is in force, or end without naming the missing rule? Look at each lookup beside the use-case refusals.

### Check Shape at Compile Time and Dates at Startup

**ID:** `litespec/core/knowledge#check-shape-at-compile-time-and-dates-at-startup`

**Strength:** required

**Applies to:** artifact: knowledge-model, knowledge-set

**Statement:** Shape and cross-references in code MUST be checked at compile time, while overlaps, gaps, and coverage of the current period are checked at startup.

**Rationale:** Row types, roles, and references between model types are fixed by the code, and the compiler proves them on every build. Dates depend on the rows a set ships, so only loading the set proves that its periods overlap nowhere, leave no gap, and cover today. Startup is the first moment the whole set is known, and a failure there names the row before any request uses it.

**Example:** Claimwick's build proves that every per-diem row carries a country, an amount, dates, and a source. At startup the travel model rejects a German row overlapping another or leaving the current year uncovered, and the product stops serving until it is fixed. The rejection raises `InvalidOperationException`, as `TravelRegistry.Validate` does.

**Reviewed for:** Is any date rule left to a compile-time check, or any shape rule left to a startup check? Look at the build diagnostics beside the startup validation of each set.

## Conventions

None.
