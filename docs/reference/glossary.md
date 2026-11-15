# Glossary

## Code Terms

These terms name the code shapes that fulfill the specification.

### Aggregate Root

The single type through which outside code changes an aggregate.

Home: docs/backend/domain.md

Source: DDD.

### AppHost

The local orchestrator that starts every application and its dependencies together for development.

Home: docs/backend/dotnet/architecture.md

Source: .NET Aspire.

### Architecture Layer

One role a backend type plays, whatever folder or project holds it: Domain, Application, Infrastructure, or WebApi.

Home: docs/backend/architecture.md

Source: LiteSpec.

### Authorization Policy

One rule deciding whether an actor is allowed to perform an action on a target.

Home: docs/core/access.md

Source: RBAC.

### Caller

The party making the current call, distinct from the Actor record holding roles.

Home: docs/core/access.md

Source: LiteSpec.

### Central Package Management

One central file pinning every dependency version in the workspace.

Home: docs/workspace/dependencies.md

Source: NuGet.

### Child Entity

One entity inside an aggregate, changed only through its root.

Home: docs/backend/domain.md

Source: DDD.

### Domain Event

One immutable statement that a relevant fact completed inside the bounded context, where a published fact crossing it is an integration event.

Home: docs/core/domain-model.md

Source: DDD.

### Domain Repository

One boundary interface that loads and stages complete aggregates.

Home: docs/backend/persistence.md

Source: DDD.

Rejected: repository used alone, which hides whether the boundary loads a whole aggregate or a row.

### Endpoint

One class serving one route operation, converting HTTP input to an Application message and mapping the result back.

Home: docs/backend/api.md

Source: web API practice.

### Entity

One object defined by its identity rather than by its values.

Home: docs/backend/domain.md

Source: DDD.

### Input Validation

Checks on the shape of a command or query, run before business behavior runs.

Home: docs/backend/application.md

Source: LiteSpec.

### Layer Claim

The record layer a project claims in `layers`, with the practice it follows in that layer: `intent` and `behavior` take `litespec`, and `implementation` and `proof` take `litespec` or `own`.

Home: docs/core/scope.md

Source: LiteSpec.

### Layer Projects

The backend layout with one project per architecture layer, selected through the layer-projects extension.

Home: docs/backend/dotnet/ext/layer-projects.md

Source: LiteSpec.

### Lockfile

The committed file recording the exact resolved dependency versions.

Home: docs/workspace/dependencies.md

Source: the package managers.

### Module Layout

The default backend layout, with every module in the modules project beside the shared kernel project, layer folders at each node, one `UseCases/` folder per aggregate and module, and one flat folder per use case inside it.

Home: docs/backend/dotnet/architecture.md

Source: LiteSpec.

### Operation

One route operation, carrying its stable operation id and served by one endpoint class.

Home: docs/backend/api.md

Source: web API practice.

### Page File

The framework file that serves one address: `+page.svelte` with its `+page.ts` on Svelte, one route file on React. A page file renders one screen; the screen is the record, the route is the address, and the page file is the code.

Home: docs/frontend/web/structure.md

Source: LiteSpec.

### Persistence Constraint

One rule enforced by durable storage, such as uniqueness.

Home: docs/backend/persistence.md

Source: LiteSpec.

### Projection

One read model derived from authoritative facts.

Home: docs/backend/persistence.md

Source: CQRS.

### Read Model

One record holding a module's own copy of another module's state, filled from published integration events and shaped for a query.

Home: docs/core/automation.md

Source: CQRS.

Rejected: view, which collides with UI and database views.

### Route

One HTTP path with its method, resolving to the one endpoint class serving it.

Home: docs/backend/api.md

Source: web API practice.

### Trace Context

The trace and span identifiers carried across logs and calls.

Home: docs/quality/operations.md

Source: W3C Trace Context.

### Value Object

One immutable type defined by its values rather than by identity.

Home: docs/backend/domain.md

Source: DDD.

## Product and Domain Terms

These terms name the records and domain concepts a specification is written in.

