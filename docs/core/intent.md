# Intent Layer

**Stack:** any

**Layer:** records

## Intent

Intent is why the product exists and what it promises: the product, its modules, actors, goals, problems, features with their requirements, journeys, constraints, research, decisions, and terms. This page states where Intent lives, what shape each record takes, and what Intent never holds, before Behavior states how the system keeps the promises. The [record language page](language.md) states how each field, value and record name is written.

## Concepts

### The Questions Intent Answers

| Question | Record | Write It When | Skip It When | Familiar As |
|:---|:---|:---|:---|:---|
| What is the product and for whom? | product | Always, once | Never | Product brief |
| Which product area holds this work? | module | Always, one per product area | Never | Product area |
| Who touches the product? | actor | A person or organization appears in a problem, a journey or an access position | The "actor" is a role or a system; roles are Behavior and systems stand alone | Stakeholder |
| What software outcome do we want? | goal | You can name what you count | The system controls it directly: that is a quality requirement | Product goal |
| What pain do people outside the team have today? | problem | You can name who suffers, what it costs them and what they do instead, with evidence | Only one solution is imaginable: it is a feature | Pain point |
| What do we offer against a problem? | feature | Scope needs tracking | The commitment is permanent and no offer carries it | Capability |
| What exactly must hold? | requirement | Always | Never | User story |
| Can a person get from start to finish? | journey | A task crosses several actions | It is one action | User journey |
| Which outside rule binds us? | constraint | An outside party can hold us to it | It is our own choice: a decision | Mandated constraint |
| Which outside fact did we rely on, and when was it true? | research | A choice rests on it and it could change | It binds us: a constraint | Spike |
| Which option did we choose, and what reopens it? | decision | There were real options | There was nothing to choose | Decision record |
| Which word means what? | term | A word could mean two things | Never skipped for a domain word | Glossary term |

### Product Intent and Its Parts

