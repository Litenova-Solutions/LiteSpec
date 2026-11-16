# Glossary

## Code Terms

These terms name the code shapes that put the specification into effect.

### Aggregate Root

The single type through which outside code changes an aggregate; child entities expose no mutable state outside it.

Home: docs/backend/domain.md

Source: DDD.

### AppHost

The local orchestrator that starts every application together with its dependencies for development.

Home: docs/backend/dotnet/architecture.md

Source: .NET Aspire.

### Architecture Layer

One role a backend type plays, regardless of which folder or project holds it: Domain, Application, Infrastructure, or WebApi.

Home: docs/backend/architecture.md

Source: LiteSpec.

### Authorization Policy

One rule that decides whether an actor is allowed to perform an action on a target.

Home: docs/core/access.md

Source: RBAC.

### Caller

The party making the current call. It is distinct from the Actor record, which holds roles.

Home: docs/core/access.md

Source: LiteSpec.

### Central Package Management

One central file that pins every dependency version in the workspace.

Home: docs/workspace/dependencies.md

Source: NuGet.

### Child Entity

One entity inside an aggregate, changed only through the aggregate's root.

Home: docs/backend/domain.md

Source: DDD.

### Domain Event

One immutable statement that a relevant fact completed inside the bounded context. A published fact crossing the boundary is an integration event instead.

Home: docs/core/domain-model.md

Source: DDD.

### Domain Repository

One boundary interface that loads and stages complete aggregates, with one repository per aggregate.

Home: docs/backend/persistence.md

Source: DDD.

Rejected: repository used alone, which hides whether the boundary loads a whole aggregate or a row.

### Endpoint

One class serving one route operation. It converts HTTP input to an Application message and maps the result back to HTTP.

Home: docs/backend/api.md

Source: web API practice.

### Entity

One object defined by its identity rather than by its values.

Home: docs/backend/domain.md

Source: DDD.

### Input Validation

Checks on the shape of a command or query, run before any business behavior runs.

Home: docs/backend/application.md

Source: LiteSpec.

### Layer Claim

One record layer a project claims in `layers`, with the practice it follows there: `intent` and `behavior` take `litespec`, while `implementation` and `proof` take `litespec` or `own`.

Home: docs/core/scope.md

Source: LiteSpec.

### Layer Projects

The backend layout with one project per architecture layer, selected through the `layer-projects` extension.

Home: docs/backend/dotnet/ext/layer-projects.md

Source: LiteSpec.

### Lockfile

The committed file that records the exact resolved dependency versions.

Home: docs/workspace/dependencies.md

Source: the package managers.

### Module Layout

The default backend layout: every module sits in the modules project beside the shared kernel project, with layer folders at each node, one `UseCases/` folder per aggregate and module, and one flat folder per use case inside it.

Home: docs/backend/dotnet/architecture.md

Source: LiteSpec.

### Operation

One route operation with its stable operation id, served by one endpoint class.

Home: docs/backend/api.md

Source: web API practice.

### Page File

The framework file serving one address: `+page.svelte` with its `+page.ts` on Svelte, and one route file on React. A page file renders one screen. The screen is the record, the route is the address, and the page file is the code.

Home: docs/frontend/web/structure.md

Source: LiteSpec.

### Persistence Constraint

One rule enforced by durable storage itself, such as uniqueness.

Home: docs/backend/persistence.md

Source: LiteSpec.

### Projection

One read model derived from authoritative facts.

Home: docs/backend/persistence.md

Source: CQRS.

### Read Model

One record holding a module's own copy of another module's state. It is filled from published module or integration events and shaped for a query.

Home: docs/core/automation.md

Source: CQRS.

Rejected: view, which collides with UI and database views.

### Route

One HTTP path with its method, resolving to the one endpoint class that serves it.

Home: docs/backend/api.md

Source: web API practice.

### Trace Context

The trace and span identifiers carried across logs and calls, tying one request's records together.

Home: docs/quality/operations.md

Source: W3C Trace Context.

### Value Object

One immutable type defined by its values rather than by identity.

Home: docs/backend/domain.md

Source: DDD.

## Product and Domain Terms

These terms name the records and domain concepts that specifications are written in.

### Access

One risk value. It marks permissions that decide who may act.

Home: docs/core/access.md

Source: LiteSpec.

### Act

One run of behavior that changes state. Each value it copies from elsewhere freezes at the act.

Home: docs/core/use-cases.md

