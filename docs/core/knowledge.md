# Knowledge

**Stack:** any
**Concern:** domain

The knowledge level holds the rules that govern how operational records behave: rates, limits, deadlines, and treatments that vary by a dimension such as a country or a branch. Claimwick keeps per-diem and mileage rates by country; the library product keeps loan rules by branch. Those values change on their own schedule, apart from the code that applies them, and several operational records need the same value at different dates.

LiteSpec puts those rules in a knowledge layer beside the modules. A knowledge area has one knowledge model that defines the types and the lookups, and one knowledge set per variant that holds only values. Operational code asks the model for a role on a date and never names a set or a data key. This page states what the layer owns, how a set stays free of logic, and how the rules are checked.

## Concepts

### The Knowledge Level and the Operational Level

The operational level holds aggregates and the use cases that change them. The knowledge level holds the rules those use cases consult: a rate, a limit, a deadline, or a template. An expense line is operational; the per-diem rate applied to its travel date is knowledge. The two levels change for different reasons. An operational record changes through a use case, while a knowledge row changes through a release or through an editor of the values. Keeping them apart lets a rate change without rewriting the code that reads it.

### Knowledge Area

A knowledge area is one subject of rules, such as travel rates or loan rules. Its records live at `.litespec/behavior/knowledge/<area>/`, and a reference-data table under it carries the id `reference-data/<area>.<table>`. When the values differ by a dimension, the table states `variation` with that dimension. Claimwick's `travel` area holds the per-diem and mileage tables and varies by `country`. The library product's lending area varies by `branch`.

### Knowledge Model

The knowledge model defines the types of one area: rows, closed sets, roles, typed keys, policy methods, the set contract, the runtime that indexes a set and answers lookups by key or role and date, the registry, and the startup validation. The model owns the behavior. A module depends on the model, never on a set. In Claimwick the model is `Claimwick.Travel`: it defines a per-diem row, the `FullDay` and `PartialDay` roles, and a lookup that returns a refusal when no row covers the date.

### Knowledge Set

A knowledge set holds the values for one variant of an area: the rows, effective dates, and sources for one country or one branch. It holds no logic and builds rows only through the model's constructors. Claimwick ships one set per country, such as `Claimwick.Travel.Germany` and `Claimwick.Travel.Netherlands`; the library product ships one set per branch. Only a host references a set, to register it with the model at startup.

### Variant Dimension

The variant dimension is the value that selects a set, such as `country` or `branch`. An area that varies by nothing keeps its one table in the model and needs no set. A table whose rows differ by variant states `variation`, so a reader and a check know which dimension selects the values.

### Effectivity and Versions

Each row states the date from which it applies and the source behind its value. A released row stays unchanged: a new value arrives as a new row, and the old row takes an end date. Each set carries a version. An operational record keeps the key, value, and version it used, so a later rate change never rewrites what the record decided.

### Templates

A template is a starting set of values, such as a starting chart or a default policy. Copying a template into an operational record happens once. The copy is operational from then on and changes through the record's own use cases; it no longer follows the template.

### What Is Not Knowledge

A per-tenant override is a setting on an operational aggregate: it belongs to one tenant and changes through that tenant's use case. The settings an adapter needs to connect to an outside system (endpoints, credentials, the wire format it parses) are adapter configuration. Neither belongs in a knowledge model or a set. Facts about outside providers that use cases read or screens show, such as which providers the product accepts and what it offers through them, are knowledge.

## Standards

### Give Each Knowledge Area One Model

**ID:** `litespec/core/knowledge#give-each-knowledge-area-one-model`

**Strength:** required

**Applies to:** artifact: knowledge-model, knowledge-set, record:knowledge-area, record:reference-data

**Statement:** A knowledge area MUST have one knowledge model and one knowledge set per variant of its dimension, keeping its one table in the model when it varies by nothing.

**Rationale:** One model gives every caller the same types and lookups, and one set per variant keeps values for a country or a branch apart without copying the types into each set. An area that varies by nothing needs no set: a second shape for one table would leave readers unsure which holds the values. A set project sits under `Knowledge/` beside its model and references that model; anything else the tools never treat as a set.

**Example:** Claimwick's `travel` area has the `Claimwick.Travel` model and a set for each country its rates cover, while its approval-target table, which varies by nothing, stays in the model.

**Checked by:** `litespec model check` reports `knowledge-set-missing` for a variant an area names with no set, and `knowledge-variation-undeclared` for rows that differ by variant without a declared dimension.

### Read Knowledge Through Its Model

**ID:** `litespec/core/knowledge#read-knowledge-through-its-model`

