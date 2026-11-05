# Glossary

## Code Terms

These terms name the code shapes that fulfill the specification.

### Aggregate Root

The single type through which outside code changes an aggregate.

Source: DDD.

### AppHost

The local orchestrator that starts every application and its dependencies together for development.

Source: .NET Aspire.

### Architecture Layer

One role a backend type plays, whatever folder or project holds it: Domain, Application, Infrastructure, or WebApi.

Source: LiteSpec.

### Authorization Policy

One rule deciding whether an actor is allowed to perform an action on a target.

Source: RBAC.

### Caller

The party making the current call, distinct from the Actor record holding roles.

Source: LiteSpec.

### Central Package Management

One central file pinning every dependency version in the workspace.

Source: NuGet.

### Child Entity

One entity inside an aggregate, changed only through its root.

Source: DDD.

### Domain Event

One immutable record that describes a fact completed inside the bounded context.

Source: DDD.

### Domain Repository

One boundary interface that loads and stages complete aggregates.

Source: DDD.

Rejected: repository used alone, which hides whether the boundary loads a whole aggregate or a row.

### Endpoint

One class serving one route operation, converting HTTP input to an Application message and mapping the result back.

Source: web API practice.

### Entity

One object defined by its identity rather than by its values.

Source: DDD.

### Input Validation

Checks on the shape of a command or query, run before business behavior runs.

Source: LiteSpec.

### Layer Projects

The backend layout with one project per architecture layer, selected through the layer-projects extension.

Source: LiteSpec.

### Lockfile

The committed file recording the exact resolved dependency versions.

Source: the package managers.

### Module Layout

The default backend layout, with every module in the modules project beside the shared kernel project, layer folders at each node, one `UseCases/` folder per aggregate and module, and one flat folder per use case inside it.

Source: LiteSpec.

### Operation

One route operation, carrying its stable operation id and served by one endpoint class.

Source: web API practice.

### Page File

The framework file that serves one address: `+page.svelte` with its `+page.ts` on Svelte, one route file on React. A page file renders one screen; the screen is the record, the route is the address, and the page file is the code.

Source: LiteSpec.

### Persistence Constraint

One rule enforced by durable storage, such as uniqueness.

Source: LiteSpec.

### Projection

One read model derived from authoritative facts.

Source: CQRS.

### Read Model

Data shaped for a query, served without loading an aggregate.

Source: CQRS.

### Route

One HTTP path with its method, resolving to the one endpoint class serving it.

Source: web API practice.

### Trace Context

The trace and span identifiers carried across logs and calls.

Source: W3C Trace Context.

### Value Object

One immutable type defined by its values rather than by identity.

Source: DDD.

## Product and Domain Terms

These terms name the records and domain concepts a specification is written in.

### Access

One risk value marking permissions that decide who may act.

Source: LiteSpec.

### Actor

One person or organization that acts in the product. An actor records whether it belongs to the team building and running the product. An external system is no actor, and internal machinery is no actor.

Layer: Intent.

Source: RBAC.

### Aggregate

One unit of business state that stays consistent and changes in one step. Its kind is entity, saga, or store.

Layer: Behavior.

Source: DDD.

Rejected: entity, which in DDD also names a child inside an aggregate.

### Aggregate Data

The named fields of an aggregate, each with a meaning, a data class, and a classification of public, internal, personal, or sensitive.

Layer: Behavior.

Source: LiteSpec.

### Anonymize

One erasure position after which data can no longer identify anyone.

Layer: Behavior.

Source: the GDPR.

### Automation

The work that runs without a request, stated as reactions and jobs.

Source: LiteSpec.

### Behavior

The record layer stating how the system keeps the promises Intent states.

Source: LiteSpec.

Rejected: behaviour, the British spelling.

### Bounded Context

The boundary inside which one domain model and one glossary hold.

Source: DDD.

### Calculation

One rule stating how a value is derived, proven by a scenario with an examples table.

Layer: Behavior.

Source: the Business Rules Group.

### Cites

The reason link a record carries to the records behind it.

Layer: Intent or Behavior.

Source: LiteSpec.

### Command

One use case kind that changes business state.

Layer: Behavior.

Source: CQRS.

### Compensation

The per-step undo of a saga: the use case that reverses one step, or none with its reason.

Layer: Behavior.

Aliases: undo.

Source: LiteSpec.

### Conclusion

The one sentence a research record concludes, stated as data so a decision can cite it.

Layer: Intent.

Source: LiteSpec.

### Consistency