Source: LiteSpec.

### Actor

One person or organization acting in the product. The record notes whether the actor belongs to the team building and running the product. An external system is no actor, and internal machinery is no actor either.

Layer: Intent.

Home: docs/core/intent.md

Source: RBAC.

### Aggregate

One unit of business state that stays consistent and changes in one step, with states and invariants.

Layer: Behavior.

Home: docs/core/domain-model.md

Source: DDD.

Rejected: entity, which in DDD also names a child inside an aggregate.

### Aggregate Data

The named fields of an aggregate. Each field carries a meaning, a data class, and a classification of `public`, `internal`, `personal`, or `sensitive`.

Layer: Behavior.

Home: docs/core/domain-model.md

Source: LiteSpec.

### Anonymize

One erasure position. Past it, the data can no longer identify anyone.

Layer: Behavior.

Home: docs/core/privacy.md

Source: the GDPR.

### Automation

Work that runs without a request, stated as reactions and jobs.

Home: docs/core/automation.md

Source: LiteSpec.

### Behavior

The record layer stating how the system keeps the promises the Intent layer states.

Home: docs/core/behavior.md

Source: LiteSpec.

Rejected: behaviour, the British spelling.

### Bounded Context

The boundary inside which one domain model and one glossary hold; each LiteSpec project covers one bounded context.

Home: docs/core/scope.md

Source: DDD.

### Brand

The stated character of the product, which every screen keeps: its reader, what it is accountable for, the three words it is and the three it is not, and the rules every screen keeps.

Layer: Intent.

Home: docs/core/intent.md

Source: LiteSpec.

### Calculation

One rule stating how a value is derived. It is proven by a scenario with an examples table.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: the Business Rules Group.

### Cites

The link a record carries to the records behind it, giving its reason.

Layer: Intent or Behavior.

Home: docs/core/model.md

Source: LiteSpec.

### Closing Record

One record whose change closes a gap waiver. The waiver names it in `closedBy`.

Layer: Intent or Behavior.

Home: docs/core/waivers.md

Source: LiteSpec.

### Command

One use case kind. A command changes business state.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: CQRS.

### Compensation

Work a reaction does to undo a committed step after a later step fails. It names the undone use case in `compensates`.

Layer: Behavior.

Home: docs/core/automation.md

Aliases: undo.

Source: LiteSpec.

### Conclusion

The one sentence a research record concludes with, stated as data so a decision can cite it.

Layer: Intent.

Home: docs/core/intent.md

Source: LiteSpec.

### Consistency

The declared write shape of a command writing more than one aggregate: `atomic` or `per-record`.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: LiteSpec.

### Constraint

One outside rule the product obeys, such as a law or provider terms. It carries its authority, its kind, and the date it was read.

Layer: Intent.

Home: docs/core/intent.md

Source: plain English.

### Data Class

One named group of aggregate fields. The group shares one classification, one purpose, one erasure position, and one retention period with its basis and enforcement.

Layer: Behavior.

Home: docs/core/privacy.md

Source: LiteSpec.

### Decided By

The command use case or aggregate that decides the figure at write time. A `snapshot` or `eventual` freshness names it in `decidedBy`.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: LiteSpec.

### Decision

One dated choice, with its options weighed and its evidence cited.

Layer: Intent.

Home: docs/core/intent.md

Source: decision records.

### Density

How much a screen shows at once, stated per entry point as `public-light`, `application-balanced`, or `admin-dense`.

Layer: Behavior.

Home: docs/core/edges.md

Source: LiteSpec.

### Domain

The real-world knowledge, language, rules, state, and behavior that the software models.

Home: docs/backend/domain.md

Source: DDD.

### Effect

One declared edge from a use case, reaction, or job to a command, a notification, an integration event, or an outside call.

Layer: Behavior.

Home: docs/core/automation.md

Source: LiteSpec.

### Entry Point

One way in from outside, through which people or systems reach the product.

Layer: Behavior.

Home: docs/core/edges.md

Source: LiteSpec.

### Entry Point Set

One page holding one module's entry points, with each point's kind and authentication.

Layer: Behavior.

Home: docs/core/edges.md

Source: LiteSpec.

### Evidence

One dated claim behind a problem, with its source and its kind: `observed`, `reported`, or `assumed`. The strongest kind present is computed and never written.

Layer: Intent.

Home: docs/core/intent.md

Source: LiteSpec.

### Example