### Access

One risk value marking permissions that decide who may act.

Home: docs/core/access.md

Source: LiteSpec.

### Act

One run of behavior that changes state, at which each value it copies from elsewhere freezes.

Home: docs/core/use-cases.md

Source: LiteSpec.

### Actor

One person or organization that acts in the product. An actor records whether it belongs to the team building and running the product. An external system is no actor, and internal machinery is no actor.

Layer: Intent.

Home: docs/core/intent.md

Source: RBAC.

### Aggregate

One unit of business state that stays consistent and changes in one step. Its kind is entity or store.

Layer: Behavior.

Home: docs/core/domain-model.md

Source: DDD.

Rejected: entity, which in DDD also names a child inside an aggregate.

### Aggregate Data

The named fields of an aggregate, each with a meaning, a data class, and a classification of public, internal, personal, or sensitive.

Layer: Behavior.

Home: docs/core/domain-model.md

Source: LiteSpec.

### Anonymize

One erasure position after which data can no longer identify anyone.

Layer: Behavior.

Home: docs/core/privacy.md

Source: the GDPR.

### Automation

The work that runs without a request, stated as reactions and jobs.

Home: docs/core/automation.md

Source: LiteSpec.

### Behavior

The record layer stating how the system keeps the promises Intent states.

Home: docs/core/behavior.md

Source: LiteSpec.

Rejected: behaviour, the British spelling.

### Bounded Context

The boundary inside which one domain model and one glossary hold.

Home: docs/core/scope.md

Source: DDD.

### Brand

The stated character of the product every screen keeps: its reader, what it is accountable for, the three words it is and the three it is not, and the rules every screen keeps.

Layer: Intent.

Home: docs/core/intent.md

Source: LiteSpec.

### Calculation

One rule stating how a value is derived, proven by a scenario with an examples table.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: the Business Rules Group.

### Cites

The reason link a record carries to the records behind it.

Layer: Intent or Behavior.

Home: docs/core/model.md

Source: LiteSpec.

### Command

One use case kind that changes business state.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: CQRS.

### Conclusion

The one sentence a research record concludes, stated as data so a decision can cite it.

Layer: Intent.

Home: docs/core/intent.md

Source: LiteSpec.

### Consistency

The declared write shape of a command that writes more than one aggregate: atomic or per-record.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: LiteSpec.

### Constraint

One outside rule the product obeys, such as a law or provider terms, with its authority, its kind, and the date it was read.

Layer: Intent.

Home: docs/core/intent.md

Source: plain English.

### Data Class

One named group of aggregate fields sharing one classification, one purpose, one erasure position, and one retention period with its basis and enforcement.

Layer: Behavior.

Home: docs/core/privacy.md

Source: LiteSpec.

### Decision

One dated choice with options weighed and evidence cited.

Layer: Intent.

Home: docs/core/intent.md

Source: decision records.

### Density

How much a screen shows at once, stated per entry point as public-light, application-balanced, or admin-dense.

Layer: Behavior.

Home: docs/core/edges.md

Source: LiteSpec.

### Departure

One recorded departure from a provision or a check, with its reason, the provisions, pages or finding codes it covers, the records or paths it covers, and an expiry date with an exit when the team means to close it.

Layer: Intent or Behavior.

Home: docs/core/departures.md

Source: LiteSpec.

### Domain

The real-world knowledge, language, rules, state, and behavior the software models.

Home: docs/backend/domain.md

Source: DDD.

### Effect

One declared edge from a use case, a reaction, or a job to a command, a notification, an integration event, or an outside call.

Layer: Behavior.

Home: docs/core/automation.md

Source: LiteSpec.

### Entry Point

One way from outside through which people or systems reach the product.

Layer: Behavior.

Home: docs/core/edges.md

Source: LiteSpec.

### Entry Point Set

One page holding the entry points of one module, with each point's kind and authentication.

Layer: Behavior.

Home: docs/core/edges.md

Source: LiteSpec.

### Evidence

