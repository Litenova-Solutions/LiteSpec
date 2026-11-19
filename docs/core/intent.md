# Intent Layer

**Stack:** any

**Concern:** records

Intent is why the product exists and what it promises: its actors, goals, directions, problems, features, requirements, flows, constraints, research, decisions, and terms, under the product record at `.litespec/product.yaml`.

This page defines where each Intent record lives, what shape it takes, and what stays out of Intent. Behavior states how the system keeps these promises. The [Record Language](language.md) page states how each field, value, and record name is written.

## Concepts

### The Questions Intent Answers

Each Intent record type answers one question, so the question you are asking names the record to write. When a record fits none of these questions, it probably belongs in Behavior or outside the records entirely.

| Question | Record | Write It When | Skip It When | Familiar As |
|:---|:---|:---|:---|:---|
| What is the product and for whom? | product | Always, once | Never | Product brief |
| Who touches the product? | actor | A person, an organization, or an outside system appears in a problem, a flow or an access position | The candidate is a position (a role record) or internal machinery that takes no flow step | Stakeholder |
| What software outcome do we want? | goal | You can name what you count | The system controls it directly: that is a quality requirement | Product goal |
| Which way might the product grow beyond what it promises now? | direction | A choice made now keeps a later way of growing open, or the owner wants a wish on record | You can name the pain and the offer: write the problem and the feature | Product direction |
| What pain do people outside the team have today? | problem | You can name who suffers, what it costs them and what they do instead, with evidence | Only one solution is imaginable: it is a feature | Pain point |
| What do we offer against a problem? | feature | Scope needs tracking | The commitment is permanent and no offer carries it, so it is a product requirement | Capability |
| What exactly must hold? | requirement | Always | Never | User story |
| Can an actor get from start to finish? | flow | A task crosses several actions | It is one action | User flow |
| Which outside rule binds us? | constraint | An outside party can hold us to it | It is our own choice: a decision | Mandated constraint |
| Which outside fact did we rely on, and when was it true? | research | A choice rests on it and it could change | It binds us: a constraint | Spike |
| Which option did we choose, and what reopens it? | decision | There were real options | There was nothing to choose | Decision record |
| Which word means what? | term | A word could mean two things | Never: a domain word always takes a term | Glossary term |

A record earns its place by answering its question with evidence. A problem no sufferer confirmed, a goal with nothing to count, or a constraint no source backs is a draft for the backlog, not a promise the team can build on.

### Product Intent and Its Parts