One narrative showing behavior on one real occasion, drawn from the example cast.

Home: docs/core/writing.md

Source: plain English.

### Example Cast

The people, place, dates, and amounts that every example draws on.

Home: docs/core/writing.md

Source: plain English.

### Examples Table

One table of rows a scenario reads through. It holds the debated values.

Home: docs/core/use-cases.md

Source: BDD.

### Excludes

The product-level list of plain-sentence exclusions. These are the only statements that override a requirement.

Layer: Intent.

Home: docs/core/intent.md

Source: LiteSpec.

### External System

One outside system the product exchanges messages with. It stands alone and names no actor. A system calling the product reaches it through an API entry point.

Layer: Behavior.

Home: docs/core/edges.md

Source: LiteSpec.

### Failure Policy

The `onFailure` handling a reaction, job, or effect declares. It carries a mode of `retry`, `skip-and-log`, or `discard`, with the retry, dead-letter, surface, and runbook its mode states.

Layer: Behavior.

Home: docs/core/automation.md

Source: LiteSpec.

### Feature

One standalone answer to one or more problems. It advances a goal and holds its requirements.

Layer: Intent.

Home: docs/core/intent.md

Source: plain English.

### Flow

The ordered steps by which one actor reaches one goal it serves, or one problem or constraint it answers. The record describes who takes the flow, what starts it, what good looks like, and the consequence at stake.

Layer: Intent.

Home: docs/core/intent.md

Source: plain English.

### Flow Branch

A path leaving a flow's step order at one step. It names the step it leaves, the condition under which it leaves, and the step it continues at, or the end, and states no action of its own.

Layer: Intent.

Home: docs/core/intent.md

Source: plain English.

### Format

The shape of a money amount or a moment on a screen. It is chosen from the money and moment patterns and named on the entry point.

Layer: Behavior.

Home: docs/frontend/ui.md

Source: LiteSpec.

### Freshness

How current a query group or read model is: its mode of `immediate`, `snapshot`, or `eventual`, with the duration within which it holds.

Layer: Behavior.

Home: docs/core/automation.md

Source: LiteSpec.

### Functional Requirement

One requirement stating what the product does, as opposed to how well it performs.

Layer: Intent.

Home: docs/core/intent.md

Source: BABOK and IREB.

### Gap Waiver

One waiver for work the team means to close. It carries an expiry date, an exit, and the records whose change closes it.

Layer: Intent or Behavior.

Home: docs/core/waivers.md

Source: LiteSpec.

### Generic Refusal

One refusal the model derives instead of a written result: `forbidden`, `invalid-input`, `not-found`, `conflict`, `unauthenticated`, or `sign-in-too-old`.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: LiteSpec.

### Glossary

The project glossary: each term with its meaning and its rejected synonyms, held as data in `glossary.yaml`.

Home: docs/core/language.md

Source: arc42; DDD's ubiquitous language.

### Goal

One measurable outcome. A feature advances it, a flow serves it, or a quality requirement confirms it. A goal carries no authority.

Layer: Intent.

Home: docs/core/intent.md

Source: Volere and product practice.

### Idempotency

The declared way a repeated command collapses to one effect. It states the mode of `key`, `state`, or `none`, and what a recognized replay does.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: LiteSpec.

### Implementation

The record layer that holds the code and technical decisions carrying out Behavior.

Home: docs/core/layers.md

Source: LiteSpec.

### Integration Event

One published fact crossing the bounded context. It carries its direction, payload, and trust, and names the external system it travels through. A fact for another module of the same product is a module event instead.

Layer: Behavior.

Home: docs/core/edges.md

Source: DDD.

### Intent

The record layer that states why the product exists and what it promises.

Home: docs/core/intent.md

Source: LiteSpec.

### Invariant

One rule that holds at every observable moment.

Layer: Behavior.

Home: docs/core/domain-model.md

Source: DDD.

### Job

One record stating work that runs on a schedule or once. It declares the outcomes each run ends in.

Layer: Behavior.

Home: docs/core/automation.md

Source: LiteSpec.

### Language

One language an entry point serves, named by its BCP 47 tag. The first tag listed is the default.

Layer: Behavior.

Home: docs/frontend/ui.md

Source: BCP 47.

### Lasting Waiver

One waiver for a choice the project keeps. It carries the consequences the project accepts, and no end date.

Layer: Intent or Behavior.

Home: docs/core/waivers.md

Source: LiteSpec.

### Measured Requirement