One dated claim behind a problem, with its source and its kind: observed, reported or assumed. The strongest kind present is computed and never written.

Layer: Intent.

Home: docs/core/intent.md

Source: LiteSpec.

### Example

One narrative showing behavior on a real occasion, drawn from the example cast.

Home: docs/core/writing.md

Source: plain English.

### Example Cast

The people, place, dates, and amounts every example draws on.

Home: docs/core/writing.md

Source: plain English.

### Examples Table

One table of rows a scenario reads through, holding the values the parts argue about.

Home: docs/core/use-cases.md

Source: BDD.

### Excludes

The product-level list of plain-sentence exclusions, the only statements that override a requirement.

Layer: Intent.

Home: docs/core/intent.md

Source: LiteSpec.

### External System

One outside system the product exchanges messages with. It stands alone and names no actor; a system calling the product reaches it through an API entry point.

Layer: Behavior.

Home: docs/core/edges.md

Source: LiteSpec.

### Failure Policy

The `onFailure` handling a reaction, job, or effect declares, with a mode of `retry`, `skip-and-log`, or `discard` and the retry, dead-letter, surface, and runbook its mode states.

Layer: Behavior.

Home: docs/core/automation.md

Source: LiteSpec.

### Feature

One standalone answer to one or more problems, advancing a goal and holding its requirements.

Layer: Intent.

Home: docs/core/intent.md

Source: plain English.

### Flow

The ordered steps by which one actor reaches one goal it serves or one problem or constraint it answers, with a description of who takes it, what starts it, what good looks like and the consequence at stake.

Layer: Intent.

Home: docs/core/intent.md

Source: plain English.

### Flow Branch

A path that leaves the order of a flow's steps at one step, named by the step it leaves, the condition under which it leaves, and the step it continues at, or end, and stating no action of its own.

Layer: Intent.

Home: docs/core/intent.md

Source: plain English.

### Freshness

How current a query group or read model is: its mode of immediate, snapshot, or eventual, with the duration within which it holds.

Layer: Behavior.

Home: docs/core/automation.md

Source: LiteSpec.

### Functional Requirement

One requirement stating what the product does.

Layer: Intent.

Home: docs/core/intent.md

Source: BABOK and IREB.

### Generic Refusal

One refusal the model derives instead of a written result: forbidden, invalid input, not found, conflict, unauthenticated, or sign-in-too-old.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: LiteSpec.

### Glossary

The project glossary: each term, its meaning, and its rejected synonyms, held as data in glossary.yaml.

Home: docs/core/language.md

Source: arc42; DDD's ubiquitous language.

### Goal

One measurable outcome a feature advances and a flow serves or a quality requirement confirms, carrying no authority.

Layer: Intent.

Home: docs/core/intent.md

Source: Volere and product practice.

### Idempotency

The declared way a repeated command collapses to one effect, stating the mode of `key`, `state`, or `none` and what a recognized replay does.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: LiteSpec.

### Implementation

The record layer holding code and technical decisions.

Home: docs/core/layers.md

Source: LiteSpec.

### Integration Event

One published fact crossing the bounded context, with its direction, payload, trust, and the external system it travels through.

Layer: Behavior.

Home: docs/core/edges.md

Source: DDD.

### Intent

The record layer stating why the product exists and what it promises.

Home: docs/core/intent.md

Source: LiteSpec.

### Invariant

One rule that holds at every observable moment.

Layer: Behavior.

Home: docs/core/domain-model.md

Source: DDD.

### Job

One record stating work that runs on a schedule or once, declaring the outcomes each run ends in.

Layer: Behavior.

Home: docs/core/automation.md

Source: LiteSpec.

### Measured Requirement

One quality requirement carrying a measure, a target, and a check that confirms them.

Layer: Intent.

Home: docs/core/intent.md

Source: Volere.

### Module

One business area organizing language, records, and use cases.

Layer: Behavior.

Home: docs/core/behavior.md

Source: DDD.

### Money

One risk value marking permissions that move, hold, or decide money.

Home: docs/core/access.md

Source: LiteSpec.