**Strength:** required

**Applies to:** artifact: knowledge-model, knowledge-set, command-handler, query-handler, reaction, job

**Statement:** A module MUST reach a knowledge area through its knowledge model, leaving a set to be referenced only by a host that registers it.

**Rationale:** A module that names a set binds its code to one country or branch, so adding a variant means editing the module. The model gives modules one stable surface, and a host registers each set with that model at startup. The dependency runs from the Kernel to the model to the modules, and from a set to its model and the Kernel only. The model owns its registry, its composition and its startup validation, so it references the dependency-injection, logging and options libraries those need.

**Example:** Claimwick's spending module asks `Claimwick.Travel` for a per-diem role on the expense date and names no country project. The API host registers `Claimwick.Travel.Germany` and `Claimwick.Travel.Netherlands` with the model at startup. The `Claimwick.Travel` model registers its sets through `TravelComposition`, which takes an `IServiceCollection` and validates the registered sets at startup.

**Checked by:** `dotnet build` reports `LS2038` for a module assembly referencing a knowledge set assembly: a project under `Knowledge/` named `<Product>.<Area>.<Variant>` of the same product that references its model, so another vendor's packages never count. It reports `LS1001` for an outer-layer reference in a model outside the dependency-injection, logging and options libraries.

### Keep Knowledge Sets Free of Logic

**ID:** `litespec/core/knowledge#keep-knowledge-sets-free-of-logic`

**Strength:** required

**Applies to:** artifact: knowledge-model, knowledge-set

**Statement:** A knowledge set MUST hold only rows built through its model's constructors, with any behavior a variant needs added as a case in the model's closed set of strategies.

**Rationale:** A set that decides something splits one rule into one copy per variant, and the copies drift. Rows built through model constructors carry the model's types and validation. When a country or a branch needs a treatment data cannot express, a case in the model's strategy set holds the rule and the set selects it, so every variant shares the same behavior.

**Example:** Claimwick's Germany and Netherlands sets list dated per-diem rows and nothing else. When a country needs a different rounding of a partial day, the rule becomes a case in the model's rounding strategy set, selected by the set's data.

**Checked by:** `dotnet build` reports `LS2039` for a method or property body in a knowledge set that does more than return or construct rows; a static `Parse`, `From`, `FromCode`, `Of` or `Create` call on a named type with only literal arguments, such as a typed key built from its text, counts as constructing.

### Model Reference Data as Domain Values

**ID:** `litespec/core/knowledge#model-reference-data-as-domain-values`

**Strength:** required

**Applies to:** artifact: record:reference-data

**Statement:** Each reference-data table MUST be read through domain values that carry the behavior its values decide, with infrastructure only loading the rows.

**Rationale:** A row of free strings means whatever each caller guesses: one value reads two ways in two callers, and a new caller adds a third reading. A domain value types what the table holds and carries the behavior each reading needs, so every caller decides the same way. Infrastructure loads rows and parses them into those values, and decides nothing from them.

**Example:** Claimwick's decision target is read through a domain value carrying days: the approval rules interpret the days, and the loader that reads the table's rows interprets nothing.

**Reviewed for:** Does any caller interpret reference-data rows outside the domain values, or does infrastructure decide behavior from them? Check the readers of each reference-data table beside the domain values.

### Read Knowledge by Role and Date

**ID:** `litespec/core/knowledge#read-knowledge-by-role-and-date`

**Strength:** required

**Applies to:** artifact: knowledge-model, knowledge-set, command-handler, query-handler, reaction, job

**Statement:** Operational code MUST read knowledge by role and date through the model's typed keys, naming no set and building no key from a string literal.

**Rationale:** A data key names one row of one set, so code naming it breaks when another set keys its rows differently. A role states what the caller needs, and each set says which row plays it. A typed key built through the model keeps the caller from spelling a country or a date into a raw string.

**Example:** Claimwick's expense submission asks the travel model for the `FullDay` per-diem role on the travel date. It builds the key through the model and names neither Germany nor a row key. A literal such as `new TravelKey("DE")` is reported.

**Checked by:** `dotnet build` reports `LS2040` for a knowledge key built from a string literal outside a knowledge set.

### Name a Role Not a Row

**ID:** `litespec/core/knowledge#name-a-role-not-a-row`

**Strength:** required

**Applies to:** artifact: record:reference-data

**Statement:** Each reference-data table MUST list in `requiredRoles` the roles its citing rules read, with each role played by a row in `role`.