Product intent is what the product commits to: what it does, for whom, and where it stops. Running the company is no part of it. Prices, markets, hiring, and launch dates stay out, except as a constraint that binds the software or as dated research behind a choice. The software-only rule in `litespec/core/principles#write-every-record-about-the-software` draws that boundary, and [The Software Test](#the-software-test) applies it to each record type.

Read Intent along the chain from problem to requirement. A problem names one pain an outsider suffers today, with the evidence that it is real. A feature is the offer against that pain. Each feature sits in the `features/` folder of the one problem it answers and names the goals it moves in `advances`. A requirement states one commitment Behavior fulfills. Each requirement sits in its own folder inside its feature's folder, and keeps one authoritative file there; a commitment no offer carries, such as tenant isolation, sits at product level instead.

The [model page](model.md) defines the record types and their links. The book gathers each feature with its requirements and the decisions behind them, and groups every feature under the problem it answers.

The product states what it leaves out in `excludes`: only an `excludes` entry sets a requirement aside, and the product summary is never read as an exclusion.

Intent states what the product decided and why; Behavior states what the system does today. A rule or use case matches the code, while a requirement can describe a position the code does not carry yet.

The two layers connect in one direction: Behavior records name the requirements they fulfill and the flow steps they serve. An Intent record never describes Behavior, with one narrow exception: an actor names its roles in `holds`, and actor and role are both Intent, so it is not a layer down-link. A settled record names its decision in `basedOn` instead of the decision naming the record.

### The Software Test

The software test decides whether a record belongs in Intent at all. A record belongs when it states something the software does, holds, refuses at runtime, or runs on: something that translates into code, results, tests, or infrastructure. The folder can be right and the content still wrong. A company goal filed as a product goal fails the test even when it sits in `intent/goals/`.

The table applies the test to each type:

| Type | Belongs when it is | Out when it is |
|:---|:---|:---|
| Goal | A software outcome: "Nights run without oversell or unreconciled money" | A business outcome: retention, revenue, market share |
| Direction | A way the software may grow: "Contractors submit expenses as employees do" | A roadmap entry with a date, a priority or a release |
| Problem | A user pain the software answers | A sales or market problem |
| Feature | A capability the software has | A marketing tactic, a launch plan, or a refusal |
| Requirement | A commitment Behavior fulfills | (always software when it reaches Behavior) |
| Constraint | Law, a provider or a limit that binds the software | A company duty with no software effect, such as incorporation |
| Research | Evidence a software record cites | Market sizing, viability, acquisition, pricing strategy |

A record that states company operation fails the test, whatever its folder. Reachability proves the rest: a record that reaches no Behavior is either unfinished work for the backlog or not software at all.

### Where Each Record Lives

Each record sits in its parent's folder, and the folder names that parent. You find a requirement by walking from the product to its problem, its feature, and the requirement itself:

```text
.litespec/
  product.yaml               the product, above the record layers
  intent/
    goals/  directions/  constraints/  decisions/  research/  actors/  roles/  flows/  requirements/
    problems/
      payouts-wait/
        payouts-wait.yaml
        payouts-wait.md          its explanation
        features/
          payout/
            payout.yaml              the feature
            payout.md                its explanation, optional
            requirements/
              paid-once/             requirement
                paid-once.yaml
                paid-once.md         its explanation, optional
    requirements/
      tenant-isolation/
        tenant-isolation.yaml    product requirement
    flows/
      pay-a-payout/
        pay-a-payout.yaml
```

The product record stands at the root of `.litespec/` as `product.yaml`, above the record layers. Product level holds problems, directions, goals, constraints, decisions, research, actors, roles, flows, and product requirements, each kind in its own folder. Each problem's folder holds the problem and the features that answer it, and each feature's folder holds the feature and its requirements. Flows cross modules through their steps.

Decision folders take their numbers from one sequence across every home, as `litespec/core/model#take-decision-numbers-from-one-sequence` states.

Every folder comes from a closed set per layer. A record of a type in the wrong folder, and a record type outside its allowed levels, are blocking findings.

Every Intent record sits in the home its type names, as `litespec/core/model#write-every-record-in-its-home` states.

Each explanation file states the same specification as its record, restated and elaborated for a person, as `litespec/core/writing#explain-the-same-specification-the-records-state` states.

Modules are not declared here. A module is a code boundary, so Behavior declares it. [Modules as Product Areas](behavior.md#modules-as-product-areas) states how one module name runs through Behavior and code.

### Identifiers and Placement

An identifier names the record, not its place. `problem/organizers-wait-for-ticket-money` is unique in the product, and every link and citation uses it. When a feature moves to another problem, you move its folder and change no citation, because the identifier names no problem.

Placement is separate from the identifier: the folder path and the record's `module` state where the record sits. The book and the CLI show the identifier together with that placement, so you can tell where a record sits without a second identifier.

### How Intent Reaches Behavior

Records disagree sometimes. The [model page](model.md) states the authority order that settles which one wins. Goals, directions, problems, features, flows, actors, research, and prose carry no authority, and between peers a decision wins.

A decision settles its choice, and a feature states the choice as requirements. Behavior records name the requirements they fulfill and the flow steps they serve, and the code carries what happens today.

When Behavior contradicts a requirement, either the code carries a defect or the product changed. A changed product means a new decision first, before any record or code moves. A lower record never overrides a higher one; only an entry in the product `excludes` sets a requirement aside.

Facts from the outside world enter Intent as research or as a constraint, cited and dated. Where Intent stays silent, working code is evidence that a record is missing, not a defect to delete.

### The Product Brand

The product record carries the brand in `brand`, because the brand is a product choice: who the product addresses, what it stands for, and what every screen keeps. Every screen inherits it from there. The brand holds five parts:

- `reader`: who the product addresses, read as a sentence.
- `accountableFor`: the one thing the product answers for on every screen, read as a sentence.
- `is`: what the product is, exactly three single words.
- `isNot`: what the product is not, exactly three single words.
- `do` and `doNot`: the rules every screen keeps, plain sentences a reviewer checks on any screen.

The words a screen avoids come from the glossary: each term's `rejected` words are the words not to use, and its `aliases` help readers find it. Its `labels` give the word per language.

### Requirements with Except and Fallback

A requirement carries `kind`, `statement`, and `rationale`: whether it is functional, quality, or policy; the commitment in one testable sentence; and the reason in one line. A quality requirement whose statement names a number also carries `measure`, naming what is counted. `setBy` names who sets its value: `product`, `operator`, or `tenant`.

Some commitments do not hold in every case. An `except` entry states in product words when the commitment does not hold, and `fallback` names the requirement that covers what happens then. The fallback carries no `except` of its own: exceptions stay one deep, so every exception ends in a promise.

### Flow Shape and Fields

A flow is one task's path through the software: a trigger, ordered steps, alternative paths, step failures, and an end state. Its name is the standard user flow term, so it fits any actor. The fields match that shape: `steps` hold the main flow in order, and each step holds the alternative paths leaving it in its own `branches`.

A flow leads one actor toward one goal, problem, or constraint. It carries `actor`, `summary`, `startedWhen`, `finishedWhen`, ordered `steps`, and exactly one reason: `serves` naming one goal, or `answers` naming one problem or one constraint. Each reason field holds a list of exactly one identifier, the shape every link field has.

`summary` states the flow's purpose in up to four sentences: who takes the flow, what starts it, what good looks like, and the consequence at stake and for whom. The book renders it first on the page, so a cold reader learns the purpose before the mechanics. `startedWhen` states the observable state in which the flow starts, in one sentence; `finishedWhen` states the end, in one sentence.

Each step carries its `id` as `step/<flow>.<act>`, where `<flow>` is the flow's name and `<act>` is the step's act as a verb phrase in kebab case, such as `step/submit-and-reimburse.submit-expense`. The id names the act, so a citation reads without opening the flow, and list order is the main path, so no position lives in an id. Each step carries its `title`, a short Title Case name of its act, and the id's act is the title's words in kebab case. The step states one act of its performer in one product sentence in `statement`. A step names another actor only at a handoff, in `actor`: another person or organization, or the system actor standing for the outside system when no person acts. It names no performer when the actor taking the flow performs it. Where the flow can fail, the step carries its failure text in `failure`. Each step names the requirements it carries in `carries`. The flow carries no `carries` of its own; the model reads the union of its steps.

Each branch sits on the step it leaves, in that step's `branches`. A branch states one `when` condition, and exactly one of `then`, naming the step of the same flow where the path continues, or `ends`, stating the outcome when the flow ends there. A branch that does work of its own states that work in `instead`, with its own `carries` where the work needs them; the work is done by the step's performer.

Steps name no use case. Use cases, reactions, and jobs point up with `serves`, naming the steps they serve. Jobs and reactions never stand in `actor`; they serve the step from above like use cases. Code never marks a step: code points one layer up, to the behavior serving it, and the trace walks from there.

The book shows each flow as record detail: `startedWhen`, the steps with their titles and sentences, their `failure` texts and their branches indented under them, `finishedWhen`, each step with the requirements it cites (with their statements, `except` entries, and fallbacks inline), the serving use cases' goals, and the Then clauses of their scenarios as context. Each step carries its trace: the screens and use cases serving it, the requirements it carries with the use cases fulfilling them, and the code files and tests behind them. Field names never render literally in flow detail.

A step names another actor only at a handoff, or the system actor standing for the outside system when no person acts, and a flow with several reasons splits into several flows.

### Staff Flows

Flows are not only customer flows. A staff actor takes a staff flow the same way: finance exporting the approved batch carries `serves` or `answers`, `startedWhen`, `finishedWhen`, and steps with their branches like any other flow, and staff steps read as product sentences too.

A scheduled promise is different: it is a requirement that a job fulfills, not a flow, because no actor takes a path through it.

### Directions

A direction states a way the product might grow beyond what it promises now: a light wish the owner keeps in the records, which may or may not ever become requirements, such as contractors submitting expenses the way employees do. It states `statement`, takes `summary` only when the owner wants it, sits at product level under `.litespec/intent/directions/`, and holds no date, release, priority, status, or order among directions. Its explanation file is free-form. Order and timing of work stay on the backlog, outside the records.

A direction exists so a choice made today can name the future it keeps open. A decision that shapes a record or a model so a direction stays reachable names the direction in `basedOn`. A requirement that takes a step toward a direction names it in `cites`. Nothing else links to a direction, and a direction links to nothing except `terms`.

A direction reaches no Behavior, by design. It promises nothing, so no reach check reads it and no finding reports a direction that nothing cites: an uncited direction is a legitimate wish. An agent builds nothing from a direction until a requirement that cites it exists: when the way of growing becomes concrete, someone writes the problem, the feature, and its requirements; the requirements cite the direction; and the usual reach rules apply to them.

### What Intent Never Contains

Intent states decisions and promises, not how the system works today. States, results, codes, transactions, edges, consistency modes, Behavior field names, and code belong to Behavior. `litespec/core/intent#keep-behavior-out-of-intent` names everything that stays out.

## Standards

### State One Product

**ID:** `litespec/core/intent#state-one-product`

**Strength:** required

**Applies to:** artifact: record:product

**Statement:** A consumer MUST state exactly one product record at `.litespec/product.yaml`, with `boundary`, `users`, and `summary`.

**Rationale:** One product record gives every reader one answer to what the product is and who it serves. The `summary` states what the product is, and the `boundary` and `users` focus every later choice: two product statements let each plan cite the one it prefers.

The product lists what it leaves out in `excludes`, in plain sentences. Only an `excludes` entry sets a requirement aside; the summary is never read as one.

**Checked by:** `litespec model check` reports no `product-count-invalid` finding, and `litespec model check` reports `record-invalid` for product fields outside the record schema.

### State the Brand on the Product

**ID:** `litespec/core/intent#state-the-brand-on-the-product`

**Strength:** required

**Applies to:** artifact: record:product

**Statement:** A consumer MUST state the product brand in `brand`, with its reader, what the product is accountable for on every screen, three words the product is, three words it is not, and the rules every screen keeps.

**Rationale:** The brand decides what every screen keeps, so it belongs with the product every screen serves. Filed beside token values and component names in the implementation design contract, it sits outside what the records govern, and a project claiming only Intent and Behavior has nowhere to state it. Stated on the product record, the reader, the accountability, the two word lists, and the screen rules travel with the product itself. Proposed by [decision/0089-specify-the-ui-as-a-blueprint](decision/0089-specify-the-ui-as-a-blueprint).

**Example:** Claimwick states its reader as an employee submitting expenses from a phone, its accountability as showing what each expense waits for, `is` as calm, plain, and exact, `isNot` as playful, dense, and showy, and one `do` rule: every screen shows each amount with its currency code.

**Checked by:** `litespec model check` reports `record-invalid` for a product `brand` outside the record schema.

### Reach Every Intent Record with Behavior

**ID:** `litespec/core/intent#reach-every-intent-record-with-behavior`

**Strength:** required

**Applies to:** artifact: any; layer: behavior

**Statement:** Each Intent record other than a direction MUST reach Behavior through its stated path.

**Rationale:** The folder fixes where a record sits; reachability fixes that it leads to software. An Intent record no Behavior answers is either work the backlog still holds or content that was never software, and without this rule nobody can tell which.

A direction is the one exception: it states a way the product may grow and promises nothing yet, so it reaches nothing by design. Every other kind has its stated path: a goal reaches a flow or a quality requirement, a problem reaches a feature, a citing requirement, or an answering flow, a feature or requirement reaches a use case, a flow, or a quality scenario, a constraint is answered by a requirement or a flow, research is cited by a record that reaches Behavior, and a decision is named in `basedOn` by a record that reaches Behavior.

**Checked by:** `litespec model check` reports `intent-unreached` for a record that reaches no Behavior.

### State Each Goal with Its Measure

**ID:** `litespec/core/intent#state-each-goal-with-its-measure`

**Strength:** required

**Applies to:** artifact: record:goal

**Statement:** A goal MUST state its `summary`, `statement`, `measure`, and `target` in fields.

**Rationale:** Without a measure, nobody can tell whether the product serves the goal, so the goal is a wish no check can confirm. The four fields each do one job: `summary` states what the goal is in one paragraph, `statement` names the outcome and the consequence at stake, and `measure` with `target` names what is counted and the number aimed for. A goal explains why features exist; it carries no authority and never decides a conflict. Reworded by [decision/0049-literal-flow-words-concrete-consequences](decision/0049-literal-flow-words-concrete-consequences).

**Checked by:** `litespec model check` reports `record-invalid` for a goal record file missing a required field.

### Move Every Goal with a Flow or a Quality Requirement

**ID:** `litespec/core/intent#move-every-goal-with-a-flow-or-a-quality-requirement`

**Strength:** required

**Applies to:** artifact: record:goal

**Statement:** A goal MUST be moved by a flow serving it or by a quality requirement citing it.

**Rationale:** A goal no flow serves and no quality requirement confirms is an outcome the product never touches: a company hope filed as a product goal. A flow serving the goal shows the path that moves the counted outcome, and a quality requirement citing it states the measure that confirms the move. Only a flow naming the goal in `serves` moves it; a flow answering a problem or a constraint moves no goal measure.

An outcome the system controls directly is a quality requirement, not a goal, and an outcome only the company controls is no goal at all. Reworded by [decision/0049-literal-flow-words-concrete-consequences](decision/0049-literal-flow-words-concrete-consequences).

**Example:** The library goal of faster pickup is moved by the borrow-and-return flow serving it. A goal of higher membership sales is moved by no flow, so it is no product goal.

**Checked by:** `litespec model check` reports `goal-mover-missing` for a goal moved by no flow and no quality requirement.

### Record Goal Outcomes as Observations

**ID:** `litespec/core/intent#record-goal-outcomes-as-observations`

**Strength:** required

**Applies to:** artifact: record:goal

**Statement:** A goal MUST record each measured outcome in `observations` with its `date`, `value` and `source`.

**Rationale:** Delivery is not outcome. A goal's completion shows that its features, flows, and requirements exist; it never shows the counted outcome moved. Until the goal records observations, the outcome reads "not measured", and nobody mistakes shipped work for a reached goal. Each observation carries the date it was read, the measured value, and the source, so a reader sees what moved and how anyone knows. Proposed by decision 0085.

**Example:** The library goal of faster pickup records each month the share of holds ready within one day, with the report it was read from. Until the first observation, the book shows the outcome as not measured.

**Reviewed for:** Does every goal record its measured outcomes, each with a date, a value, and a source? Look at each goal record beside its completion.

### State Each Direction in Its Statement

**ID:** `litespec/core/intent#state-each-direction-in-its-statement`

**Strength:** required

**Applies to:** artifact: record:direction

**Statement:** A direction MUST state the way the product may grow in its `statement` field and sit at product level under `.litespec/intent/directions/`.

**Rationale:** Someone reads a direction long after it was written, usually to judge whether a choice still serves it. That reader needs the way of growing stated plainly: `statement` names the product once it has grown this way, as a present-tense fact. The direction sits at product level because it belongs to the whole product: ways of growing such as contractors joining the employee flow cross modules, and a module-level direction would hide them. The `summary` says who wants it and why, and stays optional, because a wish needs no case made for it; the explanation file is free-form, so the owner writes as much or as little as the wish needs. Settled by [decision/0094-record-product-directions](decision/0094-record-product-directions).

**Example:** Claimwick's `direction/contractor-expenses` states "Contractors submit and track their work expenses in Claimwick as employees do." in `statement`, and its summary names the contractors who invoice their costs by hand today.

**Checked by:** `litespec model check` reports `record-invalid` for a direction record file missing `statement`, and `record-placement-invalid` for a direction outside `intent/directions/`.

### Keep Dates and Priorities Out of Directions

**ID:** `litespec/core/intent#keep-dates-and-priorities-out-of-directions`

**Strength:** required

**Applies to:** artifact: record:direction

**Statement:** A direction MUST name no date, release, priority, status, or order among directions.

**Rationale:** A direction states a way the product may grow, which changes rarely. When the work happens, and in which order, is planning: it changes every week and belongs to the backlog, outside the records, as running the company does. A date or priority written into a direction goes stale without any check noticing, and a later reader takes a forgotten plan for a commitment.

**Example:** Claimwick's contractor direction names the way of growing only. That the team may start on it after the approval reminders ship is a backlog fact, kept on the board.

**Reviewed for:** Does any direction name a date, a release, a priority, a status, or an order among directions? Look at each direction's fields and explanation file.

### Build Nothing from a Direction

**ID:** `litespec/core/intent#build-nothing-from-a-direction`

**Strength:** required

**Applies to:** artifact: record:direction

**Statement:** An agent MUST build no record below Intent, code, or test from a direction until a requirement that cites the direction exists.

**Rationale:** The specification is closed: an agent builds what Intent promises and nothing more. A direction promises nothing, so building from it adds behavior no requirement asks for. When the way of growing becomes concrete, the owner's problem, feature, and requirements state the promise and cite the direction, and the agent builds from those. Until then a direction may shape a decision that keeps it open, never the code itself.

**Example:** An agent asked to add the approval reminders builds them from their requirements. It adds no contractor fields just because Claimwick holds a contractor direction.

**Reviewed for:** Does any Behavior record, code, or test serve only a direction, with no citing requirement behind it? Compare the change with the requirements that cite each direction.

### Cite the Direction a Decision Keeps Open

**ID:** `litespec/core/intent#cite-the-direction-a-decision-keeps-open`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** A decision chosen so that a direction stays reachable MUST name that direction in `basedOn`.

**Rationale:** A choice made for a later way of growing looks like over-engineering to a reader who cannot see that way of growing. Naming the direction in `basedOn` gives the reason an identifier: the reader follows it to the direction, the book shows the decision under it, and a later change to the direction shows which choices to revisit. A reason kept only in prose is lost the first time someone rewrites the prose.

**Example:** A Claimwick decision records who submitted each expense as a person rather than as an employee, and names `direction/contractor-expenses` in `basedOn`. A reader sees why the expense holds no employee number.

**Reviewed for:** Does every decision made to keep a later way of growing open name that direction in `basedOn`? Look at each decision's options and consequences beside the directions.

### Admit Only Outside Pains as Problems

**ID:** `litespec/core/intent#admit-only-outside-pains-as-problems`

**Strength:** required

**Applies to:** artifact: record:problem

**Statement:** A problem MUST name in `affects` a primary sufferer that is a person or organization actor outside the team building the product, state the pain in `summary` and `statement` in fields, carry graded evidence, state its failure cases in `failureCases`, its workaround in `workaround`, its cost in `cost`, and its resolution criteria in `resolutionCriteria`.

**Rationale:** A problem is a pain someone outside the team has today. The first `affects` entry is the primary sufferer, so who has the pain is the first fact a reader meets. The `statement` names the sufferer, the condition, and the consequence in one sentence, with the product never as its subject; the `summary` states the pain in one paragraph: when it happens and what the person was trying to do, never a bare pointer to another page. The graded evidence shows the pain is real, as [Grade Problem Evidence by Kind](#grade-problem-evidence-by-kind) states. The `failureCases` state each failure that can happen on its own as its own sentence, with what is missing or wrong, how the sufferer finds it, and what follows when they do not. The `workaround` states what the sufferer does instead today, with the burden that work carries. The `cost` states what the pain costs the sufferer, with time, money, and risk kept apart. The `resolutionCriteria` state one observable outcome per failure case, with no solution named. The explanation file restates each of these fields in its fixed sections.

Content that is no problem goes to its own record: a solution in disguise becomes a feature, a worry about what might happen becomes a decision or a goal, an internal limit becomes a limit on the plan written outside Intent, a market fact becomes research, and a legal duty becomes a constraint.

**Example:** In the library product, readers losing track of due dates is a problem whose primary sufferer is the reader. The library having too few staff for the summer is an internal limit, not a problem.

**Checked by:** `litespec model check` reports `problem-sufferer-invalid` for a problem whose primary sufferer is no person or organization outside the team; `litespec model check` reports `record-invalid` for a problem record file missing `affects`, `summary` or `statement`; `litespec model check` reports `record-invalid` for a problem record file missing `failureCases`, `workaround`, `cost`, or `resolutionCriteria`.

### Open Each Problem with Its Gap and Consequence

**ID:** `litespec/core/intent#open-each-problem-with-its-gap-and-consequence`

**Strength:** required

**Applies to:** artifact: record:problem

**Statement:** A problem explanation file MUST open with a paragraph that states what the sufferer cannot reliably do, the gap that causes it, and what the sufferer loses or risks as a result.

**Rationale:** The opening paragraph may be all a cold reader reads, so it carries why the problem matters. An opening that starts with the workaround tells the reader a task is done by hand but never why the task is needed, and the reader supplies a cause of their own. The opening states three facts: what the sufferer cannot reliably do, the gap behind it, and what the sufferer loses or risks. That form is a check on the paragraph, not a sentence to copy. The workaround follows in its own section.

**Example:** The Claimwick explanation for waiting expenses opens: employees cannot get an expense paid back quickly, because nothing shows the approver how long an expense has waited, so employees carry the amount from their own account for weeks.

**Reviewed for:** Does every problem explanation open with what fails, the gap behind it, and its consequence? Read the opening paragraph of each problem explanation file.

### State Each Failure Case on Its Own

**ID:** `litespec/core/intent#state-each-failure-case-on-its-own`

**Strength:** required

**Applies to:** artifact: record:problem

**Statement:** Each item of a problem's `failureCases` MUST state one failure that can happen independently on its own, naming what is missing or wrong, how the sufferer finds it, and what follows when they do not.

**Rationale:** Two failures joined in one sentence read as one, and the reader cannot see that each needs its own check and its own resolution. Direction matters: when two records should agree, an entry missing from the first and an entry missing from the second are found by different checks, so a solution running only one check leaves the other failure in place. Naming how the sufferer finds each failure, and what follows when they miss it, shows what the failure costs and what a solution has to catch. The explanation file's Failure Cases section restates each `failureCases` item for a person.

**Example:** The library problem of readers losing due dates lists two failure cases. A loan missing from the reader's paper list goes overdue unnoticed, and the reader learns of it from the late fee. A renewed loan keeps its old date on the list, so the reader returns the copy early and loses the renewal.

**Reviewed for:** Does every failure case stand on its own, with how the sufferer finds it and what follows when they miss it? Look at each `failureCases` item of each problem record beside the Failure Cases section of its explanation file.

### State the Cost and Workaround of Every Problem

**ID:** `litespec/core/intent#state-the-cost-and-workaround-of-every-problem`

**Strength:** required

**Applies to:** artifact: record:problem

**Statement:** A problem MUST state in `workaround` what the sufferer does instead today with the burden that workaround carries, and in `cost` what the pain costs them, with time, money and risk each in the sufferer's own units or the word `unknown`.

**Rationale:** A pain with no cost and no workaround is a slogan: no reader can tell a daily loss from a passing annoyance, and no feature can be judged against the price of standing still. The workaround shows the pain is real enough to work around. Work done by hand is no burden on its own, so the workaround names the burden: the time it takes, what the sufferer repeats or has to remember, and what it still misses.

Time, money, and risk stay apart because each is judged in its own units; a total mixing them hides which one matters. Where nobody priced the cost or named the workaround, `unknown` states the gap honestly. The explanation file restates `workaround` and `cost` in its Current Workaround and Cost sections.

**Example:** A reader who loses track of due dates pays late fees, an amount nobody has measured. The reader keeps a paper list of due dates, updates it at every loan and renewal, and cannot check it when the list stays at home.

**Reviewed for:** Does every problem state its workaround with its burden, and its cost with time, money, and risk kept apart? Look at the `workaround` and `cost` fields of each problem record beside the Current Workaround and Cost sections of its explanation file.

### Claim No More Than the Evidence Shows

**ID:** `litespec/core/intent#claim-no-more-than-the-evidence-shows`

**Strength:** required

**Applies to:** artifact: record:problem

**Statement:** A problem explanation file MUST state each claim about who suffers, how often and at what cost no wider than its evidence shows, naming an estimate as an estimate and a possible loss as possible.

**Rationale:** An estimate written as a measurement sizes a feature for a pain nobody counted; a possible loss written as an observed one makes the problem look proven when nobody saw it happen; one sufferer's report written about every sufferer reads as true of all. Narrow claims keep what is known apart from what is assumed, so a reader can weigh the problem and name the evidence that would settle it.

**Example:** The library problem writes "One reader at the desk estimates a late fee each month; nobody has measured it", not "Readers pay a late fee each month".

**Reviewed for:** Does any problem claim state an estimate as a measurement, a possible loss as an observed one, or one sufferer's report as true of all? Look at Affected Users, Cost, and Evidence in each problem explanation file, beside its evidence entries.

### Grade Problem Evidence by Kind

**ID:** `litespec/core/intent#grade-problem-evidence-by-kind`

**Strength:** required

**Applies to:** artifact: record:problem

**Statement:** A problem MUST list each evidence entry with its `claim`, its `source` as a record id or a URL, its `readOn` date, and its `kind` as observed, reported or assumed, and carry at least one observed or reported entry.

**Rationale:** Evidence states how anyone knows the pain is real. Observed means a sufferer was watched, reported means a sufferer said it, and assumed means nobody outside the team confirmed it. The strength of a problem is the strongest kind present: the builder computes it and nobody writes it, so it cannot drift from the entries. A problem resting on assumption alone, or on no entry, is unfinished work for the backlog until a sufferer is watched or heard.

**Example:** The library problem of readers losing due dates carries a reported entry from the desk log, read on its date, so its computed strength is reported.

**Checked by:** `litespec model check` reports `problem-evidence-missing` for a problem with only assumed evidence or none. The builder writes `evidenceStrength` on each problem in the model.

### Keep Feasibility Out of Problem Evidence

**ID:** `litespec/core/intent#keep-feasibility-out-of-problem-evidence`

**Strength:** required

**Applies to:** artifact: record:problem

**Statement:** A problem evidence entry MUST show that the sufferer has the pain, not only that a solution can be built.

**Rationale:** A file format, an API, an integration, or another product shows a solution is possible and says nothing about whether anyone loses time or money today. Feasibility kept in the evidence makes a problem look proven by the existence of its answer. It belongs in research that a feature cites. A feature request is evidence of the requested solution; it supports the pain only for what the requester reports about their own work.

**Example:** The Claimwick problem of waiting expenses cites employees reporting a three-week wait. A payment provider page showing same-day transfers is feasibility: it belongs in research that a feature cites.

**Reviewed for:** Does any problem evidence entry show that a solution can be built rather than that sufferers have the pain? Look at each problem's evidence entries and the sources they cite.

### Name No Solution in a Problem Statement or Its Resolution Criteria

**ID:** `litespec/core/intent#name-no-solution-in-a-problem-statement`

**Strength:** required

**Applies to:** artifact: record:problem

**Statement:** A problem MUST name no product, feature, solution, or implementation in its `statement` or its `resolutionCriteria`, and resolve each failure case with an observable outcome for the sufferer.

**Rationale:** A statement naming a solution files a missing feature as a pain: the reader accepts the answer before weighing the pain, and every alternative answer reads as off-topic. A statement naming the sufferer, the condition, and the consequence leaves the answer to the feature that solves it.

Criteria covering one failure let a solution end half the problem and look finished. An outcome leaves the solution open, so a reader can propose a different one and judge it against the same criteria. The feature delivering the outcome sits in the folder of the problem it answers, so the book shows which feature answers it without the problem naming the feature. An implementation that is already required is a constraint, written as its own record.

**Example:** "Readers lose track of due dates" names a pain. "Readers need a reminder dashboard" names a solution and is reported. The library problem of readers losing due dates states two criteria, one for each failure case: a reader knows the due date of every copy they hold without a paper list, and a renewed copy shows its new date wherever the reader looks.

**Reviewed for:** Does any problem `statement` or `resolutionCriteria` outcome name a product, feature, solution, or implementation, and does every failure case have a resolution criterion stating an observable outcome for the sufferer? Look at the problem statements beside their sufferers and at `failureCases` beside `resolutionCriteria` in each problem record and its explanation file.

### Answer One Problem with Each Feature

**ID:** `litespec/core/intent#answer-one-problem-with-each-feature`

**Strength:** required

**Applies to:** artifact: record:feature

**Statement:** A feature MUST sit in the features folder of the one problem it answers, `intent/problems/<problem>/features/`, and an offer that answers two pains is two features.

**Rationale:** The folder names the problem, so a reader of a problem sees every answer to it beside the problem. An offer that answers two pains is two features, one per problem, so each pain keeps its answers beside it.

**Example:** The approval feature and the approval reminders feature both answer the expense wait problem, so both sit in `intent/problems/expenses-wait-weeks/features/`.

**Checked by:** `litespec model check` reports `record-placement-invalid` for a feature outside a problem's `features/` folder.

### Explain Every Problem in Its Fixed Sections

**ID:** `litespec/core/intent#explain-every-problem-in-its-fixed-sections`

**Strength:** required

**Applies to:** artifact: record:problem

**Statement:** A problem explanation file MUST carry Affected Users, Failure Cases, Current Workaround, Cost, Evidence, and Resolution Criteria, in that order, with an optional Example after Failure Cases.

**Rationale:** The fields state who suffers, the pain, each failure, the workaround, the cost, and the outcomes that would end each failure; the explanation restates them in fixed sections and adds the circumstances, one concrete case where the failures are hard to follow, and the evidence behind each claim. A fixed order means a reader who learned one problem page finds each part on every problem page.

A problem page is complete when a reader arriving cold can understand the problem, judge its evidence, and propose a different solution without asking its author. Settled by [decision/0044-state-problems-by-failure-case](../intent/decisions/0044-state-problems-by-failure-case/0044-state-problems-by-failure-case.md), which gives each problem its failure cases and one resolution criterion for each.

**Example:** The library problem on due dates lists its two failure cases, shows one reader at the desk with three overdue copies in Example, and closes with Resolution Criteria: a reader knows the due date of every copy they hold without a paper list, and a renewed copy shows its new date.

**Reviewed for:** Does every problem explanation carry each fixed section in order? Look at the H2 sections of each problem explanation file.

### State Each Feature with Its Offering

**ID:** `litespec/core/intent#state-each-feature-with-its-offering`

**Strength:** required

**Applies to:** artifact: record:feature

**Statement:** A feature MUST state its `offering`, its success criteria in `successCriteria`, what it leaves out in `excludes`, and name the goals it moves in `advances`.

**Rationale:** A feature without a written scope grows with every reading: each builder fills the gap with behavior nobody asked for. The `offering` states what is in and what is out, where the links are. The `successCriteria` state only outcomes the feature brings about on its own and a tester can check. The `excludes` state what the feature leaves to other features, which features it depends on, and each open question its owner has not decided. The explanation file restates both fields in its Success Criteria and Boundaries sections. The folder ties the offer to the pain it answers, and `advances` ties it to the goals it moves, so each offer traces to a pain beside an outcome. Proposed by [decision/0004-features-advance-goals](decision/0004-features-advance-goals) on the evidence in [research/intent-hierarchy-evidence](research/intent-hierarchy-evidence), [research/cross-layer-trace-evidence](research/cross-layer-trace-evidence), and [research/established-traceability-practice](research/established-traceability-practice).

**Reviewed for:** Does every feature state its offering, its success criteria, what it leaves out, and the goals it moves? Look at each feature record beside its goals.

### State the Boundaries of Every Feature

**ID:** `litespec/core/intent#state-the-boundaries-of-every-feature`

**Strength:** required

**Applies to:** artifact: record:feature

**Statement:** A feature MUST state in `excludes` what it leaves to other features, which features it depends on, and each open question its owner has not decided.

**Rationale:** A feature answering part of a problem reads as answering all of it unless its edges are written down. Builders then fill the gap with behavior nobody asked for. Naming what the feature leaves to other features, and which features it depends on, keeps each behavior in one feature. An open question written beside the feature reaches the owner as a question instead of reaching the code as a guess. Settled by [decision/0046-write-features-for-builders-and-testers](../intent/decisions/0046-write-features-for-builders-and-testers/0046-write-features-for-builders-and-testers.md), which writes each feature for the people who build and test it.

**Example:** The Claimwick approval feature leaves reminders for late decisions to the approval reminders feature, depends on the receipt threshold decision, and names one open question: whether a manager on leave hands decisions to a deputy.

**Reviewed for:** Does every feature state what the feature leaves to other features, what it depends on, and its open questions? Look at `excludes` and the Boundaries section of each feature explanation file.

### State Success Criteria the Feature Controls

**ID:** `litespec/core/intent#state-success-criteria-the-feature-controls`

**Strength:** required

**Applies to:** artifact: record:feature

**Statement:** A feature MUST state in `successCriteria` only outcomes that the feature brings about on its own and a tester can check.

**Rationale:** A criterion the feature cannot guarantee sets a test nobody can pass: importing a bank statement does not make the book balance equal the statement balance, because an entry can be missing or wrong and a later entry moves the balance again. A criterion the feature controls, such as showing both balances for the same date, can be built and checked. An outcome depending on people or on other features belongs to the goal the feature advances.

**Example:** The library feature for due dates states that a reader sees the due date of every copy they hold, not that no reader pays a late fee, which depends on the reader returning the copy in time.

**Reviewed for:** Does every success criterion state an outcome the feature brings about on its own, one a tester can check? Look at `successCriteria` and the Success Criteria section of each feature explanation file.

### Split Records Whose Parts Change Separately

**ID:** `litespec/core/intent#split-records-whose-parts-change-separately`

**Strength:** required

**Applies to:** artifact: record:constraint, record:research

**Statement:** A constraint or research record MUST hold only claims that share one question or source, one scope and one rate of change, so a claim verified or changed separately sits in its own record.

**Rationale:** A record bundling a rule from one source with a rule from another cannot be rechecked in one pass, and a change to one rule reopens the whole record. Splitting by the same test keeps each answer one reader can follow. The split stops where the parts of one answer no longer change apart: an answer spread over several records is as hard to follow as a bundled one. Proposed by [decision/0054-write-each-shared-record-rule-once](decision/0054-write-each-shared-record-rule-once).

**Example:** One constraint holds the seven-year receipt retention rule with its source, and a second holds the deduction rule for the same tax, because the two come from different sources and change on different dates.

**Reviewed for:** Does any constraint or research record bundle claims with different sources, scopes, or rates of change, or split one answer across records? Look at the records the change adds.

### Keep Actors to People Organizations and Systems

**ID:** `litespec/core/intent#keep-actors-to-people-organizations-and-systems`

**Strength:** required

**Applies to:** artifact: record:actor

**Statement:** An actor MUST carry a `kind` of person, organization, or system, state in `belongsToTeam` whether it belongs to the team building and running the product, state its responsibilities in `responsibilities`, and state what the product assumes it knows in `knowledge`.

**Rationale:** An actor answers for what it does. A person or an organization can hold a role, suffer a problem, and take a flow. A system actor stands for one outside system in flow steps, and that system's record names the actor in `represents`. Internal machinery such as a reaction, a job, a transport, or a renderer is never an actor. `belongsToTeam` reads as a predicate about the record, so a reader tells a team actor from a customer at a glance, and a problem can refuse a team sufferer. The `responsibilities` state the acts the product holds the actor to. The `knowledge` states the product's assumption about what the actor knows. The explanation file restates both fields in its Responsibilities and Knowledge sections. Reworded by [decision/0049-literal-flow-words-concrete-consequences](decision/0049-literal-flow-words-concrete-consequences).

**Example:** The library reader is an actor of kind person with `belongsToTeam` false. The text gateway standing in a flow step is an actor of kind system, named by its external-system record in `represents`.

**Checked by:** `litespec model check` reports `record-invalid` for an actor record file with a kind outside person, organization, and system, or with no `belongsToTeam`, `responsibilities`, or `knowledge`.

### State What the Product Assumes Each Actor Knows

**ID:** `litespec/core/intent#state-what-the-product-assumes-each-actor-knows`

**Strength:** required

**Applies to:** artifact: record:actor

**Statement:** An actor MUST state in `knowledge` what the product assumes the actor knows, as an assumption of the product, and it states no user's knowledge as a fact about every user.

**Rationale:** What a user knows decides what the product can ask of them, so a reader needs the assumption stated where it applies. A fact such as "has no training in accounting" written in the opening paragraph reads as a claim about every user, and the product has no way to check it. Written as "Claimwick does not assume the employee knows the expense policy", it names the product's assumption and the actor it applies to. The explanation file restates `knowledge` in its Knowledge section. Proposed by [decision/0053-actors-by-responsibility-and-knowledge](decision/0053-actors-by-responsibility-and-knowledge).

**Example:** Claimwick's employee actor states in its Knowledge section that Claimwick does not assume the employee knows the expense policy.

**Reviewed for:** Does any actor `knowledge` item state a user's knowledge as a fact about every user instead of an assumption of the product? Look at the `knowledge` items and the Knowledge section of each actor explanation.

### Name Every Actor from a Flow, a Problem or an Access Position

**ID:** `litespec/core/intent#name-every-actor-from-a-flow-a-problem-or-an-access-position`

**Strength:** required

**Applies to:** artifact: record:actor

**Statement:** Every actor MUST be named by a flow, by a problem in `affects`, by an access position in `holds`, or by an external system in `represents`.

**Rationale:** An actor named by nothing is a party nobody suffers as, acts as, grants to, or stands for in a flow: a leftover, or a machine promoted beside real parties. Naming each actor from the flow it takes, the pain it suffers, the role it holds, or the outside system it stands for keeps the actor list equal to the parties the product serves. Reworded by [decision/0049-literal-flow-words-concrete-consequences](decision/0049-literal-flow-words-concrete-consequences).

**Example:** The library reader is named by the borrow-and-return flow and by the due-date problem; the librarian is named by the holds of the librarian role; the text gateway actor is named by its external-system record in `represents`.

**Checked by:** `litespec model check` reports `actor-unused` for an actor named by no flow, problem, access position, or external system.

### Define Each Recurring Capitalized Phrase

**ID:** `litespec/core/intent#define-each-recurring-capitalized-phrase`

**Strength:** required

**Applies to:** artifact: record:term

**Statement:** A capitalized phrase of two to four words named in three or more records MUST be defined by a glossary term.

**Rationale:** A phrase used across records reads as a defined term whether or not any definition exists: readers and agents build on a meaning nobody wrote. Flagging the phrase while it is still cheap to define keeps the glossary equal to the words the records actually use.

**Example:** A library phrase naming its first lending partners, used across goals and problems, takes a glossary term stating who those partners are, or the records stop using the phrase.

**Checked by:** `litespec model check` reports `term-missing` for a capitalized phrase of two to four words named in three or more product records that no glossary term defines; research, constraints and decisions quote outside sources by their own names and are not read, and a phrase holding an acronym, a month, a currency code, a cast name or a sentence opener is no term.

### Answer Every Constraint

**ID:** `litespec/core/intent#answer-every-constraint`

**Strength:** required

**Applies to:** artifact: record:constraint

**Statement:** A constraint MUST state its `authority`, `summary`, `sources`, `kind`, `applies`, `appliesBecause`, `coverage`, `obligations`, and `productEffect` in fields, and What It Requires is generated from the requirements that cite it.

**Rationale:** An unanswered constraint is an outside rule the product can break unknowingly: a law, a regulation, or provider terms the team never examined. Recording each rule with its authority and applicability proves the team asked.

The `kind` is technical, organizational, convention, or legal. The `authority` names the body issuing the rule in a few words, and the `summary` states the rule with its condition and whom it binds, in the form "the authority requires the subject to act when the condition holds". Each source in `sources` carries its `name`, `url`, and `readOn`, the shape research uses, so each rule cites its source by name. A rule that stops binding is set `applies: false` with its reason in `appliesBecause`; no end date is written. What It Requires lists the requirements citing the constraint, generated at build time. The `coverage` states which subjects, transactions, and periods the rule covers. The `obligations` state each outside rule as one sourced statement with the names of the sources that support it. The `productEffect` states what the rule changes in the product, naming each requirement that holds it. The `openPoints` hold optional questions about application. The explanation file restates each of these fields in its fixed sections.

A constraint can state `effectiveFrom`, the date its rule starts to bind, and can name in `basedOn` the research it rests on: a fact learned from the world, such as the payout bank lengthening its settlement window, first lands as research with its source and date, then as a constraint naming it in `basedOn`. Proposed by [decision/0047-cite-a-source-for-each-constraint-rule](decision/0047-cite-a-source-for-each-constraint-rule).

**Checked by:** `litespec model check` reports `record-invalid` for a constraint record file missing a required field or holding no source, and `record-invalid` for a `basedOn` that names no research record; the builder generates What It Requires from citing requirements.

### Cite a Source for Each Constraint Rule

**ID:** `litespec/core/intent#cite-a-source-for-each-constraint-rule`

**Strength:** required

**Applies to:** artifact: record:constraint

**Statement:** Each item of a constraint's `obligations` MUST name in its `sources` one name of the constraint's `sources`, and the explanation holds the generated source-links region that writes each source's link definition.

**Rationale:** A reader checking an obligation needs the source beside it, and an obligation with two sources needs to show which one holds it. A law stating the rule on one public page is its own source, so the source sits on the constraint rather than in a research record of its own, as [decision/0047-cite-a-source-for-each-constraint-rule](decision/0047-cite-a-source-for-each-constraint-rule) decides.

`model render` writes each source's link definition into the explanation file, so the link resolves in a plain Markdown preview and a reader needs no book. A hand edit to that region is drift, because the record file holds the source. Proposed by [decision/0047-cite-a-source-for-each-constraint-rule](decision/0047-cite-a-source-for-each-constraint-rule).

**Checked by:** `litespec model check` reports `constraint-obligation-source-unknown` for an `obligations` item naming a name the constraint's `sources` list does not hold; `litespec model check` reports `source-link-bare` for a bare URL, `reference-unknown` for a name the constraint does not declare, and `source-unused` for a source no obligation cites, on the constraint's explanation; it reports `source-links-missing` and `source-links-drift` for the generated region.

### State Each Constraint Rule with Its Condition

**ID:** `litespec/core/intent#state-each-constraint-rule-with-its-condition`

**Strength:** required

**Applies to:** artifact: record:constraint

**Statement:** Each item of a constraint's `obligations` MUST keep the condition under which it holds, and it states only the outside obligation, with no product effect and no unverified fact stated as settled.

**Rationale:** A rule dropping its condition binds more or less than its source says, and the team builds against the wrong reach. An item stating what the product does about the obligation mixes two authors in one rule, so the product's response stays in `productEffect`, which the explanation's Effect on the Product section restates. The book shows the requirements citing the constraint from their `cites` links. A fact the source does not establish, written as settled, sends the team to build on a guess, so that question stays in the `openQuestions` of the research record.

Scope says whom the rule covers; it is separate from `applies`, which says whether the rule binds the product at all. A constraint holds rules sharing one source, scope, and change, so rules changing separately are separate constraints. Proposed by [decision/0047-cite-a-source-for-each-constraint-rule](decision/0047-cite-a-source-for-each-constraint-rule).

**Reviewed for:** Does any `obligations` item drop its condition, state a product effect, or state an unverified fact as settled? Look at each `obligations` item beside its source and the Rules section of the explanation.

### Record Research with Its Question, Conclusion, and Sources

**ID:** `litespec/core/intent#record-research-with-its-question-conclusion-and-sources`

**Strength:** required

**Applies to:** artifact: record:research

**Statement:** A research record MUST state its `question`, state its `conclusion` in one sentence, list each source with its `name`, `url`, and `readOn`, and state its `findings` in fields.

**Rationale:** A fact without its source cannot be rechecked when a decision depends on it. `readOn` says when someone read the source, so a later reader knows how old the reading is and whether to read it again. It does not say when a rule took effect: the date a rule starts, or the period it covers, is a claim a finding states with its source. Research without sources and read dates asks every later choice to rest on memory.

The `conclusion` states in one sentence what the sources establish, with the conditions the findings carry, and leaves out what follows: a consequence for the product belongs to the constraint, decision, or requirement citing the research. Each source carries its `name`, `url`, and `readOn`, an optional `note` holding what a later reader needs about that source, and `publishedOn` where the source states its publication date. The `findings` state each claim with its sources, as `litespec/core/intent#state-each-research-finding-with-its-source` requires. The `openQuestions` hold what the sources do not establish. The explanation file restates each of these fields in its fixed sections, citing a source with a reference link `[text][name]`; a bare URL, an unknown name, or an unused source is a finding. Observed provider behavior lands here too, cited and dated, before any constraint or requirement rests on it. Proposed by [decision/0009-research-states-its-conclusion](decision/0009-research-states-its-conclusion) on the evidence in [research/research-conclusion-evidence](research/research-conclusion-evidence), and by [decision/0052-tie-each-research-finding-to-its-source](decision/0052-tie-each-research-finding-to-its-source).

**Checked by:** `litespec model check` reports `record-invalid` for a research record file without `question`, `conclusion`, `findings`, or `sources`; `litespec model check` reports `source-link-bare` for a bare URL, `reference-unknown` for an unknown name, and `source-unused` for an unused source.

### State Each Research Finding with Its Source

**ID:** `litespec/core/intent#state-each-research-finding-with-its-source`

**Strength:** required

**Applies to:** artifact: record:research

**Statement:** Each item of a research record's `findings` MUST state one claim with its conditions, name in its `sources` one name of the record's `sources`, and say whether the source states the claim or the writer infers it.

**Rationale:** A reader rechecking a finding needs to know whether the source says it or the writer concluded it, so an inference is marked as one in the sentence stating it. A finding dropping a condition the source gives claims more than the source does, and one claim per item keeps each source tied to one statement.

The record's question tests the findings: a finding answering another question belongs in another research record, as [decision/0052-tie-each-research-finding-to-its-source](decision/0052-tie-each-research-finding-to-its-source) decides. A label such as high or medium confidence does not say what the source states, so the sentence says it instead. Proposed by [decision/0052-tie-each-research-finding-to-its-source](decision/0052-tie-each-research-finding-to-its-source).

**Reviewed for:** Does any `findings` item state an inference as a source statement, drop a condition the source gives, or answer a question other than the record's `question`? Look at each `findings` item beside its source. `litespec model check` reports `research-finding-source-unknown` for a `findings` item naming a name the record's `sources` list does not hold, and `finding-source-missing` for a Findings paragraph with no source link, so this review covers the wording only.

### State Each Decision with Context and Choice

**ID:** `litespec/core/intent#state-each-decision-with-context-and-choice`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** A decision MUST state its `context`, `choice`, and `consequences` in fields and carry Decision in its explanation file.

**Rationale:** A decision read cold needs its situation, its options, and its outcome in the same places every time. The `context` states the situation that forced the choice, Options compares each real option on the same points, and Decision states the chosen option in the explanation; the `choice` field names it, and `consequences` states what follows. The explanation file restates `context` in its Context section. Evidence holds the supporting material with its confidence where the choice rests on it.

The `reopenedWhen` condition names what would reopen the choice, so the decision stays stable until the world actually changes instead of expiring on a date. The `basedOn` link names the constraints and research behind it, and requirements only on a technical decision. The [model page](model.md) requires each product decision to be named in `basedOn` by at least one record it settles.

**Checked by:** `litespec model check` reports `record-invalid` for a decision record file missing a required field; `litespec model check` reports `explanation-missing` for a decision without Decision in its explanation file.

### Record Each Decision Option with Pros and Cons

**ID:** `litespec/core/intent#record-each-decision-option-with-pros-and-cons`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** Each decision option MUST carry its `pros` and its `cons`, with the options compared on their merits in Options.

**Rationale:** A later reader can revisit a choice argued from options and evidence, but not one argued from authority. Each option carries its name with what speaks for it in `pros` and against it in `cons`, so the comparison sits in the entries where every option shows both sides, instead of hiding in a prose section. Options compared on the same points let a later reader rejudge the choice when the world moves; a record naming only the winner forces the whole argument to run again. Any option singled out from the decision carries its own sentence saying why it stands apart.

**Example:** A Claimwick decision compares a receipt threshold of EUR 25 with EUR 50 on refusal rate and approver load.

**Checked by:** `litespec model check` reports `record-invalid` for a decision option missing `pros` or `cons`.

### Identify Every Requirement

**ID:** `litespec/core/intent#identify-every-requirement`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** Each requirement MUST carry one identifier that names its parent: `requirement/<feature>.<name>` for a feature requirement whose feature segment names the feature whose folder holds it, and `requirement/<name>` for a product requirement.

**Rationale:** Without an identifier, a Behavior record cannot name the commitment it fulfills, the model cannot resolve the name, and a commitment written only as prose cannot be traced from Intent to code. The identifier names the parent so the link reads alone: a feature requirement's folder sits inside its feature's folder, and the feature segment names that same feature wherever the identifier is cited. A product requirement sits at product level and names only its own folder. Settled by [decision/0045-keep-each-requirement-in-its-own-folder](../intent/decisions/0045-keep-each-requirement-in-its-own-folder/0045-keep-each-requirement-in-its-own-folder.md), which gives each requirement one authoritative file with room for an explanation. Proposed by [decision/0003-requirement-origin](decision/0003-requirement-origin) on the evidence in [research/intent-hierarchy-evidence](research/intent-hierarchy-evidence), [research/cross-layer-trace-evidence](research/cross-layer-trace-evidence), and [research/established-traceability-practice](research/established-traceability-practice).

**Checked by:** `litespec model check` reports `record-invalid` for a requirement without identifier; `litespec model check` reports `identifier-invalid` for a feature segment differing from the feature whose folder holds it, or for a product requirement carrying a feature segment.

### Place Product-Wide Commitments as Product Requirements

**ID:** `litespec/core/intent#place-product-wide-commitments-as-product-requirements`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** A requirement no offer carries MUST sit in `intent/requirements/<name>/` and cite in `cites` at least one constraint, goal or problem.

**Rationale:** A permanent commitment with no offer, such as tenant isolation, audit integrity, accessibility, or recovery, has no feature to hold it. Its `cites` takes the list a feature requirement takes, so the research and decisions behind it stay citable beside the constraint, goal, or problem it answers. Sitting at product level with its reason cited keeps it reachable without an invented feature. Proposed by decision 0080. Settled by [decision/0080-give-product-commitments-a-home](decision/0080-give-product-commitments-a-home).

**Example:** The library holds tenant isolation as `requirement/tenant-isolation` at product level, citing the constraint that binds it.

**Checked by:** `litespec model check` reports `record-placement-invalid` for a product requirement outside `intent/requirements/`, and `record-invalid` for a product requirement citing no constraint, goal or problem.

### State Each Requirement with Kind and Statement

**ID:** `litespec/core/intent#state-each-requirement-with-kind-and-statement`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** Each requirement MUST carry `kind` and `statement`.

**Rationale:** The `kind` tells the reader what sort of commitment this is: functional for what the product does, quality for how well it does it, or policy for what the business holds. A policy requirement carries its `values` with effective dates, so a later reader sees which value held when. The `statement` states the commitment in one testable sentence.

**Checked by:** `litespec model check` reports `record-invalid` for a requirement without `kind` or `statement`, or a policy requirement without dated values.

### State One Testable Behavior in Each Requirement

**ID:** `litespec/core/intent#state-one-testable-behavior-in-each-requirement`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** A requirement `statement` MUST state one behavior that a tester can pass or fail on its own, with each condition, date and amount its result depends on.

**Rationale:** A statement leaving out a condition passes in one reading and fails in another. "Each bank account shows its balance beside the closing balance of its last statement" names no date for the book balance, so today's balance meets last month's statement and correct books show a difference. Naming the date, the amount, or the condition makes the result decidable. One behavior per requirement lets each one be fulfilled, proven, and changed on its own. A requirement is complete when a developer can tell what to build and a tester can tell what passes and what fails.

**Example:** `requirement/approval.above-threshold-receipt` states that an expense above the receipt threshold carries a receipt, and `decision/0001-receipt-threshold` sets the threshold at 25 EUR, so a tester knows which expense passes.

**Reviewed for:** Does every requirement statement hold one behavior with each condition, date, and amount its result depends on? Look at the statements of each requirement.

### Keep Each Rationale to Its Reason

**ID:** `litespec/core/intent#keep-each-rationale-to-its-reason`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** A requirement `rationale` MUST state in one line why the requirement exists, adding no behavior and claiming no more than the reason supports.

**Rationale:** A commitment without its reason cannot be rejudged when the world moves: the reader finds what holds but not why. One line forces the reason into the open without inviting an essay. The feature explanation carries the full story, and an optional requirement explanation carries what the statement does not make obvious; the `rationale` carries the reason beside the commitment.

A rationale adding a rule hides a second requirement where no check reads it. A rationale naming one cause where several exist, or claiming a comparison nobody measured, misleads the reader rejudging the requirement later. A difference between two balances can come from a missing entry, a wrong amount, a duplicate, or a wrong opening balance, so "a difference means an entry is missing" is too narrow, and "the quickest sign" is unmeasured.

**Example:** The Claimwick rationale for `requirement/approval.nobody-approves-own-expense` reads "A second person checks each expense before it is paid": it states the reason and adds no rule.

**Reviewed for:** Does every requirement explain its reason in a one-line `rationale` that adds no rule, names no single cause where several exist, and claims no unmeasured comparison? Look at the `rationale` of each requirement.

### Cite the Reasons of Every Requirement

**ID:** `litespec/core/intent#cite-the-reasons-of-every-requirement`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** Each requirement MUST name its reasons in `cites`, including each constraint with `applies: true` that binds it.

**Rationale:** A requirement with no stated reason cannot be rejudged when the world moves. The `cites` list holds the reasons: any of goal, direction, problem, feature, constraint, research, and decision, in no order of precedence. The parent is the feature whose folder holds the requirement, or the product for a product requirement; the cited reasons are the connections explaining why the commitment exists. A requirement citing nothing reports as `requirement-cites-missing`.

An outside rule the team acknowledged but no requirement answers is a rule the product can break unknowingly. Citing each applying constraint from the requirements bound by it ties the rule to the commitments obeying it, and What It Requires lists those commitments back on the constraint. A constraint with `applies: true` that no requirement cites reports as `constraint-unreached` at check level G.

**Checked by:** `litespec model check` reports `requirement-cites-missing` for a requirement naming no reason in `cites`, and `constraint-unreached` for a constraint with `applies: true` that no requirement cites.

### State the Measure on a Counted Requirement

**ID:** `litespec/core/intent#state-the-measure-on-a-counted-requirement`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** A quality requirement whose statement names a number MUST carry `measure`.

**Rationale:** A number nobody defines cannot be checked: the team cannot tell what is counted. The `measure` names what is counted, so a quality statement naming a number without one is unfinished. A number in a functional or policy requirement asks for no `measure`.

A requirement carrying `measure` needs a `check` and a passing run, and a quality statement naming a number with no `measure` reports at check level A.

**Checked by:** `litespec model check` reports `requirement-measure-missing` for a requirement carrying `measure` without `check` or a passing run, and for a quality statement naming a number with no `measure`.

### State Each Quality Requirement as a Scenario

**ID:** `litespec/core/intent#state-each-quality-requirement-as-a-scenario`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** A quality requirement MUST state the moment it holds for in `stimulus` and `environment`, what the product does in `response`, how that is measured in `measure`, and how that is confirmed in `check`, with `target`, `conditions`, `runsOn`, and `evidence` where they apply.

**Rationale:** "Fast", "secure", or "always available" cannot be built to or checked. A quality requirement holds only for a moment that can happen: something arrives, under some condition, and the product answers within a measure. Written that way, it tells the builder what to design for and tells a test or a probe what to look for, and a later change in the moment (a bigger on-sale, a new attack) visibly reopens the target. The four fields follow the quality attribute scenario the architecture literature uses, so a reviewer can compare one target with another.

The `check` names the probe or test confirming the measure, and it is required where the requirement carries `measure`. The `target` names the number aimed for, `conditions` names what holds while measuring, `runsOn` names where the check runs, and `evidence` names what the run leaves behind; each applies where the confirmation needs it.

**Example:** `requirement/approval-reminders.month-end-submission` has `kind: quality`, `stimulus` "Five hundred employees submit expenses in the last hour of the month", `environment` "Normal operation, one application instance down", `response` "Each submission is stored and acknowledged", `measure` "Share of submissions acknowledged within two seconds: at least 99 percent", and `check` "month-end acknowledgement probe".

**Checked by:** `litespec model check` reports `record-invalid` for a quality requirement missing `stimulus`, `environment`, `response`, `measure`, or `check`.

### Restrict Requirement Fulfillment to Behavior

**ID:** `litespec/core/intent#restrict-requirement-fulfillment-to-behavior`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** A functional or policy requirement MUST be fulfilled only from Behavior records.

**Rationale:** Only Behavior keeps functional and policy promises. The records that fulfill are rule, use case, reaction, job, notification, setting, entry point, and screen. An entry point fulfills through a served content entry in its `servedContent` list, and a screen fulfills a requirement about the interface, such as what a chart shows. A flow names the requirements it shows in its steps' `carries`, but it never fulfills: a flow alone never clears an unfulfilled requirement. A quality requirement is fulfilled by a Behavior record, by a technical decision naming it in `basedOn`, by its `check`, or by a test proving it. Behavior points up to the promises it keeps, and each promise traces down to the behavior keeping it.

**Checked by:** `litespec model check` reports `requirement-fulfillment-missing` for a requirement no Behavior record fulfills.

### Lead Each Flow Toward the Record It Exists For

**ID:** `litespec/core/intent#lead-each-flow-toward-the-record-it-exists-for`

**Strength:** required

**Applies to:** artifact: record:flow

**Statement:** A flow MUST lead one actor toward the record it exists for through ordered steps, naming exactly one reason, one goal in `serves` or one problem or constraint in `answers`, and stating each path off that order in the `branches` of the step it leaves.

**Rationale:** A flow with several actors or several reasons is several flows tangled together: no reader can tell where one task ends and the next starts. One actor means a step names another actor only at a handoff; one reason means a flow with several reasons splits into several flows. Both reason fields hold a list of exactly one identifier, because the field grammar gives each name one shape ([decision/0066-name-the-record-a-flow-exists-for](decision/0066-name-the-record-a-flow-exists-for)). The order shows how the steps join toward the reason, so a step fitting nowhere is visible. Each step names in `carries` the requirements it carries, and the use cases, reactions, and jobs serving each step point up with `serves`. Each step holds the paths leaving it in its own `branches`, each stating its condition and where the path continues or ends, and Example in the explanation file shows one concrete occasion. Proposed by [decision/0005-flows-serve-a-goal](decision/0005-flows-serve-a-goal) on the evidence in [research/intent-hierarchy-evidence](research/intent-hierarchy-evidence), [research/cross-layer-trace-evidence](research/cross-layer-trace-evidence), and [research/established-traceability-practice](research/established-traceability-practice), and reworded by [decision/0048-state-the-start-and-branches-of-each-flow](decision/0048-state-the-start-and-branches-of-each-flow). Reworded by [decision/0049-literal-flow-words-concrete-consequences](decision/0049-literal-flow-words-concrete-consequences). Settled by [decision/0066-name-the-record-a-flow-exists-for](decision/0066-name-the-record-a-flow-exists-for). Settled by [decision/0111-name-flow-steps-and-keep-branches-on-them](decision/0111-name-flow-steps-and-keep-branches-on-them).

**Example:** Claimwick held a 21-step flow with five actors, from filing an expense to closing the month. Split by reason, it is four flows: file an expense; approve an expense; reimburse the employee; close the month.

**Checked by:** `litespec model check` reports `record-invalid` for a flow record file missing its `actor`, `summary`, `startedWhen`, `finishedWhen`, or `steps`, for a flow with neither `serves` nor `answers`, and for a flow with both.

### Summarize Every Flow Before Its Steps

**ID:** `litespec/core/intent#summarize-every-flow-before-its-steps`

**Strength:** required

**Applies to:** artifact: record:flow

**Statement:** A flow MUST state its `summary` in up to four sentences before its steps.

**Rationale:** Steps first would teach a cold reader the mechanics before the purpose. The summary states the flow in the actor's terms: who takes the flow, what starts it, what good looks like, and the consequence at stake and for whom, as `litespec/core/writing#write-for-the-reader-who-arrives-cold` states. The book renders it first on the page, so purpose comes before mechanics. `finishedWhen` stays the checkable end state; the summary carries the purpose no field checks. Proposed by [decision/0049-literal-flow-words-concrete-consequences](decision/0049-literal-flow-words-concrete-consequences). Settled by [decision/0068-call-ordered-steps-a-flow](decision/0068-call-ordered-steps-a-flow).

**Example:** The library flow to borrow and return describes a reader who holds a copy, starting at the shelf and ending with the copy back on its shelf, because a reader plans the next visit around the return.

**Checked by:** `litespec model check` reports `record-invalid` for a flow record file missing `summary`.

### Perform Each Flow Step with an Actor

**ID:** `litespec/core/intent#perform-each-flow-step-with-an-actor`

**Strength:** required

**Applies to:** artifact: record:flow

**Statement:** A flow step MUST name at most one performer in `actor`: another actor at a handoff, or the system actor standing for the outside system when no person acts, and a step naming neither is performed by the actor taking the flow.

**Rationale:** Every link points from the lower or later record to the higher or earlier one, so a step names its performer in `actor` instead of naming an external system beside it. A step performed by machinery names the system actor standing for the outside system, and that system's record names the actor in `represents`, which states the reliance on that system as an intentional relationship. Jobs and reactions never stand in `actor`; they serve the step from above like use cases. Settled by [decision/0067-cite-requirements-on-each-flow-step](decision/0067-cite-requirements-on-each-flow-step).

**Example:** In the library flow, the step sending the overdue notice names the text gateway actor in `actor`; the steps the reader takes name no performer.

**Checked by:** `litespec model check` reports `reference-unknown` for a flow step whose `actor` names no written actor, and `record-invalid` for a step naming a system actor no external system represents.

### Keep Behavior Out of Intent

**ID:** `litespec/core/intent#keep-behavior-out-of-intent`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An Intent record MUST NOT describe states, results, codes, transactions, edges, consistency modes, Behavior field names, or code.

**Rationale:** An Intent record naming Behavior detail becomes a second specification, and it drifts from the first with the next Behavior change. Two specifications force every reader to ask which one the code follows. Behavior records own behavior and point up to the requirements and steps they serve, so each fact has one home.

**Reviewed for:** Is every Intent record under `.litespec/intent/` free of states, results, codes, transactions, edges, consistency modes, Behavior field names, and code? Read each Intent record for the detail the Statement names.

### Treat Generated Research as Source Material

**ID:** `litespec/core/intent#treat-generated-research-as-source-material`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST treat records generated outside `docs/` as source material until a research record or a decision states the claim.

**Rationale:** Records generated outside `docs/` mix verified facts, inferences, and recommendations in one confident voice. Treating them as source material with no authority keeps a recommendation from being built as if someone decided it. A research record or a decision restates the claim before anything rests on it.

**Reviewed for:** Does a research record or a decision stand behind every position built from generated records? Compare the change with the research and decisions it cites.

### State Each Exception with Its Fallback

**ID:** `litespec/core/intent#state-each-exception-with-its-fallback`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** A requirement that names an `except` condition MUST name its `fallback` requirement in its own record.

**Rationale:** An exception without a fallback leaves the reader at the exact moment the promise breaks: the product refuses with no stated alternative. The `condition` states in product words when the commitment does not hold, and `fallback` names the requirement covering what happens then. The fallback carries no `except` of its own, so the chain stays one deep and every exception ends in a promise.

**Example:** A reimbursed expense lands in the employee's account, never more than they spent. Except when the payout bank's settlement window has closed for that batch, with `fallback` naming the requirement that has finance repay the employee by other means.

**Checked by:** `litespec model check` reports no `except-fallback-missing` finding.

### Write Flow Steps as Sentences with Ids

**ID:** `litespec/core/intent#write-flow-steps-as-sentences-with-ids`

**Strength:** required

**Applies to:** artifact: record:flow

**Statement:** Each flow step MUST carry its `id` as `step/<flow>.<act>`, its `title` naming its act in Title Case, its product sentence in `statement` naming one act of its performer and no use case, and in `carries` each requirement the step carries.

**Rationale:** A step written as a product sentence reads in flow detail with no Behavior knowledge: it states what a person experiences, not which handler runs. The sentence states one act of its performer and may end with the product's observable response after the act, as in "Finance reimburses the approved expense, and Claimwick pays the amount into the employee's account". How the product computes that response stays in the cited requirement, and a second act of a person is a second step. Numbers, limits, and conditions stay in the cited requirement; the step states the experience only. Proposed by [decision/0048-state-the-start-and-branches-of-each-flow](decision/0048-state-the-start-and-branches-of-each-flow).

The `id` names the step's act as a verb phrase in kebab case, scoped by its flow, as `step/submit-and-reimburse.submit-expense`: use cases, reactions, and jobs point up with `serves` to that id, and the citation reads without opening the flow. The `title` names the same act in Title Case, as `Submit Expense`, and the act is the title's words in kebab case, as `litespec/core/model#name-each-identifier-from-its-title` states. The book, the trace, and `model query` show the title, while the `statement` keeps the full sentence. A step naming its use case drags Behavior into Intent and breaks the flow detail every time Behavior renames. An act that only numbers the step, such as `s3` or `step-2`, says nothing and breaks on every inserted step, so the act names what the performer does.

The performer and the failure text are optional: a step names another actor only at a handoff in `actor`, or the system actor standing for the outside system when no person acts, and carries `failure` where the flow can fail. Reworded by [decision/0049-literal-flow-words-concrete-consequences](decision/0049-literal-flow-words-concrete-consequences). Settled by decision 0067. Settled by [decision/0111-name-flow-steps-and-keep-branches-on-them](decision/0111-name-flow-steps-and-keep-branches-on-them).

**Example:** The step `step/submit-and-reimburse.reimburse-expense` carries `Reimburse Expense` in `title`, states `Finance reimburses the approved expense.` in `statement`, and names no use case; `use-case/spending.reimburse-expense` names that id in `serves`.

**Checked by:** `litespec model check` reports `record-invalid` for a flow step missing `id`, `title`, or `statement`, or naming a use case; `litespec model check` reports `identifier-invalid` for a step id outside the `step/<flow>.<act>` form, for a `<flow>` segment naming another flow, or for a positional act; `litespec model check` reports `identifier-title-invalid` for a step whose act is no word subsequence of its title; `litespec model check` reports `reference-unknown` for a step citation naming no step.

### State When Each Flow Starts

**ID:** `litespec/core/intent#state-when-each-flow-starts`

**Strength:** required

**Applies to:** artifact: record:flow

**Statement:** A flow MUST state the observable state in which it starts in `startedWhen`, in one sentence.

**Rationale:** A flow whose start sits in its summary, its Example, and its first step has three starting points, and a reader cannot tell where the path begins. `startedWhen` gives the start one home beside `finishedWhen`, which states the end, and a check reads it. The detail of one occasion belongs in the Example, not in the field. Proposed by [decision/0048-state-the-start-and-branches-of-each-flow](decision/0048-state-the-start-and-branches-of-each-flow).

**Example:** `startedWhen`: the employee has paid a work cost and has not yet submitted it.

**Checked by:** `litespec model check` reports `record-invalid` for a flow record file missing `startedWhen`.

### Branch a Flow Only Between Its Steps

**ID:** `litespec/core/intent#branch-a-flow-only-between-its-steps`

**Strength:** required

**Applies to:** artifact: record:flow

**Statement:** A flow MUST leave the order of its steps only through the `branches` of the step each path leaves, each branch stating one `when` condition and exactly one of the step it continues at in `then` or the outcome in `ends`.

**Rationale:** A path stated in prose lets an action hide where no check reads it, and a reader cannot see which step the path leaves. A branch on its step names where the path goes, so a check reads each path: `then` names a step of the same flow, and `ends` states the outcome when the flow ends there. One condition per branch keeps each path testable: two conditions joined by "or" are two branches. A branch that does work of its own states that work in `instead`, with its own `carries`, so the alternative reads beside the step it replaces. Proposed by [decision/0048-state-the-start-and-branches-of-each-flow](decision/0048-state-the-start-and-branches-of-each-flow). Settled by [decision/0111-name-flow-steps-and-keep-branches-on-them](decision/0111-name-flow-steps-and-keep-branches-on-them).

**Example:** The decide step carries a branch with `when` the manager rejects the expense and `ends` stating that the expense is rejected and no payout is made; the act of deciding stays the step.

**Checked by:** `litespec model check` reports `record-invalid` for a branch missing `when`, carrying both or neither of `then` and `ends`, joining two conditions with "or" in `when`, or continuing at a step of another flow, and `reference-unknown` for a `then` naming no step.

### Reach Every Flow Step by a Path

**ID:** `litespec/core/intent#reach-every-flow-step-by-a-path`

**Strength:** required

**Applies to:** artifact: record:flow

**Statement:** Every flow step MUST be reached by the step order or by a branch: the first step starts the flow, and each later step follows the step before it or is named by a branch's `then`.

**Rationale:** A step no path reaches is a moment the actor never gets to: an act filed in the flow that no walk through it performs. The step order reaches each step from the one before it, and a branch reaches its `then` step from the step it leaves. A step reached neither way is reported, so the flow holds no act outside its paths. Settled by [decision/0111-name-flow-steps-and-keep-branches-on-them](decision/0111-name-flow-steps-and-keep-branches-on-them).

**Example:** Claimwick's reimburse step follows the decide step in order, so the order reaches it; a step that follows nothing and that no branch names is reported.

**Checked by:** `litespec model check` reports `step-unreached` for a step that is not the first step, that no earlier step falls through to, and that no branch's `then` names.

### Reach Each Finish Condition Through a Step

**ID:** `litespec/core/intent#reach-each-finish-condition-through-a-step`

**Strength:** required

**Applies to:** artifact: record:flow

**Statement:** A flow MUST reach each clause of `finishedWhen` through a step, with each step performable once `startedWhen` and the steps before it hold, and its Example starting from `startedWhen` and performing the steps in order.

**Rationale:** A flow whose end no step reaches states a finish the path never gets to, and a step needing a state no earlier step or start provides cannot be performed. An Example skipping a step, or starting from a state other than `startedWhen`, teaches a path the flow does not hold. Proposed by [decision/0048-state-the-start-and-branches-of-each-flow](decision/0048-state-the-start-and-branches-of-each-flow).

**Example:** The flow to reimburse an expense ends in `finishedWhen` with the employee holding the money, which step 4 reaches; its Example starts at the paid cost and performs the submission, the approval, and the payout in that order.

**Reviewed for:** Does every `finishedWhen` clause end a path the steps walk, with each step performable from the start and the steps before it, and the Example walking them in order? Compare the Example with `startedWhen`, `finishedWhen`, and the steps.

### State Each Flow Blocker as a Step Failure

**ID:** `litespec/core/intent#state-each-flow-blocker-as-a-step-failure`

**Strength:** required

**Applies to:** artifact: record:flow

**Statement:** A condition that stops a flow MUST be the `failure` of the step it blocks, stating what the person sees and can do next with the requirement that holds the rule carried in the step's `carries`, and never stated only in a branch or in the explanation.

**Rationale:** A blocker stated in a branch or in the Example reads as a path the flow takes, and a blocker stated only in prose has no step a reader can find. The `failure` of the step keeps each blocker where the step fails, and the step's `carries` names the requirement holding the rule, so the rule is not restated in the flow.

The `failure` states what the person sees and can do next, never the rule behind it. A branch on the blocked step names where the actor goes after the failure. Proposed by [decision/0048-state-the-start-and-branches-of-each-flow](decision/0048-state-the-start-and-branches-of-each-flow). Settled by decision 0067.

**Example:** The step submitting an expense carries in `failure` that the expense cannot be submitted and the employee can attach the receipt and submit again, and carries in its `carries` the requirement holding the receipt rule.

**Reviewed for:** Does every condition stopping the flow sit as the `failure` of the step it blocks, stating what the person sees and can do next, with the requirement holding the rule carried in the step's `carries`? Read each branch and each Example sentence naming a refusal or a wait.

### State When Each Flow Finishes

**ID:** `litespec/core/intent#state-when-each-flow-finishes`

**Strength:** required

**Applies to:** artifact: record:flow

**Statement:** A flow MUST state its observable end in `finishedWhen`.

**Rationale:** A flow without a stated end can stop anywhere: the reader cannot tell a finished flow from an abandoned one, and serving records cannot tell done from dropped. `finishedWhen` states the observable end in one sentence, in product words. Reworded by [decision/0049-literal-flow-words-concrete-consequences](decision/0049-literal-flow-words-concrete-consequences).

**Example:** `finishedWhen`: the employee holds the reimbursement.

**Checked by:** `litespec model check` reports `record-invalid` for a flow record file missing `finishedWhen`.

### Treat Tenant-Set Values as Data

**ID:** `litespec/core/intent#treat-tenant-set-values-as-data`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** A consumer MUST treat a `setBy: tenant` value as tenant data.

**Rationale:** Tenants choose their own values within the ranges Intent sets: a tenant holds its receipt threshold at EUR 25 by default within the EUR 0 to EUR 500 range. The requirement sets the range and the default, and each tenant's choice inside it is data. Treating tenant values as product values would bind every tenant to one tenant's choice.

**Example:** The tenant sets a receipt threshold of EUR 50; the requirement covering threshold ranges already sets the range, so the threshold itself is data.

**Reviewed for:** Is every `setBy: tenant` value stored as tenant data? Look at each tenant-set value beside the requirement setting its range.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/core/intent#lead-each-flow-toward-the-record-it-exists-for`, `litespec/core/intent#write-flow-steps-as-sentences-with-ids`, `litespec/core/intent#state-when-each-flow-starts`, `litespec/core/intent#state-when-each-flow-finishes`, `litespec/core/intent#branch-a-flow-only-between-its-steps`, `litespec/core/intent#reach-every-flow-step-by-a-path`, `litespec/core/intent#identify-every-requirement`, `litespec/core/intent#state-each-exception-with-its-fallback`, and `litespec/core/intent#treat-tenant-set-values-as-data`.

The book shows flow `flow/submit-and-reimburse` as record detail. No field name, YAML, or Behavior page appears.

```text
Submit and reimburse

An employee submits an expense and is reimbursed after approval.

Started when: the employee has paid a work cost and has not yet submitted it.

Finished when: the employee holds the reimbursement.

1. Submit Expense
   The employee submits the expense with its amount and currency.
2. Attach Receipt
   The employee attaches the receipt to the expense.
3. Decide Expense
   The manager approves or rejects the expense within five business days.
   When the manager rejects the expense, the flow ends with no payout.
4. Reimburse Expense
   Finance reimburses the approved expense.

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