One quality requirement carrying a measure, a target, and a check confirming them.

Layer: Intent.

Home: docs/core/intent.md

Source: Volere.

### Module

One business area that organizes language, records, and use cases.

Layer: Behavior.

Home: docs/core/behavior.md

Source: DDD.

### Module Event

One fact one module publishes for the other modules of the same product, delivered in process through the transactional outbox. It translates one domain event of its own module and carries a payload.

Layer: Behavior.

Home: docs/core/automation.md

Source: LiteSpec.

### Money

One risk value. It marks permissions that move, hold, or decide money.

Home: docs/core/access.md

Source: LiteSpec.

### Navigation Map

The drawing of one entry point's screens as places, with the connections between them. The book draws it from `leadsTo`.

Home: docs/tools/book.md

Source: LiteSpec.

### Notification

One person-facing message with its audience, channel, consent rule, and claims. It is delivered through one external system.

Layer: Behavior.

Home: docs/core/edges.md

Source: LiteSpec.

### Observation

One dated measured value on a goal, with its source. It shows the outcome reached, apart from delivery.

Layer: Intent.

Home: docs/core/intent.md

Source: LiteSpec.

### Outcome

One named way an operation ends. It is answered with a status at the API boundary, or with one of the closed run states of a reaction or job.

Layer: Behavior.

Home: docs/core/edges.md

Source: LiteSpec.

### Page Frame

One named screen layout from the page-frame pattern catalog. Each screen record names it with the option the product chose.

Layer: Behavior.

Home: docs/frontend/patterns/page-frames.md

Source: LiteSpec.

### Permission

One allowed action, taken from the shared closed list.

Layer: Behavior.

Home: docs/core/access.md

Source: RBAC.

### Permission Set

One page holding one module's permissions, with the risk each permission carries.

Layer: Behavior.

Home: docs/core/access.md

Source: LiteSpec.

### Personal Data

One risk value. It marks permissions that keep personal or sensitive data.

Home: docs/core/access.md

Source: LiteSpec.

### Physical Access

One risk value. It marks permissions that control who passes a physical boundary.

Home: docs/core/access.md

Source: LiteSpec.

### Policy Requirement

One requirement stating a policy the business holds. Where the value changes over time, it carries effective dates.

Layer: Intent.

Home: docs/core/intent.md

Source: LiteSpec.

### Precondition

One rule that holds before a use case runs.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: Design by Contract.

### Problem

One pain a person or organization outside the team building the product has today. It names its primary sufferer, its cost today, its workaround today, and its graded evidence.

Layer: Intent.

Home: docs/core/intent.md

Source: plain English.

### Product

The software capability offered to users, with its boundary and the exclusions that override its requirements. The product stands above the record layers as their root.

Home: docs/core/intent.md

Source: plain English.

### Product Area

One Behavior module. The book groups Intent by product area, with one group per module. Each group lists the features and flows the module's use cases, reactions, jobs, or screens reach.

Home: docs/tools/book.md

Source: LiteSpec.

### Product Requirement

One requirement with no offer behind it. It sits at product level and cites the constraint, goal, or problem it comes from.

Layer: Intent.

Home: docs/core/intent.md

Source: LiteSpec.

### Prose Reference

One record identifier or provision identifier written in prose in backticks. It must name a written record, or a provision the carried release ships.

Layer: Intent or Behavior.

Home: docs/core/model.md

Source: LiteSpec.

### Pseudonymize

One erasure position. It keeps personal data personal while replacing direct identifiers.

Layer: Behavior.

Home: docs/core/privacy.md

Source: the GDPR.

Rejected: pseudonymise, the British spelling.

### Purpose

The reason for keeping personal data, stated on each data class.

Layer: Behavior.

Home: docs/core/privacy.md

Source: the GDPR.

### Quality Requirement

One requirement stating how well the product performs. It carries a measure and a target.

Layer: Intent.

Home: docs/core/intent.md

Source: BABOK and IREB.

### Query

One use case kind. A query reads without changing business state.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: CQRS.

### Reaction

One record stating work that runs when a domain, module, or integration event arrives. It declares the outcomes each run ends in.

Layer: Behavior.

Home: docs/core/automation.md

Source: LiteSpec.

### Record

One written unit of the specification, of a fixed type and with fixed links.

Home: docs/core/model.md

Source: LiteSpec.

### Refusal