The declared write shape of a use case: atomic, per-record, or compensated.

Layer: Behavior.

Source: LiteSpec.

### Constraint

One outside rule the product obeys, such as a law or provider terms, with its authority, its kind, and the date it was read.

Layer: Intent.

Source: plain English.

### Cross-Aggregate Rule

One rule spanning more than one aggregate.

Layer: Behavior.

Source: LiteSpec.

### Data Class

One named group of aggregate fields sharing one classification, one purpose, and one erasure position.

Layer: Behavior.

Source: LiteSpec.

### Decision

One dated choice with options weighed and evidence cited.

Layer: Intent.

Source: decision records.

### Domain

The real-world knowledge, language, rules, state, and behavior the software models.

Source: DDD.

### Effect

One declared edge from a use case, a reaction, or a job to a command, a notification, an integration event, or an outside call.

Layer: Behavior.

Source: LiteSpec.

### Entry Point

One door from outside through which people or systems reach the product.

Layer: Behavior.

Source: LiteSpec.

### Event

One immutable statement that a relevant fact completed. The domain event stays inside the bounded context; a published fact is an integration event.

Layer: Behavior.

Source: DDD.

### Evidence

One dated claim behind a problem, with its source and its kind: observed, reported or assumed. The strongest kind present is computed and never written.

Layer: Intent.

Source: LiteSpec.

### Example

One narrative showing behavior on a real occasion, drawn from the example cast.

Source: plain English.

### Example Cast

The people, place, dates, and amounts every example draws on.

Source: plain English.

### Examples Table

One table of rows a scenario walks through, holding the values the parts argue about.

Source: BDD.

### Exception

One recorded departure from a provision or one check held red, with its reason and expiry date.

Layer: Intent or Behavior.

Source: LiteSpec.

Rejected: deviation and known gap, which named two records where one holds both.

### External System

One outside system the product exchanges messages with. It stands alone and names no actor; a system calling the product reaches it through an API entry point.

Layer: Behavior.

Source: LiteSpec.

### Failure Policy

The `onFailure` handling a reaction, job, or effect declares, with a mode of `retry`, `skip-and-log`, or `discard` and the retry, dead-letter, surface, and runbook its mode states.

Layer: Behavior.

Source: LiteSpec.

### Feature

One answer to a problem under a module, advancing a goal and solving a problem.

Layer: Intent.

Source: plain English.

### Freshness

How current a query group is: immediate, snapshot, bounded by a duration, or advisory.

Layer: Behavior.

Source: LiteSpec.

### Functional Requirement

One requirement stating what the product does.

Layer: Intent.

Source: BABOK and IREB.

### Generic Refusal

One refusal the builder derives from access and input instead of a written result: forbidden or invalid input.

Layer: Behavior.

Source: LiteSpec.

### Glossary

The project glossary: each term, its meaning, and its rejected synonyms, held as data in glossary.yaml.

Source: arc42; DDD's ubiquitous language.

### Goal

One measurable outcome a feature advances and a journey serves or a quality requirement confirms, carrying no authority.

Layer: Intent.

Source: Volere and product practice.

### Idempotency

The declared way a repeated call collapses to one effect, stated as a mode of `key`, `state`, `not-pending`, or `none` with the key, scope, or reason its mode needs.

Layer: Behavior.

Source: LiteSpec.

### Implementation

The record layer holding code and technical decisions.

Source: LiteSpec.

### Integration Event

One published fact crossing the bounded context, with its direction, payload, and trust.

Layer: Behavior.

Source: DDD.

### Intent

The record layer stating why the product exists and what it promises.

Source: LiteSpec.

### Invariant

One rule that holds at every observable moment.

Layer: Behavior.

Source: DDD.

### Job

One record stating work that runs on a schedule, once, or by hand.

Layer: Behavior.

Source: LiteSpec.

### Journey

The ordered steps by which one actor reaches one goal, with a description of who walks, what starts it, what good looks like and why it matters. A step is performed by an actor, or by a named external system, job or reaction when no person acts. Steps name no use case; each serving use case points up with serves.

Layer: Intent.

Source: plain English.

### Measured Requirement

One quality requirement carrying a measure, a target, and a check that confirms them.

Layer: Intent.

Source: Volere.

### Module

One business area organizing language, records, and use cases.

Layer: Behavior.

Source: DDD.

### Money

One risk value marking permissions that move, hold, or decide money.

Source: LiteSpec.

### Notification

One person-facing message with its addresses, channel, consent, and claims.

Layer: Behavior.