**Rationale:** A data key names one row of one data set, so code naming the key breaks when another data set keys its rows differently or not at all. A role names what the product needs, such as the standard target, and each data set says which row plays it. The `reference-data-role-missing` check reports a required role no row plays, and code asks for the role instead of the key.

**Example:** Claimwick's `reference-data/spending.decision-target` lists `requiredRoles: [standard]`, and both its rows play `standard`, so the approval rules read the standard target without naming a row.

**Checked by:** `litespec model check` reports `reference-data-role-missing` (level G) for a required role played by no row.

### Version Each Set and Freeze What Records Used

**ID:** `litespec/core/knowledge#version-each-set-and-freeze-what-records-used`

**Strength:** required

**Applies to:** artifact: knowledge-set, record:aggregate

**Statement:** Each knowledge set MUST carry a version that an operational record keeps with the key and value it used.

**Rationale:** A rate that changes after an expense was decided must not rewrite that decision. The set's version pins the row in force at the time, and the record keeps the key, value, and version so a reader can defend the figure. Row effectivity and sourcing follow `litespec/core/domain-model#date-every-reference-data-row`, and released rows stay unchanged under `litespec/core/domain-model#append-released-reference-rows`.

**Example:** A Claimwick expense submitted under the 2026 per-diem row keeps that row's key, value, and set version. When a 2027 row takes effect, the expense still shows the figure it used.

**Reviewed for:** Does any operational record name a knowledge value without the key, value, and version in force when it was made? Look at each record beside the set version it cites.

### Refuse a Missing Rule with a Reason

**ID:** `litespec/core/knowledge#refuse-a-missing-rule-with-a-reason`

**Strength:** required

**Applies to:** artifact: knowledge-model, command-handler, query-handler, record:use-case

**Statement:** A lookup with no row in force MUST end its use case in a refusal naming the missing role, date, and variant.

**Rationale:** A null or a default value makes the caller spend a value no rule supports, with no record of why. A refusal with a reason points the operator at the row that is missing, and the use case records the refusal beside its other endings. The model derives that ending as `result/<use-case>.rule-not-stated`. The model throws the product's rule-not-stated refusal type, which derives the kernel's domain exception.

**Example:** If no German per-diem row covers a travel date, Claimwick's submission ends in a refusal naming the `FullDay` role, that date, and Germany, rather than paying zero. Claimwick's travel model throws `TravelRuleMissingException`, which derives the kernel `DomainException` and carries the code `travel.rule-missing`.

**Reviewed for:** Does any knowledge lookup return a null or a default when no row is in force, or end without naming the missing rule? Look at each lookup beside the use-case refusals.

### Check Shape at Compile Time and Dates at Startup

**ID:** `litespec/core/knowledge#check-shape-at-compile-time-and-dates-at-startup`

**Strength:** required

**Applies to:** artifact: knowledge-model, knowledge-set

**Statement:** Shape and cross-references in code MUST be checked at compile time, while overlaps, gaps, and coverage of the current period are checked at startup.

**Rationale:** Row types, roles, and references between model types are fixed by the code, and the compiler proves them on every build. Dates depend on the rows a set ships, so only loading the set can prove that its periods do not overlap, leave no gap, and cover today. Startup is the first moment the whole set is known, and a failure there names the row before any request uses it. A set that fails startup validation raises a startup configuration failure, not a business refusal, so `LS1005` does not apply inside a knowledge model.

**Example:** Claimwick's build proves that every per-diem row carries a country, an amount, dates, and a source. At startup the travel model rejects a German row that overlaps another or leaves the current year uncovered, and the product does not serve until it is fixed. The rejection raises `InvalidOperationException`, as `TravelRegistry.Validate` does.

**Reviewed for:** Is any date rule left to a compile-time check, or any shape rule left to a startup check? Look at the build diagnostics beside the startup validation of each set.

### Validate Reference Data Before Serving

**ID:** `litespec/core/knowledge#validate-reference-data-before-serving`

**Strength:** required

**Applies to:** artifact: record:reference-data

**Statement:** Each reference-data table MUST hold only rows that resolve their cross-references, stay within its closed set where one is listed, and play its required roles.

**Rationale:** A broken cross-reference, an unlisted value, or a missing role each fails at read time, far from the row that caused it. Validating the whole table before serving it moves each failure to startup, naming the file and row. The record checks report `reference-data-key-unknown`, `reference-data-value-invalid`, and `reference-data-role-missing`, and the host refuses to start until the tables it loads validate.

**Example:** A Claimwick row referring to `reference-data/spending.decision-target#target-2030` is reported, naming the file and row, before the product serves the table.