One named way a use case ends without its effect. Refusals read in precedence order, with the first match winning. A refusal is a declared business outcome where the use case did not perform its act because a rule, access or input said no; a technical failure (a timeout, a provider error, a crash) is not a result and is handled by retry, the outbox and operations, not declared on the use case.

Layer: Behavior.

Home: docs/core/use-cases.md

Aliases: rejection, error.

Source: LiteSpec.

### Refusal Code

One stable identifier a module's refusal set declares for a way a use case ends without its effect. It reads `<module>.<name>`.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: LiteSpec.

### Refusal Sentence

The sentence a person reads when a refusal reaches them. It sits on the refusal result in the product's language, with one sentence per further language beside it.

Layer: Behavior.

Home: docs/core/edges.md

Source: LiteSpec.

### Refusal Set

One page holding one module's refusal codes, with each code's meaning and problem type.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: LiteSpec.

### Requirement

One behavior the product shows, or one guarantee it keeps. An `except` entry names its fallback requirement, and a tenant-set value is no exception to the requirement. Its `cites` names the goals, visions, problems, features, constraints, research, and decisions behind it.

Layer: Intent.

Home: docs/core/intent.md

Source: BABOK and IREB.

Rejected: product rule, which clashes with the rule record.

### Research

One outside fact that informs a choice.

Layer: Intent.

Home: docs/core/intent.md

Source: plain English.

Rejected: decision evidence, which states where the fact is used instead of what it is.

### Result

One way a use case ends: success or a named refusal. Refusals read in precedence order, and a success may carry codes for recorded outcomes inside it. A refusal result carries a code from its module's refusal set, and its identifier is `result/<use-case>.<name>`.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: the Result pattern.

### Role

One named set of permissions granted together. The role holds one scope.

Layer: Intent.

Home: docs/core/access.md

Source: RBAC.

### Rule

One testable commitment that states what holds.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: the Business Rules Group.

### Rule Set

One page holding one module's or aggregate's rules, with each rule's condition and refusal.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: LiteSpec.

### Runbook

The steps for running or recovering the product.

Layer: Implementation.

Home: docs/quality/operations.md

Source: the SRE book.

### Scenario

One testable example proving one behavior, stated as given, when, then, and stored in the file of the use case, reaction, or job it exercises. A scenario may point at a derived result. Its identifier names the behavior it exercises, as `scenario/<use-case>.<name>`.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: BDD and Gherkin.

Rejected: acceptance criterion, which names the review use instead of the example.

### Screen

One page a person sees through a browser or device entry point. It serves a flow step only through a use case it offers.

Layer: Behavior.

Home: docs/core/edges.md

Source: LiteSpec.

### Setting

One named bound or default the product runs within, with its setter and its behavior at the bound. The use case, reaction, or job reading it names it in `reads`.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: LiteSpec.

### Setting Set

One page holding one module's settings, with each setting's bound or default.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: LiteSpec.

### Specification

The whole set of records describing one product.

Home: docs/core/model.md

Source: LiteSpec.

### State

One named stage in an aggregate's lifecycle, with its meaning.

Layer: Behavior.

Home: docs/core/domain-model.md

Source: LiteSpec.

### Store

One record holding state that no aggregate invariant guards, such as a suppression list or a delivery log. It names its owning module or external system and carries no states.

Layer: Behavior.

Home: docs/core/domain-model.md

Source: LiteSpec.

### Storyboard

The drawing of one flow as a strip of its steps' screens in step order. The book draws it.

Home: docs/tools/book.md

Source: LiteSpec.

### Team Actor

One actor belonging to the team building and running the product, marked on its record. A problem never names a team actor as its primary sufferer.

Layer: Intent.

Home: docs/core/intent.md

Source: LiteSpec.

### Tenant Scope

The declared reach of a use case grant across tenants: `own`, `cross`, `public`, or `none`.

Layer: Behavior.

Home: docs/core/access.md

Source: LiteSpec.

### Term

One word with one meaning, labels in each language, and rejected synonyms. Each term carries its layer and its established aliases.

Layer: Intent or Behavior.

Home: docs/core/language.md

Source: arc42; DDD's ubiquitous language.

### Token Role

One named use of color, such as text, card edge, or focus ring. The token file holds the role's values, and the role carries the limits they meet in each declared scheme.

Home: docs/frontend/ui.md

Source: LiteSpec.

### Transition

One declared move between aggregate states. A use case makes it, or time moves it through a job.

Layer: Behavior.