Source: LiteSpec.

### Outcome

One named success or failure kind at the API boundary, answered with the status the outcome table assigns it.

Layer: Behavior.

Source: LiteSpec.

### Permission

One allowed action from the shared closed list.

Layer: Behavior.

Source: RBAC.

### Personal Data

One risk value marking permissions that keep personal or sensitive data.

Source: LiteSpec.

### Physical Access

One risk value marking permissions that control who passes a physical boundary.

Source: LiteSpec.

### Policy Requirement

One requirement stating a policy the business holds, with effective dates where the value changes over time.

Layer: Intent.

Source: LiteSpec.

### Precondition

One rule that holds before a use case runs.

Layer: Behavior.

Source: Design by Contract.

### Problem

One pain a person or organization outside the team building the product has today, with its primary sufferer, its cost today, its workaround today and its graded evidence.

Layer: Intent.

Source: plain English.

### Product

The software capability offered to users, with its boundary.

Layer: Intent.

Source: plain English.

### Pseudonymize

One erasure position that keeps personal data personal while replacing direct identifiers.

Layer: Behavior.

Source: the GDPR.

Rejected: pseudonymise, the British spelling.

### Purpose

The reason personal data is kept, stated on each data class and retention row.

Layer: Behavior.

Source: the GDPR.

### Qualified Identifier

The display form of a record identifier with its placement, built from the identifier's segments plus placement. Links never use it.

Source: LiteSpec.

### Quality Requirement

One requirement stating how well the product performs, with a measure and a target.

Layer: Intent.

Source: BABOK and IREB.

### Query

One use case kind that reads a read model without changing business state.

Layer: Behavior.

Source: CQRS.

### Reaction

One record stating work that runs when a domain or integration event arrives.

Layer: Behavior.

Source: LiteSpec.

### Record

One written unit of the specification, of a fixed type with fixed links.

Source: LiteSpec.

### Refusal

One named way a use case ends without its effect, listed in precedence order with first match winning.

Layer: Behavior.

Aliases: rejection, error.

Source: LiteSpec.

### Refusal Code

The stable identifier of one refusal, in identifier form.

Layer: Behavior.

Source: LiteSpec.

### Requirement

One behavior the product shows or one guarantee it keeps. An exception names its fallback requirement; a tenant-set value is not an exception. Its cites names the goals, problems, features, constraints, research, and decisions behind it.

Layer: Intent.

Source: BABOK and IREB.

Rejected: product rule, which clashes with the rule record.

### Research

One outside fact informing a choice.

Layer: Intent.

Source: plain English.

Rejected: decision evidence, which states where the fact is used instead of what it is.

### Result

One way a use case ends: success or a named refusal. Refusals read in precedence order; a success may carry codes for recorded outcomes inside it. Its identifier is `result/<use-case>.<name>`.

Layer: Behavior.

Source: the Result pattern.

### Retention Schedule

The one page mapping each data class to its basis, purpose, period, enforcement, and cites.

Layer: Behavior.

Source: the GDPR.

### Role

One named set of permissions granted to an actor at a scope.

Layer: Intent.

Source: RBAC.

### Rule

One testable commitment stating what holds.

Layer: Behavior.

Source: the Business Rules Group.

### Runbook

The steps to run or recover the product.

Layer: Implementation.

Source: the SRE book.

### Saga

One coordinating aggregate whose steps run use cases on other aggregates, each with its compensation.

Layer: Behavior.

Aliases: process manager.

Source: LiteSpec.

### Scenario

One testable example proving one result, stated as given, when, then. A scenario may point at a derived result. Its identifier is `scenario/<use-case>.<name>`.

Layer: Behavior.

Source: BDD and Gherkin.

Rejected: acceptance criterion, which names the review use instead of the example.

### Screen

One page a person sees through a browser or device entry point, which serves a journey step only through a use case it offers.

Layer: Behavior.

Source: LiteSpec.

### Setting

One named bound or default the product runs within, with its setter and its behavior at the bound. The use cases that read it name it in `reads`.

Layer: Behavior.

Source: LiteSpec.

### Specification

The whole set of records describing one product.

Source: LiteSpec.

### State

One named stage in an aggregate lifecycle, with its meaning.

Layer: Behavior.

Source: LiteSpec.

### Store

One aggregate kind holding state no invariant guards.

Layer: Behavior.

Source: LiteSpec.

### Team Actor

One actor that belongs to the team building and running the product, marked on its record. A problem never names a team actor as its primary sufferer.

Layer: Intent.