### Navigation Map

The drawing of one entry point's screens as places and the connections between them, drawn by the book from `leadsTo`.

Home: docs/tools/book.md

Source: LiteSpec.

### Notification

One person-facing message with its audience, channel, consent rule, and claims, delivered through one external system.

Layer: Behavior.

Home: docs/core/edges.md

Source: LiteSpec.

### Observation

One dated measured value on a goal, with its source, showing the outcome reached apart from delivery.

Layer: Intent.

Home: docs/core/intent.md

Source: LiteSpec.

### Outcome

One named way an operation ends, answered with a status at the API boundary or with one of the closed run states of a reaction or job.

Layer: Behavior.

Home: docs/core/edges.md

Source: LiteSpec.

### Page Frame

One named screen layout from the page-frame pattern catalog, named on each screen record with the option the product chose.

Layer: Behavior.

Home: docs/frontend/patterns/page-frames.md

Source: LiteSpec.

### Permission

One allowed action from the shared closed list.

Layer: Behavior.

Home: docs/core/access.md

Source: RBAC.

### Permission Set

One page holding the permissions of one module, with the risk each permission carries.

Layer: Behavior.

Home: docs/core/access.md

Source: LiteSpec.

### Personal Data

One risk value marking permissions that keep personal or sensitive data.

Home: docs/core/access.md

Source: LiteSpec.

### Physical Access

One risk value marking permissions that control who passes a physical boundary.

Home: docs/core/access.md

Source: LiteSpec.

### Policy Requirement

One requirement stating a policy the business holds, with effective dates where the value changes over time.

Layer: Intent.

Home: docs/core/intent.md

Source: LiteSpec.

### Precondition

One rule that holds before a use case runs.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: Design by Contract.

### Problem

One pain a person or organization outside the team building the product has today, with its primary sufferer, its cost today, its workaround today and its graded evidence.

Layer: Intent.

Home: docs/core/intent.md

Source: plain English.

### Product

The software capability offered to users, with its boundary and the exclusions that override its requirements.

Layer: Intent.

Home: docs/core/intent.md

Source: plain English.

### Product Requirement

One requirement with no offer behind it, sitting at product level and citing the constraint, goal, or problem it comes from.

Layer: Intent.

Home: docs/core/intent.md

Source: LiteSpec.

### Pseudonymize

One erasure position that keeps personal data personal while replacing direct identifiers.

Layer: Behavior.

Home: docs/core/privacy.md

Source: the GDPR.

Rejected: pseudonymise, the British spelling.

### Purpose

The reason personal data is kept, stated on each data class.

Layer: Behavior.

Home: docs/core/privacy.md

Source: the GDPR.

### Quality Requirement

One requirement stating how well the product performs, with a measure and a target.

Layer: Intent.

Home: docs/core/intent.md

Source: BABOK and IREB.

### Query

One use case kind that reads a read model without changing business state.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: CQRS.

### Reaction

One record stating work that runs when a domain or integration event arrives, declaring the outcomes each run ends in.

Layer: Behavior.

Home: docs/core/automation.md

Source: LiteSpec.

### Record

One written unit of the specification, of a fixed type with fixed links.

Home: docs/core/model.md

Source: LiteSpec.

### Refusal

One named way a use case ends without its effect, listed in precedence order with first match winning.

Layer: Behavior.

Home: docs/core/use-cases.md

Aliases: rejection, error.

Source: LiteSpec.

### Refusal Code

One stable identifier a module's refusal set declares for a way a use case ends without its effect, in `<module>.<name>` form.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: LiteSpec.

### Refusal Sentence

The sentence a person reads when a refusal reaches them, carried on the refusal result in the product's language, with one sentence per language beside it.

Layer: Behavior.

Home: docs/core/edges.md

Source: LiteSpec.

### Refusal Set

One page holding the refusal codes of one module, with each code's meaning and problem type.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: LiteSpec.

### Requirement

One behavior the product shows or one guarantee it keeps. A departure names its fallback requirement; a tenant-set value is not a departure. Its cites names the goals, problems, features, constraints, research, and decisions behind it.