Product intent is what the product commits to: what it does, for whom, and where it stops. Running the company is no part of it. [Keep Company Operation Out of Intent](#keep-company-operation-out-of-intent) states the boundary, and [The Software Test](#the-software-test) names what belongs for each record type.

Intent reads module, then feature, then requirement. A module names one product area of the bounded context; each feature sits under one module and carries the offer against a problem; each requirement sits as an entry on its feature and states one commitment Behavior fulfills. The [model page](model.md) defines the record types and links. A feature page keeps the offer with its commitments, while the book lists the requirement entries and the decisions behind them.

Intent is not Behavior. A rule or use case states what the system does today and matches the code. A requirement states what the product decided and why, and it can describe a position the code does not carry yet. The two connect in one direction: Behavior records name the requirements they fulfill and the steps they serve, and an Intent record never describes Behavior, except a decision naming the records it settles in `decides` and an actor naming its roles in `holds`.

### The Software Test

Every record states something the software does, holds, refuses at runtime or runs on, and translates into code, results, tests or infrastructure. The structure stays; the content test changes:

| Type | Belongs when it is | Out when it is |
|:---|:---|:---|
| Goal | A software outcome: "Nights run without oversell or unreconciled money" | A business outcome: retention, revenue, market share |
| Problem | A user pain the software answers | A sales or market problem |
| Feature | A capability the software has | A marketing tactic, a launch plan, or a refusal |
| Requirement | A commitment Behavior fulfills | (always software when it reaches Behavior) |
| Constraint | Law, a provider or a limit that binds the software | A company duty with no software effect, such as incorporation |
| Research | Evidence a software record cites | Market sizing, viability, acquisition, pricing strategy |
| Module | A product area of the bounded context | A company function |

A record that states company operation fails the test, even when its folder is right. Reachability proves the rest: a record that reaches no Behavior is either unfinished work for the backlog or not software.

### Modules as Product Areas

Each Intent area names a module declared in Behavior, so one name runs through every layer:

```text
.litespec/intent/modules/<module>/features/    features with their requirements
.litespec/behavior/modules/<module>/...        aggregates and use cases
src/<Layer>/<Module>/<Aggregate>/<UseCase>     code
```

The reader finds a use case by module, then aggregate, then use case, and the requirements behind it under the same module name in Intent. The module is also the seam where a monolith would later split into services. Anything cross-cutting, such as accessibility or security, sits at product level. A folder under `intent/modules/` that names no declared module gets a blocking finding.

The folder is the parent, at product level or under a module:

```text
.litespec/intent/
  product.yaml
  goals/  problems/  constraints/  decisions/  research/  actors/  journeys/
  modules/
    finance/
      module.yaml
      goals/  problems/  constraints/  decisions/  research/
      features/
        payout.yaml            the feature with its requirements as entries
.litespec/behavior/
  modules/
    finance/
      payout/                  aggregate
        request-payout.yaml    use case
```

Product level holds only the product, actors, roles and journeys; journeys cross modules through their steps. Module level holds only features with their requirements. Goals, problems, constraints, decisions and research sit at either level, and the folder says which. Every folder is one of a closed set per layer; a record of a type in the wrong folder, a module folder without its module, and a record type outside its allowed levels are blocking findings.

A record under a module is linked only from records in that module. A link from another module is a blocking finding, because the record is then not local and belongs at product level. A product-level record may be linked from one module or many.

### Two Identifiers

The identifier stays as it is: `problem/organizers-wait-for-ticket-money`, unique in the product, and what every link and citation uses. Moving a record between product and module level does not change it, so no citation changes.

The qualified identifier is computed from the identifier segments plus placement and never written, like a fully qualified type name: `finance/problem/organizers-wait-for-ticket-money`, or `problem/...` with no prefix at product level, and `finance/payout/use-case/request-payout` in Behavior. The book and the CLI show both, and `litespec model query` accepts either.

### How Intent Reaches Behavior

The [model page](model.md) states the authority order that settles conflicts between records. Goals, problems, features, journeys, actors, research, and prose carry no authority, and decisions win between peers.

A decision wins its choice, and a feature states it as requirements. Behavior records name the requirements they fulfill and the journey steps they serve, and the code carries what happens today. When Behavior contradicts a requirement, either the code carries a defect or the product changed. A changed product means a new decision before any page or code moves.

A fact learned from the world enters Intent as research or a constraint, cited and dated. Where Intent is silent, working code is evidence for a missing record, not a defect to delete.

### Requirements with Except and Fallback

A requirement carries `kind` (functional, quality, or policy), `statement`, `rationale`, and `measure` where its statement names a number. `setBy` names who sets its value: `product`, `operator`, or `tenant`.

An `except` entry states in product words when the commitment does not hold, and `fallback` names the requirement that covers what happens then. The fallback carries no `except` of its own: exceptions stay one deep, so every exception ends in a promise.

### Journeys as Product Sentences

A journey carries `actor`, `serves`, `summary`, `finishedWhen`, ordered `steps`, `betweenSteps`, and `cites`. `serves` names the goal record. `summary` states in up to four sentences who walks, what starts the walk, what good looks like, and why it matters, and the book renders it first on the page. `finishedWhen` states the observable end in one sentence. Each step carries its `id` (`s1`, `s2`, and so on) and its product sentence in `statement`, names the walking actor only at a handoff in `actor`, or names the system record performing it in `system` when no person acts, and carries its failure text in `failure` where the walk can fail. A step carries exactly one of `actor` and `system`, or neither when the walking actor performs it. `betweenSteps` states the branch order between the steps. A step id reads `journey/<name>#s1`.

Steps name no use case. Use cases, reactions, and jobs point up with `serves`, naming the steps they serve. The book shows each journey as record detail: `finishedWhen`, the steps as sentences with `failure`, the requirements it cites with their statements and their `except` and fallback inline, the serving use case goals, and the Then clauses of their scenarios as context. Field names never render literally in journey detail.

A journey walks one actor toward one goal. A step names another actor only at a handoff, and a walk with several goals splits into several journeys.

### Staff Journeys

Journeys are not only employee walks. A staff journey walks a staff actor, such as finance exporting the approved batch, through the same shape of serves, `finishedWhen`, steps, and `betweenSteps`. A scheduled promise is a requirement that a job fulfills, not a journey. Staff steps read as product sentences too.

### What Intent Never Contains

Intent never holds Behavior detail. `litespec/core/intent#keep-behavior-out-of-intent` names what stays out.

## Standards

### Keep Intent in Its Homes

**ID:** `litespec/core/intent#keep-intent-in-its-homes`

**Strength:** rule

**Applies to:** artifact: record:product, record:goal, record:problem, record:constraint, record:decision, record:research, record:actor, record:role, record:journey, record:module, record:feature

**Statement:** A consumer MUST keep its product record at `.litespec/intent/product.yaml`, its product-level goals, problems, constraints, decisions, research, actors and journeys in their folders under `.litespec/intent/`, and each module with its goals, problems, constraints, decisions, research and features under `.litespec/intent/modules/<module>/`.

**Rationale:** Intent split across notes and books gives no single answer to the question an agent asks before building. One home per kind, with the product record, goals, problems, constraints, decisions, research, actors and journeys at product level and each module with its goals, problems, constraints, decisions, research and features under its module folder, gives every reader one place to look. A second home for any kind starts a second truth about the product. The folder is the parent: product level holds only the product, actors, roles and journeys, module level holds only features with their requirements, and goals, problems, constraints, decisions and research sit at either level with the folder saying which. Each module folder names a module declared in Behavior, so one name runs through Intent, Behavior and code.

**Checked by:** `litespec model check` reports a blocking finding for a record outside its home, a folder under `intent/modules/` naming no declared module, a module folder without its module, a record type outside its allowed levels, and a link from another module to a module-local record.

### State One Product

**ID:** `litespec/core/intent#state-one-product`

**Strength:** rule

**Applies to:** artifact: record:product

**Statement:** A consumer MUST state exactly one product record at `.litespec/intent/product.yaml`, with `boundary`, `addresses`, and `summary`.

**Rationale:** One product record gives every reader one answer to what the product is and who it serves. The `boundary` and `addresses` focus every later choice. Two product statements let each plan cite the one it prefers. `summary` states what the product is in one place.

**Checked by:** `litespec model check` reports no `product-count-invalid` finding, and `litespec model check` reports `record-invalid` for product fields outside the record schema.

### Keep Company Operation Out of Intent

**ID:** `litespec/core/intent#keep-company-operation-out-of-intent`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** An Intent or Behavior record MUST state only behavior the application performs, data it holds, a refusal it raises at runtime, infrastructure it runs on, or a limit it enforces.

**Rationale:** LiteSpec specifies the product, not the company. Prices as an offer, markets, hiring, sales actions, launch calendars, company goals and founder concerns can be checked only against a bank account, a diary or a contract, so they sit outside the records a reader reviews as the product. A company fact that binds the product still enters: as a constraint when an authority or a limit binds the application, and as dated research when a choice rests on it. [The Software Test](#the-software-test) names what belongs for each record type: a goal states a software outcome, a problem states a user pain the software answers, a feature states a capability the software has, and a module names a product area, never a company function. Plan entitlements the software enforces stay, as settings and permissions; the price list as a company offer leaves.

**Example:** A library holds its lending limit as a setting the application enforces, so the limit stays as Behavior. The membership price the library charges as an offer is a company fact: it enters only as dated research behind the decision that sets the limit.

**Reviewed for:** Intent and Behavior records stating company operation beside the constraints and research that carry the binding facts.

### Reach Every Intent Record with Behavior

**ID:** `litespec/core/intent#reach-every-intent-record-with-behavior`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** Each Intent record MUST reach Behavior through its stated path.

**Rationale:** The folder fixes where a record sits. Reachability fixes that it leads to software. A goal reaches a journey or a quality requirement. A problem reaches a feature or a requirement that addresses it. A feature or a requirement reaches a use case, a journey or a quality scenario in Behavior. A constraint is answered by a requirement. Research is cited by a record that reaches Behavior. A decision decides a record that reaches Behavior. A record that reaches no Behavior is either unfinished work for the backlog or not software.

**Checked by:** `litespec model check` reports a blocking finding for a record that reaches no Behavior.

### State Each Goal with Its Measure

**ID:** `litespec/core/intent#state-each-goal-with-its-measure`

**Strength:** rule

**Applies to:** artifact: record:goal

**Statement:** A goal MUST state its `summary`, `statement`, `measure`, and `target` in fields.

**Rationale:** A goal without a measure is a wish no check can confirm: the team cannot tell whether the product serves it. The `summary` states what the goal is in one paragraph, the `statement` names the outcome and why it matters, and `measure` and `target` name what is counted and the number aimed for. A goal carries no authority: it explains why features exist and never decides a conflict.

**Checked by:** `litespec model check` reports `record-invalid` for a goal record file missing a required field.

### Move Every Goal with a Journey or a Quality Requirement

**ID:** `litespec/core/intent#move-every-goal-with-a-journey-or-a-quality-requirement`

**Strength:** rule

**Applies to:** artifact: record:goal

**Statement:** A goal MUST be moved by a journey serving it or by a quality requirement citing it.

**Rationale:** A goal no journey serves and no quality requirement confirms names an outcome the product never touches: a company hope filed as a product goal. A journey serving the goal shows the walk that moves the counted outcome, and a quality requirement citing it states the measure that confirms the move. An outcome the system controls directly is a quality requirement, and an outcome only the company controls is no goal at all.

**Example:** The library goal of faster pickup is moved by the borrow-and-return journey serving it; a goal of higher membership sales is moved by no journey and is no product goal.

**Checked by:** `litespec model check` reports `goal-mover-missing` for a goal moved by no journey and no quality requirement.

### Admit Only Outside Pains as Problems

**ID:** `litespec/core/intent#admit-only-outside-pains-as-problems`

**Strength:** rule

**Applies to:** artifact: record:problem

**Statement:** A problem MUST name in `affects` a primary sufferer that is a person or organization actor outside the team building the product, and state the pain in `summary` and `statement` in fields.

**Rationale:** A problem is a pain someone outside the team has today. The first `affects` entry is the primary sufferer, so who has the pain is the first fact a reader meets. `statement` states the sufferer, the condition and the consequence in one sentence, with the product never as its subject. `summary` states what the pain is in one paragraph: when it happens and what the person was trying to do, and never stands as a bare pointer to another page. What is no problem leaves for its own record: a solution in disguise becomes a feature, a worry about what might happen becomes a decision or a goal, an internal limit becomes a limit on the plan written outside Intent, a market fact becomes research, and a legal duty becomes a constraint.

**Example:** In the library product, readers losing track of due dates is a problem whose primary sufferer is the reader. The library having too few staff for the summer is an internal limit and no problem.

**Checked by:** `litespec model check` reports `problem-sufferer-invalid` for a problem whose primary sufferer is no person or organization outside the team; `litespec model check` reports `record-invalid` for a problem record file missing `affects`, `summary` or `statement`.

### State the Cost and Workaround of Every Problem

**ID:** `litespec/core/intent#state-the-cost-and-workaround-of-every-problem`

**Strength:** rule

**Applies to:** artifact: record:problem

**Statement:** A problem MUST state what the pain costs its sufferer today and what the sufferer does instead, each in the sufferer's own units or the word `unknown`, in its explanation file.

**Rationale:** A pain with no cost and no workaround is a slogan: no reader can tell a daily loss from a passing annoyance, and no feature can be judged against the price of standing still. The workaround shows the pain is real enough to work around, and why that workaround fails. Where nobody has priced the cost or named the workaround, `unknown` states the gap honestly. Who Has It states the cost and What They Do Today states the workaround, so the fields stay checkable and the context stays readable.

**Example:** A reader who loses track of due dates pays late fees each month, and keeps a paper list of due dates, which fails when the list stays at home.

**Reviewed for:** The cost and the workaround, each in the sufferer's own units or the word `unknown`, in Who Has It and What They Do Today of each problem explanation file.

### Grade Problem Evidence by Kind

**ID:** `litespec/core/intent#grade-problem-evidence-by-kind`

**Strength:** rule

**Applies to:** artifact: record:problem

**Statement:** A problem MUST list each evidence entry with its `claim`, its `source` as a record id or a URL, its `readOn` date, and its `kind` as observed, reported or assumed, and carry at least one observed or reported entry.

**Rationale:** Evidence states how anyone knows the pain is real: observed means a sufferer was watched, reported means a sufferer said it, and assumed means nobody outside the team has confirmed it. The strength of a problem is the strongest kind present, computed by the builder and never written, so it cannot drift from the entries. A problem resting on assumption alone, or on no entry, is unfinished work for the backlog until a sufferer is watched or heard.

**Example:** The library problem of readers losing due dates carries a reported entry from the desk log, read on its date, so its computed strength is reported.

**Checked by:** `litespec model check` reports `problem-evidence-missing` for a problem with only assumed evidence or none. The builder writes `evidenceStrength` on each problem in the model.

### Name No Solution in a Problem Statement

**ID:** `litespec/core/intent#name-no-solution-in-a-problem-statement`

**Strength:** rule

**Applies to:** artifact: record:problem

**Statement:** A problem `statement` MUST name no product, feature or solution.

**Rationale:** A statement naming a solution files a missing feature as a pain: the reader accepts the answer before weighing the pain, and every alternative answer reads as off-topic. A statement naming the sufferer, the condition and the consequence leaves the answer to the feature that solves it.

**Example:** "Readers lose track of due dates" names a pain. "Readers need a reminder dashboard" names a solution and is reported.

**Reviewed for:** Which problem statements name a product, a feature, or a solution? Look at the problem statements beside their sufferers.

### Name Each Sibling Problem or Merge It

**ID:** `litespec/core/intent#name-each-sibling-problem-or-merge-it`

**Strength:** rule

**Applies to:** artifact: record:problem

**Statement:** Two problems with the same primary sufferer cited from the same feature MUST name each other or merge into one problem.

**Rationale:** Two pains of one sufferer under one feature, written in different words, hide that they are one pain or two: one feature solves both halves, and the book lists the sufferer twice with no sign of the overlap. Naming the sibling states the split where a reader meets it; merging keeps one pain with one sufferer.

**Example:** Two library problems of the reader cited from the borrowing feature, one on due dates and one on renewals, name each other, so a reader sees both before the feature solves either.

**Reviewed for:** Which problems share a sufferer and a feature yet name neither each other nor a merge? Look at the problems cited from each feature.

### Explain Every Problem in Four Sections

**ID:** `litespec/core/intent#explain-every-problem-in-four-sections`

**Strength:** rule

**Applies to:** artifact: record:problem

**Statement:** A problem explanation file MUST carry Who Has It, What They Do Today, Evidence, and What Would Retire It, in that order.

**Rationale:** The fields state who suffers; the explanation carries the cost, the one concrete occasion, the workaround in use, the evidence behind each claim, and what change in the world would retire the pain. A fixed order means a reader who learned one problem page finds each part on every problem page.

**Example:** The library problem on due dates opens Who Has It with one reader at the desk with three overdue copies, and closes with What Would Retire It: readers holding their due dates without a paper list.

**Reviewed for:** The four sections, in order, in each problem explanation file.

### State Each Problem with Its Evidence

**ID:** `litespec/core/intent#state-each-problem-with-its-evidence`

**Strength:** rule

**Applies to:** artifact: record:problem

**Statement:** A problem MUST state its `summary` and `statement` in fields, with its evidence graded under [Grade Problem Evidence by Kind](#grade-problem-evidence-by-kind).

**Rationale:** A problem that names what its people face points to the pain its solution lifts. `summary` describes the pain in the people's own terms, and the graded evidence shows it is real. Problems without sufferers attract solutions nobody needs. The `affects` link names the actors who face it, so each pain traces to the people a solution must serve.

**Checked by:** `litespec model check` reports `record-invalid` for a problem record file missing its `summary` or `statement`.

### State Each Feature with Its Offering

**ID:** `litespec/core/intent#state-each-feature-with-its-offering`

**Strength:** rule

**Applies to:** artifact: record:feature

**Statement:** A feature MUST state its `offering`, name the problems it solves in `solves`, and name the goals it moves in `advances`.

**Rationale:** A feature names the problems it solves in `solves` and the goals it moves in `advances`, so each offer traces to a pain and an outcome. `offering` states what is in and what is out, so the scope of the offer is written where the links are. Proposed by [decision/0004-features-advance-goals](decision/0004-features-advance-goals) on the evidence in [research/intent-hierarchy-evidence](research/intent-hierarchy-evidence), [research/cross-layer-trace-evidence](research/cross-layer-trace-evidence), and [research/established-traceability-practice](research/established-traceability-practice).

**Reviewed for:** Which features miss their offering, their solved problems, or their moved goals? Look at each feature record beside its problems and goals.

### Keep Actors to People and Organizations

**ID:** `litespec/core/intent#keep-actors-to-people-and-organizations`

**Strength:** rule

**Applies to:** artifact: record:actor

**Statement:** An actor MUST carry a `kind` of person or organization and state in `belongsToTeam` whether it belongs to the team building and running the product.

**Rationale:** An actor answers for what it does, so software stands behind no actor: an external system stands alone as its own record, and internal machinery such as a reaction, a job, a transport or a renderer is never an actor. A person or an organization can hold a role, suffer a problem and walk a journey. `belongsToTeam` reads as a predicate that is true about the record, so a reader tells a team actor from a customer at a glance, and a problem can refuse a team sufferer.

**Example:** The library reader is an actor of kind person with `belongsToTeam` false. The text gateway is an external system and no actor.

**Checked by:** `litespec model check` reports `record-invalid` for an actor record file with a kind outside person and organization, or with no `belongsToTeam`; `litespec model check` reports `field-invalid` for such a kind.

### Name Every Actor from a Journey, a Problem or an Access Position

**ID:** `litespec/core/intent#name-every-actor-from-a-journey-a-problem-or-an-access-position`

**Strength:** rule

**Applies to:** artifact: record:actor

**Statement:** Every actor MUST be named by a journey, by a problem in `affects`, or by an access position in `holds`.

**Rationale:** An actor named by nothing is a party nobody suffers as, walks as or grants to: a leftover from a system or a machine promoted beside real people. Naming each actor from the walk it walks, the pain it suffers or the role it holds keeps the actor list equal to the parties the product serves.

**Example:** The library reader is named by the borrow-and-return journey and by the due-date problem; the librarian is named by the holds of the librarian role.

**Checked by:** `litespec model check` reports `actor-unused` for an actor named by no journey, problem or access position.

### Define Each Recurring Capitalized Phrase

**ID:** `litespec/core/intent#define-each-recurring-capitalized-phrase`

**Strength:** rule

**Applies to:** artifact: record:term

**Statement:** A capitalized phrase of two to four words named in three or more records MUST be defined by a glossary term.

**Rationale:** A phrase used across records reads as a defined term whether or not any definition exists: readers and agents build on a meaning nobody wrote. Flagging the phrase while it is still cheap to define keeps the glossary equal to the words the records actually use.

**Example:** A library phrase naming its first lending partners, used across goals and problems, takes a glossary term stating who those partners are, or the records stop using the phrase.

**Checked by:** `litespec model check` reports `term-missing` for a capitalized phrase of two to four words named in three or more records that no glossary term defines.

### Answer Every Constraint

**ID:** `litespec/core/intent#answer-every-constraint`

**Strength:** rule

**Applies to:** artifact: record:constraint

**Statement:** A constraint MUST state its `authority`, `readOn`, `kind`, `applies`, `appliesBecause`, and `summary` in fields, with What It Requires generated from the requirements that cite it.

**Rationale:** An unanswered constraint is an outside rule the product can break unknowingly: a law, a regulation, or provider terms the team never examined. Recording each rule with its authority and applicability proves the team asked. The `kind` is technical, organizational, convention, or legal. `summary` states the rule in the authority's own words, `readOn` states when the outside rule was read, and What It Requires lists the requirements that cite the constraint, generated at build time. A fact learned from the world, such as the payout bank lengthening its settlement window, first lands as research with its source and date, then as a constraint.

**Checked by:** `litespec model check` reports `record-invalid` for a constraint record file missing a required field; the builder generates What It Requires from citing requirements.

### Record Research with Read Sources

**ID:** `litespec/core/intent#record-research-with-read-sources`

**Strength:** rule

**Applies to:** artifact: record:research

**Statement:** A research record MUST state its `question`, state its `conclusion` in one sentence, list each source with its `key`, `url`, and `readOn`, and carry Answer in its explanation file.

**Rationale:** A fact without its source cannot be rechecked when a decision depends on it. The read date shows when the outside fact was true, so a later reader knows whether to trust or refresh it. Research without sources and dates asks every later choice to rest on memory. The `conclusion` states in one sentence what the sources establish and what follows, so a decision can cite it as data. Each source carries its `key`, `url`, and `readOn`; the explanation cites a source with a reference link `[text][key]`, and a bare URL, an unknown key, or an unused source is a finding. Answer expands the `conclusion`: what the sources say, claim by claim, with the confidence behind each claim, and Notes on Sources holds what a later reader needs about each source. Observed provider behavior lands here too, cited and dated, before any constraint or requirement rests on it. Proposed by [decision/0009-research-states-its-conclusion](decision/0009-research-states-its-conclusion) on the evidence in [research/research-conclusion-evidence](research/research-conclusion-evidence).

**Checked by:** `litespec model check` reports `record-invalid` for a research record file without `question`, `conclusion`, or `sources`; `litespec model check` reports `explanation-missing` for a research record without Answer in its explanation file, `source-link-bare` for a bare URL, `source-key-unknown` for an unknown key, and `source-unused` for an unused source.

### Shape Each Decision Page

**ID:** `litespec/core/intent#shape-each-decision-page`

**Strength:** rule

**Applies to:** artifact: record:decision

**Statement:** A decision MUST state its `choice` and `consequences` in fields and carry Context in its explanation file.

**Rationale:** Context in the explanation file states the choice, `choice` names the chosen option, and `consequences` states what follows, so each decision reads the same way. Decision in the explanation file expands the chosen option, and Evidence with Confidence holds the supporting material with its confidence where the choice rests on it. The `reopenedWhen` condition names what would reopen the choice, so a decision reopened by a condition rather than a date stays stable until the world actually changes. The `basedOn` link names the constraints and research behind it, and requirements only on a technical decision. The [model page](model.md) requires each product decision to name the records it settles in `decides`.

**Checked by:** `litespec model check` reports `record-invalid` for a decision record file missing a required field; `litespec model check` reports `explanation-missing` for a decision without Context in its explanation file.

### Record Each Decision Option with Pros and Cons

**ID:** `litespec/core/intent#record-each-decision-option-with-pros-and-cons`

**Strength:** rule

**Applies to:** artifact: record:decision

**Statement:** Each decision option MUST carry its `pros` and its `cons`.

**Rationale:** A choice argued from options and evidence can be revisited on its merits; one argued from authority cannot. Each option carries its name with what speaks for it in `pros` and against it in `cons`, so the comparison sits in the entries instead of a prose section.

**Checked by:** `litespec model check` reports `record-invalid` for a decision option missing `pros` or `cons`.

### Shape Each Module Page

**ID:** `litespec/core/intent#shape-each-module-page`

**Strength:** rule

**Applies to:** artifact: record:module

**Statement:** A module MUST state its `summary` and its `dependsOn` in fields.

**Rationale:** The module names one product area of the bounded context, so Intent, Behavior and code share one name for one area. `summary` states what the area covers in one place, and `dependsOn` names the modules it builds on. A reader learns where each area sits once and finds its records under one folder. Cross-cutting work sits at product level, never in a module that borrows it.

**Example:** The spending module covers expenses, receipts and the policy, with an empty `dependsOn` where it stands alone.

**Checked by:** `litespec model check` reports `record-invalid` for a module record file missing a required field.

### Identify Every Requirement

**ID:** `litespec/core/intent#identify-every-requirement`

**Strength:** rule

**Applies to:** artifact: record:requirement

**Statement:** Each requirement entry MUST carry one identifier `requirement/<feature>.<name>` naming its own feature.

**Rationale:** An identifier lets a Behavior record name the commitment it fulfills and lets the model resolve the name. A commitment written only as prose cannot be traced from Intent to code. The feature segment names the feature holding the entry, and the feature is the parent by identifier containment. Proposed by [decision/0003-requirement-origin](decision/0003-requirement-origin) on the evidence in [research/intent-hierarchy-evidence](research/intent-hierarchy-evidence), [research/cross-layer-trace-evidence](research/cross-layer-trace-evidence), and [research/established-traceability-practice](research/established-traceability-practice).

**Checked by:** `litespec model check` reports `record-invalid` for a requirement entry without identifier; `litespec model check` reports `identifier-invalid` for a feature segment differing from its feature.

### State Requirement Kind and Statement

**ID:** `litespec/core/intent#state-requirement-kind-and-statement`

**Strength:** rule

**Applies to:** artifact: record:requirement

**Statement:** Each requirement entry MUST carry `kind` and `statement`.

**Rationale:** The `kind` is functional, quality, or policy: what the product does, how well it does it, or what the business holds. A policy requirement carries its `values` with effective dates. The `statement` states the commitment in one testable sentence.

**Checked by:** `litespec model check` reports `record-invalid` for a requirement entry without `kind` or `statement`, or a policy requirement without dated values.

### Cite the Reasons of Every Requirement

**ID:** `litespec/core/intent#cite-the-reasons-of-every-requirement`

**Strength:** rule

**Applies to:** artifact: record:requirement

**Statement:** Each requirement entry MUST name its reasons in `cites`.

**Rationale:** The `cites` list holds any of goal, problem, feature, constraint, research, and decision, in no order of precedence. The feature holding the entry is the parent; the cited reasons are connections that explain why the commitment exists. A requirement that cites nothing reports as `requirement-cites-missing`.

**Checked by:** `litespec model check` reports `requirement-cites-missing` for a requirement entry naming no reason in `cites`.

### Cite Each Applying Constraint from a Requirement

**ID:** `litespec/core/intent#cite-each-applying-constraint-from-a-requirement`

**Strength:** rule

**Applies to:** artifact: record:requirement

**Statement:** A requirement MUST cite in `cites` each constraint with `applies: true` that binds it.

**Rationale:** An applying constraint with no citing requirement has no answer the product keeps. Citing it ties the outside rule to the commitments that obey it, and What It Requires lists those commitments back. A constraint with `applies: true` that no requirement cites reports as `constraint-unreached` at check level G.

**Checked by:** `litespec model check` reports `constraint-unreached` for a constraint with `applies: true` that no requirement cites.

### Explain Each Requirement in One Line

**ID:** `litespec/core/intent#explain-each-requirement-in-one-line`

**Strength:** rule

**Applies to:** artifact: record:requirement

**Statement:** Each requirement entry MUST explain its reason in a one-line `rationale`.

**Rationale:** A commitment without its reason cannot be rejudged when the world moves: the reader finds what holds but not why. One line forces the reason into the open without inviting an essay. The feature carries the full story; the `rationale` carries the reason beside the commitment.

**Reviewed for:** The one-line `rationale` on each requirement entry.

### State the Measure on a Counted Requirement

**ID:** `litespec/core/intent#state-the-measure-on-a-counted-requirement`

**Strength:** rule

**Applies to:** artifact: record:requirement

**Statement:** A requirement whose statement names a number MUST carry `measure`.

**Rationale:** A number without its measure cannot be checked: the team cannot tell what is counted. The `measure` names what is counted. A requirement carrying `measure` needs a `check` and a passing run, and a statement naming a number with no `measure` reports at check level A.

**Checked by:** `litespec model check` reports `requirement-measure-missing` for a requirement carrying `measure` without `check` or a passing run, and for a statement naming a number with no `measure`.

### State Each Quality Requirement as a Scenario

**ID:** `litespec/core/intent#state-each-quality-requirement-as-a-scenario`

**Strength:** rule

**Applies to:** artifact: record:requirement

**Statement:** A quality requirement MUST state the moment it holds for in `stimulus` and `environment`, what the product does in `response`, how that is measured in `measure`, and how that is confirmed in `check`, with `target`, `conditions`, `runsOn`, and `evidence` where they apply.

**Rationale:** "Fast", "secure", or "always available" cannot be built to or checked. A quality requirement holds only for a moment that can happen: something arrives, under some condition, and the product answers within a measure. Written that way, it tells the builder what to design for and tells a test or a probe what to look for, and a later change in the moment (a bigger on-sale, a new attack) visibly reopens the target. The four fields follow the quality attribute scenario the architecture literature uses, so a reviewer can compare one target with another. The `check` names the probe or test that confirms the measure, and it is required where the requirement carries `measure`. The `target` names the number aimed for, `conditions` names what holds while measuring, `runsOn` names where the check runs, and `evidence` names what the run leaves behind; each applies where the confirmation needs it.

**Example:** `requirement/expense-submission.month-end-submissions` has `kind: quality`, `stimulus` "Five hundred employees submit expenses in the last hour of the month", `environment` "Normal operation, one application instance down", `response` "Each submission is stored and acknowledged", `measure` "Share of submissions acknowledged within two seconds: at least 99 percent", and `check` "month-end acknowledgement probe".

**Checked by:** `litespec model check` reports `record-invalid` for a quality requirement missing `stimulus`, `environment`, `response`, `measure`, or `check`.

### Restrict Requirement Fulfillment to Behavior

**ID:** `litespec/core/intent#restrict-requirement-fulfillment-to-behavior`

**Strength:** rule

**Applies to:** artifact: record:requirement

**Statement:** A requirement MUST be fulfilled only from Behavior records.

**Rationale:** The records that fulfill are rule, use case, reaction, job, notification, and setting. A journey names the requirements it shows in `cites`; it never fulfills, so a journey alone never clears an unfulfilled requirement. Behavior points up to the promises it keeps, and each promise traces down to the behavior that keeps it.

**Checked by:** `litespec model check` reports `requirement-fulfillment-missing` for a requirement no Behavior record fulfills.

### Order Each Journey Toward One Goal

**ID:** `litespec/core/intent#order-each-journey-toward-one-goal`

**Strength:** rule

**Applies to:** artifact: record:journey

**Statement:** A journey MUST walk one actor toward one goal through ordered steps, naming the goal record in `serves` and stating `betweenSteps` in fields.

**Rationale:** A journey names its `actor`, its goal record in `serves`, its `finishedWhen`, and its ordered `steps`. One actor means a step names another actor only at a handoff; one goal means a walk with several goals splits into several journeys. The order shows how the steps join toward the goal, so a step that fits nowhere is visible. The journey names in `cites` the requirements it carries, and the use cases, reactions, and jobs that serve each step point up with `serves`. `betweenSteps` holds branch order only, and Example in the explanation file shows one concrete occasion. Proposed by [decision/0005-journeys-serve-a-goal](decision/0005-journeys-serve-a-goal) on the evidence in [research/intent-hierarchy-evidence](research/intent-hierarchy-evidence), [research/cross-layer-trace-evidence](research/cross-layer-trace-evidence), and [research/established-traceability-practice](research/established-traceability-practice).

**Example:** The largest consumer held a 21-step journey with five actors, from creating the organizer to closing the books. Split by goal, it is four journeys: set up and sell an event; buy a ticket; admit the crowd; close the night.

**Checked by:** `litespec model check` reports `record-invalid` for a journey record file missing its `actor`, `serves`, `summary`, `finishedWhen`, `steps`, or `betweenSteps`.

### Describe Every Journey

**ID:** `litespec/core/intent#describe-every-journey`

**Strength:** rule

**Applies to:** artifact: record:journey

**Statement:** A journey MUST state in `summary`, in up to four sentences, who walks, what starts the walk, what good looks like, and why it matters.

**Rationale:** A journey page opens with its steps, so a reader landing on it cold learns the mechanics before the purpose. The summary states the walk in the walker's terms first, and the book renders it first on the page. `finishedWhen` stays the checkable end state; the summary carries the purpose no field checks.

**Example:** The library journey to borrow and return describes a reader who holds a copy, starting at the shelf and ending with the copy back on its shelf, because a reader plans the next visit around the return.

**Checked by:** `litespec model check` reports `record-invalid` for a journey record file missing `summary`.

### Perform Each Journey Step with an Actor or a System

**ID:** `litespec/core/intent#perform-each-journey-step-with-an-actor-or-a-system`

**Strength:** rule

**Applies to:** artifact: record:journey

**Statement:** A journey step MUST name at most one performer: another actor in `actor` at a handoff, or the external system, job or reaction in `system` when no person acts, and a step naming neither is performed by the walking actor.

**Rationale:** A step performed by machinery forced a machine actor into Intent, where it sat beside real people and held no role. Naming the system record that performs the step keeps the actor list to people and organizations, and keeps the performing party one link away. A step naming both an actor and a system names two performers for one act.

**Example:** In the library journey, the step that sends the overdue notice names the text gateway in `system`; the steps the reader walks name no performer.

**Checked by:** `litespec model check` reports `record-invalid` for a journey step carrying both `actor` and `system`; `litespec model check` reports `field-invalid` for such a step or for a `system` naming no external system, job or reaction.

### Keep Behavior Out of Intent

**ID:** `litespec/core/intent#keep-behavior-out-of-intent`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** An Intent record MUST NOT describe states, results, codes, transactions, edges, consistency modes, Behavior field names, or code.

**Rationale:** An Intent record that names Behavior detail becomes a second specification, and it drifts from the first with the next Behavior change. Two specifications force every reader to ask which one the code follows. Behavior records own behavior and point up to the requirements and steps they serve, so each fact has one home.

**Reviewed for:** Intent records under `.litespec/intent/` free of the detail the Statement names.

### Treat Generated Research as Source Material

**ID:** `litespec/core/intent#treat-generated-research-as-source-material`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** An agent MUST treat records generated outside `docs/` as source material until a research record or a decision states the claim.

**Rationale:** Records generated outside `docs/` mix verified facts, inferences, and recommendations in one confident voice. Treating them as source material with no authority keeps a recommendation from being built as if someone decided it. A research record or a decision restates the claim before anything rests on it.

**Reviewed for:** A research record or a decision behind each position built from generated records.

### State Each Exception with Its Fallback

**ID:** `litespec/core/intent#state-each-exception-with-its-fallback`

**Strength:** rule

**Applies to:** artifact: record:requirement

**Statement:** A requirement that names an `except` condition MUST name its `fallback` requirement in the same entry.

**Rationale:** An exception without a fallback leaves the reader at the exact moment the promise breaks: the product refuses with no stated alternative. The `condition` states in product words when the commitment does not hold, and `fallback` names the requirement that covers what happens then. The fallback carries no `except` of its own, so the chain stays one deep and every exception ends in a promise.

**Example:** A reimbursed expense lands in the employee's account, never more than they spent. Except when the payout bank's settlement window has closed for that batch, with `fallback` naming the requirement that has finance repay the employee by other means.

**Checked by:** `litespec model check` reports no `except-fallback-missing` finding.

### Write Journey Steps as Sentences with Ids

**ID:** `litespec/core/intent#write-journey-steps-as-sentences-with-ids`

**Strength:** rule

**Applies to:** artifact: record:journey

**Statement:** Each journey step MUST carry its `id` and its product sentence in `statement`, and name no use case.

**Rationale:** A step written as a product sentence reads in journey detail with no Behavior knowledge: it states what a person experiences, not which handler runs. The `id` (`s1`, `s2`, and so on) gives Behavior a stable target: use cases, reactions, and jobs point up with `serves` to `journey/<name>#s1`. A step that names its use case drags Behavior into Intent and breaks the journey detail every time Behavior renames. The performer and the failure text are optional: a step names the walking actor only at a handoff in `actor`, or the system record performing it in `system` when no person acts, and carries `failure` where the walk can fail.

**Example:** Step `s4` states `Finance reimburses the approved expense.` in `statement` and names no use case; `use-case/spending.reimburse-expense` names `journey/submit-and-reimburse#s4` in `serves`.

**Checked by:** `litespec model check` reports `record-invalid` for a journey step missing `id` or `statement`, or naming a use case.

### State When Each Journey Finishes

**ID:** `litespec/core/intent#state-when-each-journey-finishes`

**Strength:** rule

**Applies to:** artifact: record:journey

**Statement:** A journey MUST state its observable end in `finishedWhen`.

**Rationale:** A journey without a stated end can stop anywhere: the reader cannot tell a finished walk from an abandoned one, and serving records cannot tell done from dropped. `finishedWhen` states the observable end in one sentence, in product words.

**Example:** `finishedWhen`: the employee holds the reimbursement.

**Checked by:** `litespec model check` reports `record-invalid` for a journey record file missing `finishedWhen`.

### Treat Tenant-Set Values as Data

**ID:** `litespec/core/intent#treat-tenant-set-values-as-data`

**Strength:** rule

**Applies to:** artifact: record:requirement

**Statement:** A consumer MUST treat a `setBy: tenant` value as tenant data.

**Rationale:** Tenants choose their own values within the ranges Intent sets: a tenant holds its receipt threshold at EUR 25 by default within the EUR 0 to EUR 500 range. The requirement sets the range and the default, and each tenant's choice inside it is data. Treating tenant values as product values would bind every tenant to one tenant's choice.

**Example:** The tenant sets a receipt threshold of EUR 50; the requirement covering threshold ranges already sets the range, so the threshold itself is data.

**Reviewed for:** Each `setBy: tenant` value stored as tenant data.

## Conventions

### Group Product Records by Module

**ID:** `litespec/core/intent#group-product-records-by-module`

**Strength:** default

**Applies to:** artifact: record:product, record:goal, record:problem, record:constraint, record:decision, record:research, record:actor, record:role, record:journey, record:module, record:feature

**Default:** Keep the product record at `.litespec/intent/product.yaml`, product-level goals, problems, constraints, decisions, research, actors and journeys in their folders under `.litespec/intent/`, and each module with its goals, problems, constraints, decisions, research and features under `.litespec/intent/modules/<module>/`.

**Replacement:** A project records its own product layout in its documentation index.

**Rationale:** A module folder keeps related pages together, so the pages of one product area sit side by side. One folder per kind keeps each record findable: goals, problems, features, constraints, decisions, research, journeys, and actors each have one home. A newcomer learns the layout once and predicts where the next record lives. A module names a product area, never the company, and each module folder names a module declared in Behavior.

**Example:**

```text
.litespec/intent/
  product.yaml
  goals/
    <name>.yaml
  problems/
    <name>.yaml
  constraints/
    <name>.yaml
  decisions/
    <number>-<name>.yaml
  research/
    <name>.yaml
  actors/
    <name>.yaml
  journeys/
    <name>.yaml
  modules/
    spending/
      module.yaml
      goals/
        <name>.yaml
      problems/
        <name>.yaml
      constraints/
        <name>.yaml
      decisions/
        <number>-<name>.yaml
      research/
        <name>.yaml
      features/
        <name>.yaml
```

**Reviewed for:** The `.litespec/intent/` layout beside the project's recorded layout.

### Name Product Decisions in One Numbered Sequence

**ID:** `litespec/core/intent#name-product-decisions-in-one-numbered-sequence`

**Strength:** default

**Applies to:** artifact: record:decision

**Default:** Name each product decision `<four-digit number>-<name>.yaml` and use `decision/<four-digit number>-<name>` as its identifier.

**Replacement:** A project records its own decision naming in its documentation index.

**Rationale:** One numbered sequence orders each decision by when the choice was proposed, giving every record a short stable name. The number counts as a word, so digits never stand alone in the identifier. Scattered names would force every citation to carry the full path.

**Reviewed for:** Each decision file named `<number>-<name>.yaml` beside its matching identifier.

## Reference Example

This informative example demonstrates `litespec/core/intent#order-each-journey-toward-one-goal`, `litespec/core/intent#write-journey-steps-as-sentences-with-ids`, `litespec/core/intent#state-when-each-journey-finishes`, `litespec/core/intent#identify-every-requirement`, `litespec/core/intent#state-each-exception-with-its-fallback`, and `litespec/core/intent#treat-tenant-set-values-as-data`.

The book shows journey `journey/submit-and-reimburse` as record detail. No field name, YAML, or Behavior page appears.

```text
Submit and reimburse

An employee submits an expense and is reimbursed after approval.

Finished when: the employee holds the reimbursement.

1. The employee submits the expense with its amount and currency.
2. The employee attaches the receipt to the expense.
3. The manager approves or rejects the expense within five business days.
4. Finance reimburses the approved expense.
If it goes wrong: an expense that waits past three business days earns
its approver a reminder; an expense above the threshold with no receipt
cannot be submitted; an expense nobody approves is never reimbursed.

Promises
- Each submitted expense is decided within five business days.
- An expense above the receipt threshold carries a receipt.
- A reimbursed expense lands in the employee's account, never more than
  they spent.
    Except when the payout bank's settlement window has closed for that batch.
    Then finance repays the employee by other means and Claimwick records it.
- Nobody approves their own expense.
- An approved expense is reimbursed at most once.
```

For step 4, the serving use case `use-case/spending.reimburse-expense` contributes its goal in one line, and its scenarios contribute their Then clauses as context.