Source: LiteSpec.

### Tenant Scope

The declared reach of a use case grant across tenants: own, cross, public, or none.

Layer: Behavior.

Source: LiteSpec.

### Term

One word with one meaning, labels in each language, and rejected synonyms. Each term carries its layer and its established aliases.

Layer: Intent or Behavior.

Source: arc42; DDD's ubiquitous language.

### Transition

One declared move between aggregate states, made by a use case.

Layer: Behavior.

Source: LiteSpec.

### Trigger

The event that starts a reaction, or the schedule or manual run that starts a job.

Layer: Behavior.

Source: LiteSpec.

### Ubiquitous Language

The shared domain words the team and the model use alike.

Source: DDD.

### Use Case

One command or query with its access, results, and scenarios.

Layer: Behavior.

Source: Cockburn.

## Standard Terms

These terms name the parts of the LiteSpec standard itself.

### Agent

Software that reads task context, plans bounded work, changes the files it is allowed to change, and runs verification.

Source: LiteSpec.

### Agent-Driven Engineering

The operating model where agents do engineering work and people keep decision authority.

Source: LiteSpec.

### Alert Inventory

The list of alerts a product can fire, with the runbook each alert names.

Source: the SRE book.

### Audit Record

One entry stating actor, action, target, outcome, time, origin, and reason.

Source: NIST SP 800-53, control AU-3.

### Audit Trail

The append-only store that holds audit records, kept separate from logs and domain events.

Source: NIST SP 800-53.

### Background Caller

The reaction or job whose `runs` edge names a background-only use case.

Source: LiteSpec.

### Background-Only

The computed state of a use case a reaction or job runs, needing no journey to walk it.

Source: LiteSpec.

### Check Level

One of the three check levels: G reads records only and always blocks, S reads code through a stack reader, A reports and never blocks.

Source: LiteSpec.

### Checkable

One provision a tool checks, carrying a Checked by verification line.

Source: LiteSpec.

### Closed Specification

The specification with every record decided and every required link holding.

Source: LiteSpec.

### Command Page

One page that documents one shipped command: its arguments, options, exit codes, and what it runs underneath.

Source: LiteSpec.

### Component Inventory

The list of shipped components published with each release artifact.

Source: supply-chain practice.

Rejected: SBOM, which names one format where the inventory names the content.

### Configuration Page

One page holding every setting a reader can change.

Source: LiteSpec.

### Conformance

The claim that a project meets the LiteSpec contract it pins.

Source: the W3C QA Framework.

### Consumer

One project built against a pinned LiteSpec release.

Source: LiteSpec.

### Contract

One named set of obligations or shapes holding between parts, such as a page, a provision, a pattern, a release, or an API.

Source: LiteSpec.

### Controlled Technical Prose

The fixed writing profile that sets the allowed structures, prose bounds, exact terms, and testable provisions.

Source: LiteSpec.

### Convention

One replaceable default for a name, location, file shape, or pattern.

Source: LiteSpec.

### CORS

The browser rule that decides which origins can call the API.

Source: the Fetch standard.

### Current Standards Material

The pinned release taken on its own, without transition or history material.

Source: LiteSpec.

### Derived Artifact

One committed file produced from an authored source, such as generated types.

Source: LiteSpec.

### Diagnostic

One issue litespec-maintain validate reports about standards material.

Source: LiteSpec.

### Document

One Markdown page under a record root that holds no record.

Source: LiteSpec.

### Documentation Layer

One depth of documentation, from a first working result down to the mechanism a command runs.

Source: Diataxis.

### Engineering System

One connected set of decisions, specifications, standards, verification, and release practices.

Source: LiteSpec.

### Exercised

The computed state of a journey step with a passing proof of a use case serving it.

Source: LiteSpec.

### Explanation

One page that builds understanding of what LiteSpec is and why it works the way it does.

Source: Diataxis.

### Explanation File

One Markdown file beside a record file that explains the record for people and agents.

Source: LiteSpec.

### Extension

One conditional standards bundle a project selects when its criterion holds.

Source: LiteSpec.

### Finding

One issue litespec model check reports, with its level, code, message, path, and line.

Source: LiteSpec.

### Flaky

The computed state of a proof whose test both passed and failed within the runs read.

Source: LiteSpec.

### Frontmatter

The opening block of a document stating its page style.

Source: LiteSpec.

### Governance

The group of records beside the four record layers, holding exceptions.

Source: LiteSpec.

### Guidance

One provision a person judges, carrying a Reviewed for verification line.