Layer: Intent.

Home: docs/core/intent.md

Source: BABOK and IREB.

Rejected: product rule, which clashes with the rule record.

### Research

One outside fact informing a choice.

Layer: Intent.

Home: docs/core/intent.md

Source: plain English.

Rejected: decision evidence, which states where the fact is used instead of what it is.

### Result

One way a use case ends: success or a named refusal. Refusals read in precedence order; a success may carry codes for recorded outcomes inside it. A refusal result carries a code from its module's refusal set. Its identifier is `result/<use-case>.<name>`.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: the Result pattern.

### Role

One named set of permissions granted together, at the one scope the role holds.

Layer: Intent.

Home: docs/core/access.md

Source: RBAC.

### Rule

One testable commitment stating what holds.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: the Business Rules Group.

### Rule Set

One page holding the rules of one module or aggregate, with each rule's condition and refusal.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: LiteSpec.

### Runbook

The steps to run or recover the product.

Layer: Implementation.

Home: docs/quality/operations.md

Source: the SRE book.

### Scenario

One testable example proving one behavior, stated as given, when, then and stored in the file of the use case, reaction, or job it exercises. A scenario may point at a derived result. Its identifier names the behavior it exercises, as `scenario/<use-case>.<name>`.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: BDD and Gherkin.

Rejected: acceptance criterion, which names the review use instead of the example.

### Screen

One page a person sees through a browser or device entry point, which serves a flow step only through a use case it offers.

Layer: Behavior.

Home: docs/core/edges.md

Source: LiteSpec.

### Setting

One named bound or default the product runs within, with its setter and its behavior at the bound. The use case, reaction, or job that reads it names it in `reads`.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: LiteSpec.

### Setting Set

One page holding the settings of one module, with each setting's bound or default.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: LiteSpec.

### Specification

The whole set of records describing one product.

Home: docs/core/model.md

Source: LiteSpec.

### State

One named stage in an aggregate lifecycle, with its meaning.

Layer: Behavior.

Home: docs/core/domain-model.md

Source: LiteSpec.

### Store

One aggregate kind holding state no invariant guards.

Layer: Behavior.

Home: docs/core/domain-model.md

Source: LiteSpec.

### Storyboard

The drawing of one flow as a strip of its steps' screens in step order, drawn by the book.

Home: docs/tools/book.md

Source: LiteSpec.

### Team Actor

One actor that belongs to the team building and running the product, marked on its record. A problem never names a team actor as its primary sufferer.

Layer: Intent.

Home: docs/core/intent.md

Source: LiteSpec.

### Tenant Scope

The declared reach of a use case grant across tenants: own, cross, public, or none.

Layer: Behavior.

Home: docs/core/access.md

Source: LiteSpec.

### Term

One word with one meaning, labels in each language, and rejected synonyms. Each term carries its layer and its established aliases.

Layer: Intent or Behavior.

Home: docs/core/language.md

Source: arc42; DDD's ubiquitous language.

### Token Role

One named use of color a token file values, such as text, card edge, or focus ring, with the limits its values meet in each declared scheme.

Home: docs/frontend/ui.md

Source: LiteSpec.

### Transition

One declared move between aggregate states, made by a use case or moved by time through a job.

Layer: Behavior.

Home: docs/core/domain-model.md

Source: LiteSpec.

### Trigger

The domain or integration event that starts a reaction, or the schedule that starts a job.

Layer: Behavior.

Home: docs/core/automation.md

Source: LiteSpec.

### Ubiquitous Language

The shared domain words the team and the model use alike.

Home: docs/core/language.md

Source: DDD.

### Use Case

One command or query with its access, results, and scenarios.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: Cockburn.

### User

The person the product serves, reached through a browser or device entry point.

Layer: Behavior.

Home: docs/core/edges.md

Source: LiteSpec.

### Verified Claim

One input value a provider checked that the caller can never supply, named by the entry point or external system that verified it.

Layer: Behavior.

Home: docs/core/use-cases.md