Home: docs/core/domain-model.md

Source: LiteSpec.

### Trigger

The domain, module, or integration event starting a reaction, or the schedule starting a job.

Layer: Behavior.

Home: docs/core/automation.md

Source: LiteSpec.

### Ubiquitous Language

The domain words the team and the model share and use alike.

Home: docs/core/language.md

Source: DDD.

### Use Case

One command or query, with its access, results, and scenarios.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: Cockburn.

### User

The person the product serves. A user reaches the product through a browser or device entry point.

Layer: Behavior.

Home: docs/core/edges.md

Source: LiteSpec.

### Verified Claim

One input value a provider checked that the caller can never supply. The record names the entry point or external system that verified it.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: LiteSpec.

### Vision

Where the product is heading beyond what it promises now, stated without a date or priority. A decision keeping it open cites it, and nothing builds from it.

Layer: Intent.

Home: docs/core/intent.md

Source: product vision practice.

### Voice

How the product addresses one entry point's audience, stated on the entry point record in a sentence or two.

Layer: Behavior.

Home: docs/core/edges.md

Source: LiteSpec.

### Waiver

One record that sets a required provision, page, or finding code aside for the records or paths it names, with its reason. A gap waiver carries an expiry date, an exit, and the records whose change closes it; a lasting waiver carries the consequences the project accepts.

Layer: Intent or Behavior.

Home: docs/core/waivers.md

Source: LiteSpec.

### Wireframe

One text drawing of a screen's structure in the wireframe language. It sits in the screen's explanation file.

Layer: Behavior.

Home: docs/frontend/patterns.md

Source: LiteSpec.

## Standard Terms

These terms name the parts of the LiteSpec standard rather than the product.

### Agent

Software that reads task context, plans bounded work, changes the files it is allowed to change, and runs verification.

Home: docs/core/agent.md

Source: LiteSpec.

### Agent-Driven Engineering

The operating model in which agents do engineering work while people keep decision authority.

Home: docs/core/agent.md

Source: LiteSpec.

### Alert Inventory

The list of alerts a product can fire. Each alert names its runbook.

Home: docs/quality/operations.md

Source: the SRE book.

### Audit Record

One entry stating the actor, action, target, outcome, time, origin, and reason.

Home: docs/ext/audit.md

Source: NIST SP 800-53, control AU-3.

### Audit Trail

The append-only store holding audit records, kept separate from logs and domain events.

Home: docs/ext/audit.md

Source: NIST SP 800-53.

### Background Caller

The reaction or job with a `runs` edge to a background-only use case.

Home: docs/core/automation.md

Source: LiteSpec.

### Background-Only

The computed state of a use case a reaction or job runs. It needs no flow to reach it.

Home: docs/core/automation.md

Source: LiteSpec.

### Check Level

One of the three check levels. G reads records only, S reads code through a stack reader, and A reports. Whether a finding blocks follows the strength of the provision it reports.

Home: docs/core/model.md

Source: LiteSpec.

### Checkable

One provision a tool checks. It carries a `Checked by` verification line.

Home: docs/core/authoring.md

Source: LiteSpec.

### Closed Specification

A specification with every record decided and every required link holding.

Home: docs/core/release.md

Source: LiteSpec.

### Coded

The computed state of a use case, rule, domain event, or aggregate state for which the stack's builder finds code.

Home: docs/core/model.md

Source: LiteSpec.

### Command Page

One page documenting one shipped command: its arguments, options, exit codes, and what it runs underneath.

Home: docs/core/authoring.md

Source: LiteSpec.

### Completion Stage

One of the four stages of a record's completion: `unfulfilled`, `specified`, `coded`, or `verified`.

Home: docs/core/model.md

Source: LiteSpec.

### Component Inventory

The list of shipped components, published with each release artifact.

Home: docs/workspace/dependencies.md

Source: supply-chain practice.

Rejected: SBOM, which names one format where the inventory names the content.

### Configuration Page

One page that holds every setting a reader can change.

Home: docs/core/authoring.md

Source: LiteSpec.

### Consumer

One project built against one pinned LiteSpec release.

Home: docs/core/scope.md

Source: LiteSpec.

### Contract

One named set of obligations or shapes that holds between parts, such as a page, a provision, a pattern, a release, or an API.

Home: docs/core/authoring.md

Source: LiteSpec.

### Controlled Technical Prose

The fixed writing profile setting the allowed structures, prose bounds, exact terms, and testable provisions.