Source: LiteSpec.

### How-To Page

One page that solves one stated problem for a reader who already has a working result.

Source: Diataxis.

### Implemented

The computed state of a use case, rule, event, or aggregate state the stack's builder finds code for.

Source: LiteSpec.

### Index

One page routing readers to the pages of its section.

Source: LiteSpec.

### License

One SPDX identifier naming the terms a shipped package carries.

Source: SPDX.

Rejected: licence and licences, the British spellings.

### LiteSpec

The standards contract for specifying, building, verifying, releasing, and operating software with agents.

Source: LiteSpec.

### LiteSpec Release

One published, pinned, complete standards contract.

Source: LiteSpec.

### Mark

One written link from a code element or a test, of kind `implements` naming the record it serves or `follows` naming the provision, decision, or exception shaping it, holding the identifier in the project mark style.

Source: LiteSpec.

Rejected: anchor and tag, which name the link by one of its forms.

### Met

The computed state of a requirement everything fulfilling it verifies, with its measure's check passing.

Source: LiteSpec.

### Normative

Required of a conforming consumer through an identified provision.

Source: standards practice, as the W3C QA Framework and ISO Directives use it.

### Page

One authored record file or document under the documentation root.

Source: LiteSpec.

### Page Segment

The part of a provision ID naming its owning page.

Source: LiteSpec.

### Page Style

One page template for prose or record lists.

Source: LiteSpec.

Rejected: page class, which reads as a code type.

### Pattern

One named answer to a recurring interface question, with its options and wireframes.

Source: LiteSpec.

### Preset

One ready-made selection of shape, stacks, and frontends for a new project.

Source: LiteSpec.

### Project

One bounded-context business application built with LiteSpec.

Source: LiteSpec.

### Proof Hash

The hash of a scenario or of its test body recorded in `.litespec/proof/proofs.json` at the last passing run.

Source: LiteSpec.

### Provision

One rule, default, or piece of advice on a standards page, carrying an identifier, a strength, and a verification line.

Source: LiteSpec.

### Rate Limit

One bound on request frequency at an exposed endpoint.

Source: web API practice.

### Reachable

The computed state of an aggregate state a transition leads into.

Source: LiteSpec.

### Reads As

The phrase stating how one link reads from its record, with its pair stating how it reads back.

Layer: Behavior.

Source: LiteSpec.

### Record File

One YAML file holding one record page with its entries.

Source: LiteSpec.

### Record Layer

One of the four homes of records: Intent, Behavior, Implementation, or Proof.

Source: LiteSpec.

### Record-Driven Delivery

Delivery where records select work and define completion.

Source: LiteSpec.

### Recovery Objectives

The accepted time and data loss bounds for a restore.

Source: the SRE book.

### Reference Page

One page of exact values a reader looks up.

Source: Diataxis.

### Roll-Forward

Recovery by deploying a newer version rather than restoring the old one.

Source: release practice.

### Scope

The part of a project a provision applies to, stated in Applies to when narrower than the page.

Source: LiteSpec.

### Secret Inventory

The list of production secrets, each with its location and rotation date.

Source: security practice.

### Shape

One deployment boundary of a project: server or client-only.

Source: LiteSpec.

### Size

One of the required-record sets of a project: small or large.

Source: LiteSpec.

### Source Material

Research kept outside the documentation, which carries no authority on its own.

Source: LiteSpec.

### Stack

One technology binding of the specification, held in one folder.

Source: LiteSpec.

### Stale

The computed state of a proof whose scenario or test body changed after its test last passed.

Source: LiteSpec.

### Standard

One identified provision stating a required boundary for consumers.

Source: LiteSpec.

### Statement

The one sentence in a provision carrying its obligation.

Source: LiteSpec.

### Strength

One of the three provision strengths: rule, default, or advice.

Source: LiteSpec.

### Sub-Processor

One provider processing personal data for the product.

Source: the GDPR.

### Task Authorizer

The person who gave the agent its task.

Source: LiteSpec.

### Tested

The computed state of a record with a passing test that asserts something linking to it.

Source: LiteSpec.

### Tutorial

One page that takes a reader to a first working result.

Source: Diataxis.

### Underneath

The command page section stating what the command runs.

Source: LiteSpec.

### Verified

The computed state of a use case whose every scenario has a passing proof through the public boundary that is not stale.

Source: LiteSpec.

### Workspace

The repository that holds a consumer application or the standards.

Source: LiteSpec.

Rejected: repo, which shortens the word every path already shortens.