Source: LiteSpec.

### Vision

Where the product is heading beyond what it promises now, stated without a date or priority, which a decision keeping it open cites and nothing builds from.

Layer: Intent.

Home: docs/core/intent.md

Source: product vision practice.

### Voice

How the product addresses the audience of one entry point, stated on the entry point record in a sentence or two.

Layer: Behavior.

Home: docs/core/edges.md

Source: LiteSpec.

### Wireframe

One text drawing of a screen's structure in the wireframe language, held in the screen's explanation file.

Layer: Behavior.

Home: docs/frontend/patterns.md

Source: LiteSpec.

## Standard Terms

These terms name the parts of the LiteSpec standard itself.

### Agent

Software that reads task context, plans bounded work, changes the files it is allowed to change, and runs verification.

Home: docs/core/agent.md

Source: LiteSpec.

### Agent-Driven Engineering

The operating model where agents do engineering work and people keep decision authority.

Home: docs/core/agent.md

Source: LiteSpec.

### Alert Inventory

The list of alerts a product can fire, with the runbook each alert names.

Home: docs/quality/operations.md

Source: the SRE book.

### Audit Record

One entry stating actor, action, target, outcome, time, origin, and reason.

Home: docs/ext/audit.md

Source: NIST SP 800-53, control AU-3.

### Audit Trail

The append-only store that holds audit records, kept separate from logs and domain events.

Home: docs/ext/audit.md

Source: NIST SP 800-53.

### Background Caller

The reaction or job whose `runs` edge names a background-only use case.

Home: docs/core/automation.md

Source: LiteSpec.

### Background-Only

The computed state of a use case a reaction or job runs, needing no flow to reach it.

Home: docs/core/automation.md

Source: LiteSpec.

### Check Level

One of the three check levels: G reads records only, S reads code through a stack reader, and A reports; whether a finding blocks follows the strength of the provision it reports.

Home: docs/core/model.md

Source: LiteSpec.

### Checkable

One provision a tool checks, carrying a Checked by verification line.

Home: docs/core/authoring.md

Source: LiteSpec.

### Closed Specification

The specification with every record decided and every required link holding.

Home: docs/core/release.md

Source: LiteSpec.

### Coded

The computed state of a use case, rule, domain event, or aggregate state the stack's builder finds code for.

Home: docs/core/model.md

Source: LiteSpec.

### Command Page

One page that documents one shipped command: its arguments, options, exit codes, and what it runs underneath.

Home: docs/core/authoring.md

Source: LiteSpec.

### Completion Stage

One of the four stages of a record's completion: unfulfilled, specified, coded, or verified.

Home: docs/core/model.md

Source: LiteSpec.

### Component Inventory

The list of shipped components published with each release artifact.

Home: docs/workspace/dependencies.md

Source: supply-chain practice.

Rejected: SBOM, which names one format where the inventory names the content.

### Configuration Page

One page holding every setting a reader can change.

Home: docs/core/authoring.md

Source: LiteSpec.

### Consumer

One project built against a pinned LiteSpec release.

Home: docs/core/scope.md

Source: LiteSpec.

### Contract

One named set of obligations or shapes holding between parts, such as a page, a provision, a pattern, a release, or an API.

Home: docs/core/authoring.md

Source: LiteSpec.

### Controlled Technical Prose

The fixed writing profile that sets the allowed structures, prose bounds, exact terms, and testable provisions.

Home: docs/core/authoring.md

Source: LiteSpec.

### Convention

One replaceable default for a name, location, file shape, or pattern.

Home: docs/core/authoring.md

Source: LiteSpec.

### CORS

The browser rule that decides which origins can call the API.

Home: docs/backend/api.md

Source: the Fetch standard.

### Covered

The outcome of a finding a departure record covers: reported, and never blocking.

Home: docs/core/departures.md

Source: LiteSpec.

### Current Standards Material

The pinned release taken on its own, without transition or history material.

Home: docs/core/authoring.md

Source: LiteSpec.

### Derived Artifact