Home: docs/core/authoring.md

Source: LiteSpec.

### Convention

One replaceable default for a name, a location, a file shape, or a pattern.

Home: docs/core/authoring.md

Source: LiteSpec.

### CORS

The browser rule deciding which origins can call the API.

Home: docs/backend/api.md

Source: the Fetch standard.

### Covered

The outcome of a finding that a waiver record covers. It is reported, and never blocking.

Home: docs/core/waivers.md

Source: LiteSpec.

### Current Standards Material

The pinned release, taken on its own without transition or history material.

Home: docs/core/authoring.md

Source: LiteSpec.

### Derived Artifact

One committed file produced from an authored source. Generated types are one example.

Home: docs/workspace/structure.md

Source: LiteSpec.

### Diagnostic

One issue `litespec-maintain validate` reports about standards material.

Home: docs/maintain/validate.md

Source: LiteSpec.

### Document

One Markdown page under a record root, holding no record.

Home: docs/core/model.md

Source: LiteSpec.

### Documentation Layer

One depth of documentation, running from a first working result down to the mechanism a command runs.

Home: docs/core/authoring.md

Source: Diataxis.

### Engineering System

One connected set of decisions, specifications, standards, verification, and release practices.

Home: docs/learn/how-litespec-works.md

Source: LiteSpec.

### Exercised

The computed state of a flow step with a passing proof of a use case, reaction, or job that serves it.

Home: docs/core/model.md

Source: LiteSpec.

### Explanation

One page building understanding of what LiteSpec is and why it works the way it does.

Home: docs/core/authoring.md

Source: Diataxis.

### Explanation File

One Markdown file beside a record file, explaining the record for people and agents.

Home: docs/core/writing.md

Source: LiteSpec.

### Extension

One conditional standards bundle, which a project selects when its criterion holds.

Home: docs/core/scope.md

Source: LiteSpec.

### Finding

One issue `litespec model check` reports, with its level, code, message, path, and line.

Home: docs/tools/model.md

Source: LiteSpec.

### Flaky

The computed state of a proof whose test passed in some runs read and failed in others.

Home: docs/core/model.md

Source: LiteSpec.

### Frontmatter

The opening block of a document, stating its page style.

Home: docs/core/authoring.md

Source: LiteSpec.

### Governance

The group of records beside the four record layers. It holds waivers.

Home: docs/core/waivers.md

Source: LiteSpec.

### Guidance

One provision a person judges. It carries a `Reviewed for` verification line.

Home: docs/core/authoring.md

Source: LiteSpec.

### How-To Page

One page solving one stated problem for a reader who already has a working result.

Home: docs/core/authoring.md

Source: Diataxis.

### Index

One page that routes readers to the pages of its section.

Home: docs/core/authoring.md

Source: LiteSpec.

### License

One SPDX identifier that names the terms a shipped package carries.

Home: docs/workspace/dependencies.md

Source: SPDX.

Rejected: licence and licences, the British spellings.

### LiteSpec

The standards contract covering how software is specified, built, verified, released, and operated with agents.

Home: docs/README.md

Source: LiteSpec.

### LiteSpec Release

One standards contract that is published, pinned, and complete.

Home: docs/core/authoring.md

Source: LiteSpec.

### Mark

One written link from a code element or a test, holding the identifier in the project mark style. An `implements` mark names the record the element serves, and a `follows` mark names the provision, decision, or waiver shaping it.

Home: docs/core/layers.md

Source: LiteSpec.

Rejected: anchor and tag, which name the link by one of its forms.

### Normative

What an identified provision requires of a conforming consumer.

Home: docs/core/authoring.md

Source: standards practice, as the W3C QA Framework and ISO Directives use it.

### Page

One authored record file, or one document, under the documentation root.

Home: docs/core/authoring.md

Source: LiteSpec.

### Page Segment

The part of a provision ID that names its owning page.

Home: docs/core/authoring.md

Source: LiteSpec.

### Page Style

One template for a page of prose or record lists.

Home: docs/core/authoring.md

Source: LiteSpec.

Rejected: page class, which reads as a code type.

### Pattern

One named answer to a recurring interface question. It carries its options and wireframes.

Home: docs/frontend/patterns.md

Source: LiteSpec.

### Preset

One ready-made selection of a shape, stacks, and frontends for a new project.

Home: docs/tools/init.md

Source: LiteSpec.

### Project