**Checked by:** `litespec model check` reports `reference-data-key-unknown`, `reference-data-value-invalid`, and `reference-data-role-missing` (level G) for a cross-reference to a missing key, a value outside `choices`, and a required role played by no row.

### Seed Operational Records from Templates

**ID:** `litespec/core/knowledge#seed-operational-records-from-templates`

**Strength:** required

**Applies to:** artifact: knowledge-model, knowledge-set, record:use-case, record:aggregate

**Statement:** A template MUST seed an operational record once, after which the copy is operational and no longer reads the template.

**Rationale:** A starting chart or default policy gives a new record a valid beginning without asking the operator to enter every value. If the copy kept reading the template, a later template change would rewrite a record the business already uses. Copying once makes the record own its values and its own use cases.

**Example:** In the library product, a new branch starts from a template of loan limits. The clerk copies it when the branch opens; later edits to the branch's limits go through the branch's own use case, and a change to the template does not touch that branch.

**Reviewed for:** Does any operational record still read its template after it was created, or does any record skip the one-time copy? Look at the creation use case beside the template it seeds from.

### Keep Overrides and Integration Catalogs Out of Knowledge

**ID:** `litespec/core/knowledge#keep-overrides-and-integration-catalogs-out-of-knowledge`

**Strength:** required

**Applies to:** artifact: knowledge-model, knowledge-set, record:aggregate

**Statement:** Per-tenant overrides and the settings an adapter needs to connect to an outside system MUST stay outside knowledge, as a setting on an operational aggregate and as adapter configuration; facts about outside providers that the product itself reasons about or shows are knowledge.

**Rationale:** An override belongs to one tenant and changes through that tenant's use case, so it needs the aggregate's rules and access, not a released table. An endpoint, a credential or the wire format an adapter parses concerns only the adapter that connects; it changes with the outside system, and no use case reads it. Which providers the product accepts or offers, and what it promises about them, is different: use cases read it, screens show it, and it varies like any other knowledge, so it belongs in a knowledge area read through its model.

**Example:** A Claimwick tenant that pays a higher partial-day rate keeps that rate in its own policy aggregate through a use case. The endpoint and file layout Claimwick's import adapter uses to fetch travel bookings stay in that adapter's configuration, while the travel agencies Claimwick accepts bookings from, which an employee picks on the expense form, live in a knowledge area.

**Reviewed for:** Is any tenant override held in a knowledge set, any connection setting held in a knowledge model, or any provider fact a use case reads held only in an adapter? Look at the settings and adapter configuration beside the knowledge sets.

### Use an Aggregate for Edited Reference Data Only

**ID:** `litespec/core/knowledge#use-an-aggregate-for-edited-reference-data-only`

**Strength:** required

**Applies to:** artifact: record:reference-data

**Statement:** Reference data MUST live as an aggregate only when people edit it at runtime, on the owner who edits it rather than on the published table.

**Rationale:** A published table changes by release: its rows are written with the product and read as written. An aggregate changes by use case: it guards each change with rules, refusals, and access. Making a published table an aggregate promises guarding nothing performs, and leaving runtime-edited values in a table leaves their changes unguarded. The aggregate sits on the owner who edits the values, such as a tenant's own overrides, rather than on the published table.

**Example:** Claimwick's receipt threshold lives on the policy aggregate because managers change it through `use-case/spending.change-receipt-threshold`, while the decision target stays a reference table nobody edits at runtime.

**Reviewed for:** Does any reference-data table take edits at runtime, or does any aggregate hold a published table nobody edits? Check each table beside the use cases that write it.

## Conventions

### Keep Knowledge in the Backend Language

**ID:** `litespec/core/knowledge#keep-knowledge-in-the-backend-language`

**Strength:** default

**Applies to:** artifact: knowledge-model, knowledge-set

**Default:** Keep knowledge in the backend language when only the backend reads it and it ships with the release.

**Replacement:** A project records a data format such as JSON, YAML, or a database in its backend documentation when non-developer editors change the values, changes ship without a release, or several readers need them.

**Rationale:** Values written as code travel with the release, compile with the model, and need no loader or schema of their own. A data format earns its place when the people who own the values are not developers, when a value must change between releases, or when another reader needs the same table; in those cases the project names the format and its home.

**Example:** Claimwick's travel rows with their dates and sources live in the backend language and ship with each release. A table that editors outside the team maintain between releases is recorded as a data format.

**Reviewed for:** Is any knowledge table kept in a data format without the stated need, or shipped in code when non-developer editors change it? Look at each knowledge set beside the project's backend documentation.