One committed file produced from an authored source, such as generated types.

Home: docs/workspace/structure.md

Source: LiteSpec.

### Diagnostic

One issue litespec-maintain validate reports about standards material.

Home: docs/maintain/validate.md

Source: LiteSpec.

### Document

One Markdown page under a record root that holds no record.

Home: docs/core/model.md

Source: LiteSpec.

### Documentation Layer

One depth of documentation, from a first working result down to the mechanism a command runs.

Home: docs/core/authoring.md

Source: Diataxis.

### Engineering System

One connected set of decisions, specifications, standards, verification, and release practices.

Home: docs/learn/how-litespec-works.md

Source: LiteSpec.

### Exercised

The computed state of a flow step with a passing proof of a use case, reaction, or job serving it.

Home: docs/core/model.md

Source: LiteSpec.

### Explanation

One page that builds understanding of what LiteSpec is and why it works the way it does.

Home: docs/core/authoring.md

Source: Diataxis.

### Explanation File

One Markdown file beside a record file that explains the record for people and agents.

Home: docs/core/writing.md

Source: LiteSpec.

### Extension

One conditional standards bundle a project selects when its criterion holds.

Home: docs/core/scope.md

Source: LiteSpec.

### Finding

One issue litespec model check reports, with its level, code, message, path, and line.

Home: docs/tools/model.md

Source: LiteSpec.

### Flaky

The computed state of a proof whose test both passed and failed within the runs read.

Home: docs/core/model.md

Source: LiteSpec.

### Frontmatter

The opening block of a document stating its page style.

Home: docs/core/authoring.md

Source: LiteSpec.

### Governance

The group of records beside the four record layers, holding departures.

Home: docs/core/departures.md

Source: LiteSpec.

### Guidance

One provision a person judges, carrying a Reviewed for verification line.

Home: docs/core/authoring.md

Source: LiteSpec.

### How-To Page

One page that solves one stated problem for a reader who already has a working result.

Home: docs/core/authoring.md

Source: Diataxis.

### Index

One page routing readers to the pages of its section.

Home: docs/core/authoring.md

Source: LiteSpec.

### License

One SPDX identifier naming the terms a shipped package carries.

Home: docs/workspace/dependencies.md

Source: SPDX.

Rejected: licence and licences, the British spellings.

### LiteSpec

The standards contract for specifying, building, verifying, releasing, and operating software with agents.

Home: docs/README.md

Source: LiteSpec.

### LiteSpec Release

One published, pinned, complete standards contract.

Home: docs/core/authoring.md

Source: LiteSpec.

### Mark

One written link from a code element or a test, of kind `implements` naming the record it serves or `follows` naming the provision, decision, or departure shaping it, holding the identifier in the project mark style.

Home: docs/core/layers.md

Source: LiteSpec.

Rejected: anchor and tag, which name the link by one of its forms.

### Normative

Required of a conforming consumer through an identified provision.

Home: docs/core/authoring.md

Source: standards practice, as the W3C QA Framework and ISO Directives use it.

### Page

One authored record file or document under the documentation root.

Home: docs/core/authoring.md

Source: LiteSpec.

### Page Segment

The part of a provision ID naming its owning page.

Home: docs/core/authoring.md

Source: LiteSpec.

### Page Style

One page template for prose or record lists.

Home: docs/core/authoring.md

Source: LiteSpec.

Rejected: page class, which reads as a code type.

### Pattern

One named answer to a recurring interface question, with its options and wireframes.

Home: docs/frontend/patterns.md

Source: LiteSpec.

### Preset

One ready-made selection of shape, stacks, and frontends for a new project.

Home: docs/tools/init.md

Source: LiteSpec.

### Project

One bounded-context business application built with LiteSpec.

Home: docs/core/scope.md

Source: LiteSpec.

### Proof Hash

The hash of a scenario or of its test body recorded in `.litespec/proof/proofs.json` at the last passing run.

Home: docs/core/model.md

Source: LiteSpec.

### Provision

One rule, convention, or pattern on a standards page, carrying an identifier, a strength, and a verification line.