One business application in one bounded context, built with LiteSpec.

Home: docs/core/scope.md

Source: LiteSpec.

### Provision

One rule, convention, or pattern on a standards page. It carries an identifier, a strength, and a verification line.

Home: docs/core/authoring.md

Source: LiteSpec.

### Rate Limit

One bound on how often an exposed endpoint accepts requests.

Home: docs/backend/api.md

Source: web API practice.

### Reachable

The computed state of an aggregate state that a transition leads into.

Home: docs/core/domain-model.md

Source: LiteSpec.

### Reader

The stack reader that reads code for the model and the checks. A person reading a documentation page is not a reader in this sense.

Home: docs/core/layers.md

Source: LiteSpec.

### Reads As

The phrase stating how one link reads from its record. Its pair states how the link reads back.

Layer: Behavior.

Home: docs/core/model.md

Source: LiteSpec.

### Record File

One YAML file that holds one record page with its entries.

Home: docs/core/model.md

Source: LiteSpec.

### Record Layer

One of the four homes of records. The layers are Intent, Behavior, Implementation, and Proof.

Home: docs/core/layers.md

Source: LiteSpec.

### Record-Driven Delivery

Delivery in which records select work and define completion.

Home: docs/learn/how-litespec-works.md

Source: LiteSpec.

### Recovery Objectives

The accepted bounds on time and data loss for a restore.

Home: docs/quality/operations.md

Source: the SRE book.

### Reference Page

One page of exact values, written for a reader to look up.

Home: docs/core/authoring.md

Source: Diataxis.

### Roll-Forward

Recovery through deploying a newer version rather than restoring the old one.

Home: docs/quality/operations.md

Source: release practice.

### Scope

The part of a project a provision applies to. It is stated in `Applies to` when narrower than the page.

Home: docs/core/scopes.md

Source: LiteSpec.

### Secret Inventory

The list of production secrets. Each entry names its location and rotation date.

Home: docs/quality/security.md

Source: security practice.

### Shape

One deployment boundary of a project: `server` or `client-only`.

Home: docs/core/scope.md

Source: LiteSpec.

### Size

One of the required-record sets of a project: `small` or `large`.

Home: docs/core/scope.md

Source: LiteSpec.

### Source Material

Research kept outside the documentation. It carries no authority on its own.

Home: docs/core/intent.md

Source: LiteSpec.

### Specified

The completion stage of a record named in the specification that has no code and no passing proof.

Home: docs/core/model.md

Source: LiteSpec.

### Stack

One technology binding of the specification. It is held in one folder.

Home: docs/core/stacks.md

Source: LiteSpec.

### Stale

The computed state of a proof whose run commit is behind HEAD with a code change since: a path changed between the run commit and the working tree that is neither under the LiteSpec folder nor a Markdown file. It reads `last passed at <short commit>, <N> commits ago`.

Home: docs/core/model.md

Source: LiteSpec.

### Standard

One identified provision that states a required boundary for consumers.

Home: docs/core/authoring.md

Source: LiteSpec.

### Statement

The one sentence in a provision that carries its obligation.

Home: docs/core/authoring.md

Source: LiteSpec.

### Strength

One of the three provision strengths: `required`, `default`, or `advisory`.

Home: docs/core/scopes.md

Source: LiteSpec.

### Sub-Processor

One provider that processes personal data for the product.

Home: docs/core/privacy.md

Source: the GDPR.

### Task Authorizer

The person who assigns the agent its task.

Home: docs/core/agent.md

Source: LiteSpec.

### Tested

The computed state of a record with a passing test that asserts something linked to it.

Home: docs/core/model.md

Source: LiteSpec.

### Tutorial

One page taking a reader to a first working result.

Home: docs/core/authoring.md

Source: Diataxis.

### Underneath

The command page section that states what the command runs.

Home: docs/core/authoring.md

Source: LiteSpec.

### Unfulfilled

The completion stage of a record the specification names, but which nothing yet specifies, codes, or verifies.

Home: docs/core/model.md

Source: LiteSpec.

### Verified

The computed state of a use case whose every scenario has a passing proof through the public boundary that is not stale. A requirement is verified when everything fulfilling it is verified and its measure's check passes.

Home: docs/core/model.md

Source: LiteSpec.

### Workspace

The repository holding a consumer application or the standards.

Home: docs/workspace/structure.md

Source: LiteSpec.

Rejected: repo, which shortens the word every path already shortens.