Home: docs/core/authoring.md

Source: LiteSpec.

### Rate Limit

One bound on request frequency at an exposed endpoint.

Home: docs/backend/api.md

Source: web API practice.

### Reachable

The computed state of an aggregate state a transition leads into.

Home: docs/core/domain-model.md

Source: LiteSpec.

### Reader

The stack reader that reads code for the model and the checks. The reader of a documentation page is not a reader in this sense.

Home: docs/core/layers.md

Source: LiteSpec.

### Reads As

The phrase stating how one link reads from its record, with its pair stating how it reads back.

Layer: Behavior.

Home: docs/core/model.md

Source: LiteSpec.

### Record File

One YAML file holding one record page with its entries.

Home: docs/core/model.md

Source: LiteSpec.

### Record Layer

One of the four homes of records: Intent, Behavior, Implementation, or Proof.

Home: docs/core/layers.md

Source: LiteSpec.

### Record-Driven Delivery

Delivery where records select work and define completion.

Home: docs/learn/how-litespec-works.md

Source: LiteSpec.

### Recovery Objectives

The accepted time and data loss bounds for a restore.

Home: docs/quality/operations.md

Source: the SRE book.

### Reference Page

One page of exact values a reader looks up.

Home: docs/core/authoring.md

Source: Diataxis.

### Roll-Forward

Recovery by deploying a newer version rather than restoring the old one.

Home: docs/quality/operations.md

Source: release practice.

### Scope

The part of a project a provision applies to, stated in Applies to when narrower than the page.

Home: docs/core/scopes.md

Source: LiteSpec.

### Secret Inventory

The list of production secrets, each with its location and rotation date.

Home: docs/quality/security.md

Source: security practice.

### Shape

One deployment boundary of a project: server or client-only.

Home: docs/core/scope.md

Source: LiteSpec.

### Size

One of the required-record sets of a project: small or large.

Home: docs/core/scope.md

Source: LiteSpec.

### Source Material

Research kept outside the documentation, which carries no authority on its own.

Home: docs/core/intent.md

Source: LiteSpec.

### Specified

The completion stage of a record that is named in the specification but has no code and no passing proof.

Home: docs/core/model.md

Source: LiteSpec.

### Stack

One technology binding of the specification, held in one folder.

Home: docs/core/stacks.md

Source: LiteSpec.

### Stale

The computed state of a proof whose scenario, test body, or marked file changed after its recorded run.

Home: docs/core/model.md

Source: LiteSpec.

### Standard

One identified provision stating a required boundary for consumers.

Home: docs/core/authoring.md

Source: LiteSpec.

### Statement

The one sentence in a provision carrying its obligation.

Home: docs/core/authoring.md

Source: LiteSpec.

### Strength

One of the three provision strengths: required, default, or advisory.

Home: docs/core/scopes.md

Source: LiteSpec.

### Sub-Processor

One provider processing personal data for the product.

Home: docs/core/privacy.md

Source: the GDPR.

### Task Authorizer

The person who gave the agent its task.

Home: docs/core/agent.md

Source: LiteSpec.

### Tested

The computed state of a record with a passing test that asserts something linking to it.

Home: docs/core/model.md

Source: LiteSpec.

### Tutorial

One page that takes a reader to a first working result.

Home: docs/core/authoring.md

Source: Diataxis.

### Underneath

The command page section stating what the command runs.

Home: docs/core/authoring.md

Source: LiteSpec.

### Unfulfilled

The completion stage of a record the specification names but nothing yet specifies, codes or verifies.

Home: docs/core/model.md

Source: LiteSpec.

### Verified

The computed state of a use case whose every scenario has a passing proof through the public boundary that is not stale, and of a requirement everything fulfilling it verifies, with its measure's check passing.

Home: docs/core/model.md

Source: LiteSpec.

### Workspace

The repository that holds a consumer application or the standards.

Home: docs/workspace/structure.md

Source: LiteSpec.

Rejected: repo, which shortens the word every path already shortens.
