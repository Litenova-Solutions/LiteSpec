# Intent Layer

**Stack:** any

**Concern:** records

## Intent

Intent is why the product exists and what it promises: the product, actors, goals, visions, problems, features with their requirements, journeys, constraints, research, decisions, and terms. This page states where Intent lives, what shape each record takes, and what Intent never holds, before Behavior states how the system keeps the promises. The [record language page](language.md) states how each field, value and record name is written.

## Concepts

### The Questions Intent Answers

| Question | Record | Write It When | Skip It When | Familiar As |
|:---|:---|:---|:---|:---|
| What is the product and for whom? | product | Always, once | Never | Product brief |
| Who touches the product? | actor | A person or organization appears in a problem, a journey or an access position | The "actor" is a role or a system; roles are Behavior and systems stand alone | Stakeholder |
| What software outcome do we want? | goal | You can name what you count | The system controls it directly: that is a quality requirement | Product goal |
| Where is the product heading beyond what it promises now? | vision | A choice made now keeps a later destination open | You can name the pain and the offer: write the problem and the feature | Product vision |
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

Intent reads problem, then feature, then requirement. A problem names one outside pain with its evidence; each feature sits in the folder of the one problem it answers and carries the offer; each requirement sits in its own folder inside its feature's folder and states one commitment Behavior fulfills. The [model page](model.md) defines the record types and links. Each requirement keeps one authoritative file, and the book brings a feature's requirements together on the feature's page with the decisions behind them.

Intent is not Behavior. A rule or use case states what the system does today and matches the code. A requirement states what the product decided and why, and it can describe a position the code does not carry yet. The two connect in one direction: Behavior records name the requirements they fulfill and the steps they serve, and an Intent record never describes Behavior, except a decision naming the records it settles in `decides` and an actor naming its roles in `holds`.

### The Software Test

Every record states something the software does, holds, refuses at runtime or runs on, and translates into code, results, tests or infrastructure. The structure stays; the content test changes:

| Type | Belongs when it is | Out when it is |
|:---|:---|:---|
| Goal | A software outcome: "Nights run without oversell or unreconciled money" | A business outcome: retention, revenue, market share |
| Vision | Where the software heads: "Contractors submit expenses as employees do" | A roadmap entry with a date, a priority or a release |
| Problem | A user pain the software answers | A sales or market problem |
| Feature | A capability the software has | A marketing tactic, a launch plan, or a refusal |
| Requirement | A commitment Behavior fulfills | (always software when it reaches Behavior) |
| Constraint | Law, a provider or a limit that binds the software | A company duty with no software effect, such as incorporation |
| Research | Evidence a software record cites | Market sizing, viability, acquisition, pricing strategy |

A record that states company operation fails the test, even when its folder is right. Reachability proves the rest: a record that reaches no Behavior is either unfinished work for the backlog or not software.

### The Folder Is the Parent

Each record sits in the folder of its parent, and the folder names that parent:

```text
.litespec/intent/
  product.yaml
  goals/  visions/  constraints/  decisions/  research/  actors/  roles/  journeys/
  problems/
    payouts-wait/
      payouts-wait.yaml
      features/
        payout/
          payout.yaml          the feature
          payout.md            its explanation, optional
          requirements/
            paid-once/         requirement
              paid-once.yaml
              paid-once.md     its explanation, optional
```

Product level holds the product, visions, goals, constraints, decisions, research, actors, roles and journeys. Each problem's folder holds the problem and the features that answer it, and each feature's folder holds its requirements. Journeys cross modules through their steps. Every folder is one of a closed set per layer; a record of a type in the wrong folder and a record type outside its allowed levels are blocking findings.

Modules are not declared here. A module is a code boundary, so it is declared in Behavior, and [Modules as Product Areas](behavior.md#modules-as-product-areas) states how one module name runs through Behavior and code.

### Identifiers and Placement

The identifier stays as it is: `problem/organizers-wait-for-ticket-money`, unique in the product, and what every link and citation uses. Moving a feature to another problem moves its folder and changes no citation, because the identifier names no problem.

A record's placement shows as its folder path and its `module`. The book and the CLI show the identifier with that placement, so a reader can tell where the record sits without a second identifier.

### How Intent Reaches Behavior

The [model page](model.md) states the authority order that settles conflicts between records. Goals, visions, problems, features, journeys, actors, research, and prose carry no authority, and decisions win between peers.

A decision wins its choice, and a feature states it as requirements. Behavior records name the requirements they fulfill and the journey steps they serve, and the code carries what happens today. When Behavior contradicts a requirement, either the code carries a defect or the product changed. A changed product means a new decision before any page or code moves.

A fact learned from the world enters Intent as research or a constraint, cited and dated. Where Intent is silent, working code is evidence for a missing record, not a defect to delete.

### Requirements with Except and Fallback

A requirement carries `kind` (functional, quality, or policy), `statement`, `rationale`, and `measure` where its statement names a number. `setBy` names who sets its value: `product`, `operator`, or `tenant`.

An `except` entry states in product words when the commitment does not hold, and `fallback` names the requirement that covers what happens then. The fallback carries no `except` of its own: exceptions stay one deep, so every exception ends in a promise.

### Journeys as Product Sentences

A journey carries `actor`, `serves`, `summary`, `startedWhen`, `finishedWhen`, ordered `steps`, optional `branches`, and `cites`. `serves` names the goal record. `summary` states in up to four sentences who takes the journey, what starts it, what good looks like, and the consequence at stake and for whom, and the book renders it first on the page. `startedWhen` states the observable state in which the journey starts in one sentence, as `finishedWhen` states the end in one sentence. Each step carries its `id` (`s1`, `s2`, and so on) and its product sentence in `statement`, names the actor taking the journey only at a handoff in `actor`, or names the system record performing it in `system` when no person acts, and carries its failure text in `failure` where the journey can fail. A step carries exactly one of `actor` and `system`, or neither when the actor taking the journey performs it. `branches` states each path that leaves the order of the steps: the step it leaves, its condition and the step it continues at, or `end`. A step id reads `journey/<name>#s1`.

Steps name no use case. Use cases, reactions, and jobs point up with `serves`, naming the steps they serve. The book shows each journey as record detail: `startedWhen`, the steps as sentences with `failure`, each branch as a sentence after the steps, `finishedWhen`, the requirements it cites with their statements and their `except` and fallback inline, the serving use case goals, and the Then clauses of their scenarios as context. Field names never render literally in journey detail.

A journey leads one actor toward one goal. A step names another actor only at a handoff, and a journey with several goals splits into several journeys.

### Staff Journeys

Journeys are not only employee journeys. A staff actor takes a staff journey, such as finance exporting the approved batch, through the same shape of serves, `startedWhen`, `finishedWhen`, steps, and `branches`. A scheduled promise is a requirement that a job fulfills, not a journey. Staff steps read as product sentences too.

### Visions

A vision states where the product is heading beyond what it promises now: a destination the owner wants the product to reach, such as contractors submitting expenses the way employees do. It carries `summary` and `statement`, sits at product level under `.litespec/intent/visions/`, and holds no date, priority or status; the order and timing of work stay on the backlog, outside the records.

A vision exists so a choice made now can name the future it keeps open. A decision that shapes a record or a model so a vision stays reachable names the vision in `basedOn`, and a requirement that takes a step toward it names it in `cites`. Nothing else links to a vision, and a vision links to nothing.

A vision reaches no Behavior, by design. It is not a promise, so no reach check reads it and no planned-Intent finding reports it. An agent builds nothing from a vision: when the destination becomes concrete, someone writes the problem, the feature and its requirements, the requirements cite the vision, and the usual reach rules apply to them.
### What Intent Never Contains

Intent never holds Behavior detail. `litespec/core/intent#keep-behavior-out-of-intent` names what stays out.

## Standards

### Keep Intent in Its Homes

**ID:** `litespec/core/intent#keep-intent-in-its-homes`

**Strength:** required

**Applies to:** artifact: record:product, record:goal, record:vision, record:problem, record:constraint, record:decision, record:research, record:actor, record:role, record:journey, record:feature, record:requirement

**Statement:** A consumer MUST keep its product record at `.litespec/intent/product.yaml`, each vision, product-level goal, problem, constraint, decision, research record, actor and journey in its own folder under `.litespec/intent/`, each feature in the `features/` folder of the problem it answers, `.litespec/intent/problems/<problem>/features/`, and each requirement in its own folder in its feature's `requirements/` folder.

**Rationale:** Intent split across notes and books gives no single answer to the question an agent asks before building. One home per kind, with the product record, visions, goals, problems, constraints, decisions, research, actors and journeys at product level, gives every reader one place to look. A second home for any kind starts a second truth about the product. The folder is the parent: product level holds the product-wide kinds, a problem's folder holds the features that answer it, and a feature's folder holds its requirements. A module is declared in Behavior, not here, because a module is a code boundary and Intent is organized by reason. Proposed by [decision/0050-put-each-feature-inside-the-problem-it-answers](decision/0050-put-each-feature-inside-the-problem-it-answers) and [decision/0051-declare-modules-only-in-behavior](decision/0051-declare-modules-only-in-behavior).

**Checked by:** `litespec model check` reports `record-placement-invalid` for a record outside its home, a record type outside its allowed levels, and any file under `intent/modules/`.

### State One Product

**ID:** `litespec/core/intent#state-one-product`

**Strength:** required

**Applies to:** artifact: record:product

**Statement:** A consumer MUST state exactly one product record at `.litespec/intent/product.yaml`, with `boundary`, `addresses`, and `summary`.

**Rationale:** One product record gives every reader one answer to what the product is and who it serves. The `boundary` and `addresses` focus every later choice. Two product statements let each plan cite the one it prefers. `summary` states what the product is in one place.

**Checked by:** `litespec model check` reports no `product-count-invalid` finding, and `litespec model check` reports `record-invalid` for product fields outside the record schema.

### Keep Company Operation Out of Intent

**ID:** `litespec/core/intent#keep-company-operation-out-of-intent`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An Intent or Behavior record MUST state only behavior the application performs, data it holds, a refusal it raises at runtime, infrastructure it runs on, or a limit it enforces.

**Rationale:** LiteSpec specifies the product, not the company. Prices as an offer, markets, hiring, sales actions, launch calendars, company goals and founder concerns can be checked only against a bank account, a diary or a contract, so they sit outside the records a reader reviews as the product. A company fact that binds the product still enters: as a constraint when an authority or a limit binds the application, and as dated research when a choice rests on it. [The Software Test](#the-software-test) names what belongs for each record type: a goal states a software outcome, a problem states a user pain the software answers, a feature states a capability the software has, and a module names a product area, never a company function. Plan entitlements the software enforces stay, as settings and permissions; the price list as a company offer leaves.

**Example:** A library holds its lending limit as a setting the application enforces, so the limit stays as Behavior. The membership price the library charges as an offer is a company fact: it enters only as dated research behind the decision that sets the limit.

**Reviewed for:** Intent and Behavior records stating company operation beside the constraints and research that carry the binding facts.

### Reach Every Intent Record with Behavior

**ID:** `litespec/core/intent#reach-every-intent-record-with-behavior`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each Intent record other than a vision MUST reach Behavior through its stated path.

**Rationale:** The folder fixes where a record sits. Reachability fixes that it leads to software. A vision is the one exception, because it states where the product heads and promises nothing yet. A goal reaches a journey or a quality requirement. A problem reaches a feature or a requirement that addresses it. A feature or a requirement reaches a use case, a journey or a quality scenario in Behavior. A constraint is answered by a requirement. Research is cited by a record that reaches Behavior. A decision decides a record that reaches Behavior. A record that reaches no Behavior is either unfinished work for the backlog or not software.

**Checked by:** `litespec model check` reports `intent-unreached` for a record that reaches no Behavior.

### State Each Goal with Its Measure

**ID:** `litespec/core/intent#state-each-goal-with-its-measure`

**Strength:** required

**Applies to:** artifact: record:goal

**Statement:** A goal MUST state its `summary`, `statement`, `measure`, and `target` in fields.

**Rationale:** A goal without a measure is a wish no check can confirm: the team cannot tell whether the product serves it. The `summary` states what the goal is in one paragraph, the `statement` names the outcome and the consequence at stake, and `measure` and `target` name what is counted and the number aimed for. A goal carries no authority: it explains why features exist and never decides a conflict. Reworded by [decision/0049-use-literal-journey-words-and-concrete-consequences](decision/0049-use-literal-journey-words-and-concrete-consequences).

**Checked by:** `litespec model check` reports `record-invalid` for a goal record file missing a required field.

### Move Every Goal with a Journey or a Quality Requirement

**ID:** `litespec/core/intent#move-every-goal-with-a-journey-or-a-quality-requirement`

**Strength:** required

**Applies to:** artifact: record:goal

**Statement:** A goal MUST be moved by a journey serving it or by a quality requirement citing it.

**Rationale:** A goal no journey serves and no quality requirement confirms names an outcome the product never touches: a company hope filed as a product goal. A journey serving the goal shows the journey that moves the counted outcome, and a quality requirement citing it states the measure that confirms the move. An outcome the system controls directly is a quality requirement, and an outcome only the company controls is no goal at all. Reworded by [decision/0049-use-literal-journey-words-and-concrete-consequences](decision/0049-use-literal-journey-words-and-concrete-consequences).

**Example:** The library goal of faster pickup is moved by the borrow-and-return journey serving it; a goal of higher membership sales is moved by no journey and is no product goal.

**Checked by:** `litespec model check` reports `goal-mover-missing` for a goal moved by no journey and no quality requirement.

### State Each Vision in Its Fields

**ID:** `litespec/core/intent#state-each-vision-in-its-fields`

**Strength:** required

**Applies to:** artifact: record:vision

**Statement:** A vision MUST state its `summary` and `statement` in fields and sit at product level under `.litespec/intent/visions/`.

**Rationale:** A vision is read cold, often long after it was written, by someone deciding whether a choice still serves it. The `statement` names the destination as a fact about the product once it is there, and the `summary` says who wants it and why in one paragraph. A vision belongs to the whole product, so it sits at product level: a module-level vision would hide a destination that often crosses modules, such as contractors joining the employee flow. Settled by [decision/0041-record-product-vision](decision/0041-record-product-vision).

**Example:** Claimwick's `vision/contractor-expenses` states "Contractors submit and track their work expenses in Claimwick as employees do." in `statement`, and its summary names the contractors who invoice their costs by hand today.

**Checked by:** `litespec model check` reports `record-invalid` for a vision record file missing `summary` or `statement`, and `record-placement-invalid` for a vision outside `intent/visions/`.

### Keep Dates and Priorities Out of Visions

**ID:** `litespec/core/intent#keep-dates-and-priorities-out-of-visions`

**Strength:** required

**Applies to:** artifact: record:vision

**Statement:** A vision MUST name no date, release, priority, status, or order among visions.

**Rationale:** A vision states where the product heads, which changes rarely. When and in which order the work happens is planning, which changes every week and belongs to the backlog outside the records, as running the company does. A date or a priority written into a vision goes stale without any check noticing, and a reader then takes a forgotten plan for a commitment.

**Example:** Claimwick's contractor vision names the destination only. That the team may start on it after the approval reminders ship is a backlog fact, kept on the board.

**Reviewed for:** Which visions name a date, a release, a priority, a status, or an order among visions? Look at each vision's fields and explanation file.

### Build Nothing from a Vision

**ID:** `litespec/core/intent#build-nothing-from-a-vision`

**Strength:** required

**Applies to:** artifact: record:vision

**Statement:** An agent MUST build no record below Intent, code, or test from a vision until a requirement that cites the vision exists.

**Rationale:** The specification is closed: an agent builds what Intent promises and nothing more. A vision promises nothing, so building from it adds behavior no requirement asks for. When the destination becomes concrete, the owner's problem, feature and requirements state the promise, cite the vision, and the agent builds from them. Until then a vision may shape a decision that keeps it open, never the code itself.

**Example:** An agent asked to add the approval reminders builds them from their requirements, and adds no contractor fields because Claimwick holds a contractor vision.

**Reviewed for:** Which Behavior records, code, or tests serve only a vision, with no requirement citing it? Look at the change beside the requirements that cite each vision.

### Cite the Vision a Decision Keeps Open

**ID:** `litespec/core/intent#cite-the-vision-a-decision-keeps-open`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** A decision chosen so that a vision stays reachable MUST name that vision in `basedOn`.

**Rationale:** A choice made for a later destination looks like over-engineering to a reader who cannot see the destination. Naming the vision in `basedOn` gives the reason a record identifier: the reader follows it to the vision, the book shows the decision under it, and a later change to the vision shows which choices to revisit. A reason kept only in prose is lost the first time the prose is rewritten.

**Example:** A Claimwick decision to record who submitted each expense as a person rather than as an employee names `vision/contractor-expenses` in `basedOn`, so a reader sees why the expense holds no employee number.

**Reviewed for:** Which decisions made to keep a later destination open name no vision in `basedOn`? Look at each decision's options and consequences beside the visions.
### Admit Only Outside Pains as Problems

**ID:** `litespec/core/intent#admit-only-outside-pains-as-problems`

**Strength:** required

**Applies to:** artifact: record:problem

**Statement:** A problem MUST name in `affects` a primary sufferer that is a person or organization actor outside the team building the product, and state the pain in `summary` and `statement` in fields.

**Rationale:** A problem is a pain someone outside the team has today. The first `affects` entry is the primary sufferer, so who has the pain is the first fact a reader meets. `statement` states the sufferer, the condition and the consequence in one sentence, with the product never as its subject. `summary` states what the pain is in one paragraph: when it happens and what the person was trying to do, and never stands as a bare pointer to another page. What is no problem leaves for its own record: a solution in disguise becomes a feature, a worry about what might happen becomes a decision or a goal, an internal limit becomes a limit on the plan written outside Intent, a market fact becomes research, and a legal duty becomes a constraint.

**Example:** In the library product, readers losing track of due dates is a problem whose primary sufferer is the reader. The library having too few staff for the summer is an internal limit and no problem.

**Checked by:** `litespec model check` reports `problem-sufferer-invalid` for a problem whose primary sufferer is no person or organization outside the team; `litespec model check` reports `record-invalid` for a problem record file missing `affects`, `summary` or `statement`.

### Open Each Problem with Its Gap and Consequence

**ID:** `litespec/core/intent#open-each-problem-with-its-gap-and-consequence`

**Strength:** required

**Applies to:** artifact: record:problem

**Statement:** A problem explanation file MUST open with a paragraph that states what the sufferer cannot reliably do, the gap that causes it, and what the sufferer loses or risks as a result.

**Rationale:** The opening paragraph may be all a cold reader reads, so it carries the reason the problem matters. An opening that starts with the workaround tells the reader that a task is done by hand, never why the task is needed, and the reader supplies a cause of their own. The form "the sufferer cannot reliably do the task because of the gap, so they lose or risk the consequence" is a check on the paragraph, not a sentence to copy. The workaround follows in its own section.

**Example:** The Claimwick problem explanation for waiting expenses opens: employees cannot get an expense paid back quickly, because nothing shows the approver how long an expense has waited, so employees carry the amount from their own account for weeks.

**Reviewed for:** Which problem explanations open with the workaround, a feature or a theme instead of what fails, the gap behind it and its consequence? Look at the opening paragraph of each problem explanation file.

### State Each Failure Case on Its Own

**ID:** `litespec/core/intent#state-each-failure-case-on-its-own`

**Strength:** required

**Applies to:** artifact: record:problem

**Statement:** A problem explanation file MUST list under Failure Cases each failure that can happen independently as its own entry, naming what is missing or wrong, how the sufferer finds it, and what follows when they do not.

**Rationale:** Two failures joined in one sentence read as one, and the reader cannot see that each needs its own check and its own resolution. The direction matters: when two records should agree, an entry missing from the first and an entry missing from the second are found by different checks, and a solution that runs only one check leaves the other failure in place. Naming how the sufferer finds each failure and what follows when they miss it shows what the failure costs and what a solution has to catch.

**Example:** The library problem of readers losing due dates lists two failure cases. A loan missing from the reader's paper list goes overdue unnoticed, and the reader learns of it from the late fee. A renewed loan keeps its old date on the list, so the reader returns the copy early and loses the renewal.

**Reviewed for:** Which problem explanations join two independent failures in one entry, or name a failure without how it is found and what follows when it is missed? Look at the Failure Cases section of each problem explanation file.

### State the Cost and Workaround of Every Problem

**ID:** `litespec/core/intent#state-the-cost-and-workaround-of-every-problem`

**Strength:** required

**Applies to:** artifact: record:problem

**Statement:** A problem MUST state in its explanation file what the sufferer does instead today with the burden that workaround carries, and what the pain costs them, with time, money and risk each in the sufferer's own units or the word `unknown`.

**Rationale:** A pain with no cost and no workaround is a slogan: no reader can tell a daily loss from a passing annoyance, and no feature can be judged against the price of standing still. The workaround shows the pain is real enough to work around. Work done by hand is no burden on its own, so the workaround names the burden: the time it takes, what the sufferer repeats or has to remember, and what it still misses. Time, money and risk stay apart because each is judged in its own units, and a total that mixes them hides which one matters. Where nobody has priced the cost or named the workaround, `unknown` states the gap honestly.

**Example:** A reader who loses track of due dates pays late fees, an amount nobody has measured. The reader keeps a paper list of due dates, updates it at every loan and renewal, and cannot check it when the list stays at home.

**Reviewed for:** Which problem explanations name a workaround without its burden, or mix time, money and risk in one cost? Look at Current Workaround and Cost in each problem explanation file.

### Claim No More Than the Evidence Shows

**ID:** `litespec/core/intent#claim-no-more-than-the-evidence-shows`

**Strength:** required

**Applies to:** artifact: record:problem

**Statement:** A problem explanation file MUST state each claim about who suffers, how often and at what cost no wider than its evidence shows, naming an estimate as an estimate and a possible loss as possible.

**Rationale:** One sufferer's estimate written as a fact about every sufferer reads as a measurement, and a feature gets sized for a pain nobody counted. A possible loss written as an observed one makes the problem look proven when nobody has seen the loss happen. A narrow claim keeps what is known apart from what is assumed, so a reader can judge how much the problem weighs and which evidence would settle it.

**Example:** The library problem writes "One reader at the desk estimates a late fee each month; nobody has measured it", not "Readers pay a late fee each month".

**Reviewed for:** Which problem claims state an estimate as a measurement, a possible loss as an observed one, or one sufferer's report as true of every sufferer? Look at Affected Users, Cost and Evidence in each problem explanation file, beside its evidence entries.

### Grade Problem Evidence by Kind

**ID:** `litespec/core/intent#grade-problem-evidence-by-kind`

**Strength:** required

**Applies to:** artifact: record:problem

**Statement:** A problem MUST list each evidence entry with its `claim`, its `source` as a record id or a URL, its `readOn` date, and its `kind` as observed, reported or assumed, and carry at least one observed or reported entry.

**Rationale:** Evidence states how anyone knows the pain is real: observed means a sufferer was watched, reported means a sufferer said it, and assumed means nobody outside the team has confirmed it. The strength of a problem is the strongest kind present, computed by the builder and never written, so it cannot drift from the entries. A problem resting on assumption alone, or on no entry, is unfinished work for the backlog until a sufferer is watched or heard.

**Example:** The library problem of readers losing due dates carries a reported entry from the desk log, read on its date, so its computed strength is reported.

**Checked by:** `litespec model check` reports `problem-evidence-missing` for a problem with only assumed evidence or none. The builder writes `evidenceStrength` on each problem in the model.

### Keep Feasibility Out of Problem Evidence

**ID:** `litespec/core/intent#keep-feasibility-out-of-problem-evidence`

**Strength:** required

**Applies to:** artifact: record:problem

**Statement:** A problem evidence entry MUST show that the sufferer has the pain, not only that a solution can be built.

**Rationale:** A file format, an API, an integration or another product shows that a solution is possible, and says nothing about whether anyone loses time or money today. Feasibility kept in the evidence makes a problem look proven by the existence of its answer. It belongs in research that a feature cites. A request for a feature is evidence of the requested solution, and it supports the pain only for what the requester reports about their own work.

**Example:** The Claimwick problem of waiting expenses cites employees reporting a three-week wait. A payment provider page showing same-day transfers is feasibility, and it belongs in research that a feature cites.

**Reviewed for:** Which problem evidence entries show that a format, an API, an integration or another product exists, rather than that sufferers have the pain? Look at each problem's evidence entries and the sources they cite.

### Name No Solution in a Problem Statement

**ID:** `litespec/core/intent#name-no-solution-in-a-problem-statement`

**Strength:** required

**Applies to:** artifact: record:problem

**Statement:** A problem `statement` MUST name no product, feature or solution.

**Rationale:** A statement naming a solution files a missing feature as a pain: the reader accepts the answer before weighing the pain, and every alternative answer reads as off-topic. A statement naming the sufferer, the condition and the consequence leaves the answer to the feature that solves it.

**Example:** "Readers lose track of due dates" names a pain. "Readers need a reminder dashboard" names a solution and is reported.

**Reviewed for:** Which problem statements name a product, a feature, or a solution? Look at the problem statements beside their sufferers.

### Answer One Problem with Each Feature

**ID:** `litespec/core/intent#answer-one-problem-with-each-feature`

**Strength:** required

**Applies to:** artifact: record:feature

**Statement:** A feature MUST sit in the features folder of the one problem it answers, `intent/problems/<problem>/features/`, and an offer that answers two pains is two features.

**Rationale:** The folder names the problem, so a reader of a problem sees every answer to it beside the problem. A feature that answered two problems would be filed under two pains, and each pain would be written twice. Two pains that one offer answers are one problem. Proposed by [decision/0050-put-each-feature-inside-the-problem-it-answers](decision/0050-put-each-feature-inside-the-problem-it-answers).

**Example:** The approval feature and the approval reminders feature both answer the expense wait problem, so both sit in `problems/expenses-wait-weeks/features/`.

**Checked by:** `litespec model check` reports `record-placement-invalid` for a feature outside a problem's `features` folder, and `record-parent-missing` for a feature whose problem folder holds no problem record.

### Explain Every Problem in Its Fixed Sections

**ID:** `litespec/core/intent#explain-every-problem-in-its-fixed-sections`

**Strength:** required

**Applies to:** artifact: record:problem

**Statement:** A problem explanation file MUST carry Affected Users, Failure Cases, Current Workaround, Cost, Evidence, and Resolution Criteria, in that order, with an optional Example after Failure Cases.

**Rationale:** The fields state who suffers and the pain in one sentence; the explanation carries the circumstances, each failure, one concrete case where the failures are hard to follow, the workaround in use, the cost, the evidence behind each claim, and the outcomes that would end each failure. A fixed order means a reader who learned one problem page finds each part on every problem page. A problem page is complete when a reader who arrives cold can understand the problem, judge its evidence and propose a different solution without asking its author. Settled by [decision/0044-state-problems-by-failure-case](../intent/decisions/0044-state-problems-by-failure-case/0044-state-problems-by-failure-case.md), which gives each problem its failure cases and one resolution criterion for each.

**Example:** The library problem on due dates lists its two failure cases, shows one reader at the desk with three overdue copies in Example, and closes with Resolution Criteria: a reader knows the due date of every copy they hold without a paper list, and a renewed copy shows its new date.

**Reviewed for:** Which problem explanation files miss a section or reorder the list? Look at the H2 sections of each problem explanation file.

### Resolve Every Failure Case with an Outcome

**ID:** `litespec/core/intent#resolve-every-failure-case-with-an-outcome`

**Strength:** required

**Applies to:** artifact: record:problem

**Statement:** A problem explanation file MUST state under Resolution Criteria, for each failure case, an observable outcome for the sufferer that shows the failure has ended, naming no feature or implementation.

**Rationale:** Criteria that cover one failure let a solution end half the problem and look finished. An outcome leaves the solution open, so a reader can propose a different one and judge it against the same criteria. The feature that delivers the outcome sits in the folder of the problem it answers, so the book shows which feature answers it without the problem naming the feature. An implementation that is already required is a constraint, written as its own record.

**Example:** The library problem of readers losing due dates states two criteria, one for each failure case: a reader knows the due date of every copy they hold without a paper list, and a renewed copy shows its new date wherever the reader looks.

**Reviewed for:** Which failure cases have no resolution criterion, and which criteria name a feature, a format or a screen instead of an outcome? Look at Failure Cases beside Resolution Criteria in each problem explanation file.

### State Each Problem with Its Evidence

**ID:** `litespec/core/intent#state-each-problem-with-its-evidence`

**Strength:** required

**Applies to:** artifact: record:problem

**Statement:** A problem MUST state its `summary` and `statement` in fields, with its evidence graded under [Grade Problem Evidence by Kind](#grade-problem-evidence-by-kind).

**Rationale:** A problem that names what its people face points to the pain its solution lifts. `summary` describes the pain in the people's own terms, and the graded evidence shows it is real. Problems without sufferers attract solutions nobody needs. The `affects` link names the actors who face it, so each pain traces to the people a solution must serve.

**Checked by:** `litespec model check` reports `record-invalid` for a problem record file missing its `summary` or `statement`.

### State Each Feature with Its Offering

**ID:** `litespec/core/intent#state-each-feature-with-its-offering`

**Strength:** required

**Applies to:** artifact: record:feature

**Statement:** A feature MUST state its `offering` and name the goals it moves in `advances`.

**Rationale:** A feature names the goals it moves in `advances`, so each offer traces to an outcome. The problem it answers is the folder it sits in, as [decision/0050-put-each-feature-inside-the-problem-it-answers](decision/0050-put-each-feature-inside-the-problem-it-answers) states, so the offer traces to a pain without a second link. `offering` states what is in and what is out, so the scope of the offer is written where the links are. Proposed by [decision/0004-features-advance-goals](decision/0004-features-advance-goals) on the evidence in [research/intent-hierarchy-evidence](research/intent-hierarchy-evidence), [research/cross-layer-trace-evidence](research/cross-layer-trace-evidence), and [research/established-traceability-practice](research/established-traceability-practice).

**Reviewed for:** Which features miss their offering or their moved goals? Look at each feature record beside its goals.

### State the Boundaries of Every Feature

**ID:** `litespec/core/intent#state-the-boundaries-of-every-feature`

**Strength:** required

**Applies to:** artifact: record:feature

**Statement:** A feature explanation file MUST state under Boundaries what the feature leaves to other features, which features it depends on, and each open question its owner has not decided.

**Rationale:** A feature that answers part of a problem reads as answering all of it unless its edges are written down, and a builder fills the gap with behavior nobody asked for. Naming the features it leaves work to and the features it depends on keeps each behavior in one feature. An open question written beside the feature reaches the owner as a question instead of reaching the code as a guess. Settled by [decision/0046-write-features-for-builders-and-testers](../intent/decisions/0046-write-features-for-builders-and-testers/0046-write-features-for-builders-and-testers.md), which writes each feature for the people who build and test it.

**Example:** The Claimwick approval feature leaves reminders for late decisions to the approval reminders feature, depends on the receipt threshold decision, and names one open question: whether a manager on leave hands decisions to a deputy.

**Reviewed for:** Which feature explanations leave out what the feature leaves to other features, the features it depends on, or its open questions? Look at the Boundaries section of each feature explanation file.

### State Success Criteria the Feature Controls

**ID:** `litespec/core/intent#state-success-criteria-the-feature-controls`

**Strength:** required

**Applies to:** artifact: record:feature

**Statement:** A feature explanation file MUST state under Success Criteria only outcomes that the feature brings about on its own and a tester can check.

**Rationale:** A criterion the feature cannot guarantee sets a test nobody can pass: importing a bank statement does not make the book balance equal the statement balance, because an entry can be missing or wrong and a later entry moves the balance again. A criterion the feature controls, such as showing both balances for the same date, can be built and checked. An outcome that depends on people or on other features belongs to the goal the feature advances.

**Example:** The library feature for due dates states that a reader sees the due date of every copy they hold, not that no reader pays a late fee, which depends on the reader returning the copy in time.

**Reviewed for:** Which success criteria promise an outcome the feature cannot bring about on its own, or one no tester can check? Look at the Success Criteria section of each feature explanation file.

### Add No Behavior in an Explanation File

**ID:** `litespec/core/intent#add-no-behavior-in-an-explanation-file`

**Strength:** required

**Applies to:** artifact: record:product, record:goal, record:vision, record:problem, record:feature, record:requirement, record:constraint, record:decision, record:research, record:journey, record:actor, record:role

**Statement:** An explanation file of an Intent record MUST describe only behavior that the record's own fields and the records it cites state.

**Rationale:** A requirement statement is the one authoritative form of a commitment. A rule that appears only in prose, such as differences above 10 EUR showing in red, is a second requirement that no record states: no Behavior fulfills it, no check reaches it, and it drifts from the records. The explanation clarifies the requirements with context and an example; a new behavior becomes a requirement or leaves the prose. Proposed by [decision/0054-write-each-shared-record-rule-once](decision/0054-write-each-shared-record-rule-once), which widens this rule from features and requirements to every Intent explanation file.

**Example:** A library feature explanation that says renewed copies show their new date in bold states a behavior no requirement holds, so the sentence becomes a requirement or is removed.

**Reviewed for:** Which behaviors in an Intent explanation file appear in neither its record's fields nor the records it cites? Compare each explanation file with its record's fields and the records it cites.

### Split Records Whose Parts Change Separately

**ID:** `litespec/core/intent#split-records-whose-parts-change-separately`

**Strength:** required

**Applies to:** artifact: record:constraint, record:research

**Statement:** A constraint or research record MUST hold only claims that share one question or source, one scope and one rate of change, so a claim verified or changed separately sits in its own record.

**Rationale:** A record that bundles a rule from one source with a rule from another cannot be rechecked in one pass, and a change to one rule reopens the whole record. Splitting by the same test keeps each answer one reader can follow. The split stops where the parts of one answer no longer change apart, because an answer spread over several records is as hard to follow as a bundled one. Proposed by [decision/0054-write-each-shared-record-rule-once](decision/0054-write-each-shared-record-rule-once).

**Example:** A constraint holds the seven-year receipt retention rule and its source in one record, and the deduction rule for the same tax in a second record, because the two come from different sources and change on different dates.

**Reviewed for:** Which constraint or research records hold claims with different sources, scopes or rates of change, or split one answer across records a reader cannot follow? Look at the records the change adds.

### Keep Actors to People and Organizations

**ID:** `litespec/core/intent#keep-actors-to-people-and-organizations`

**Strength:** required

**Applies to:** artifact: record:actor

**Statement:** An actor MUST carry a `kind` of person or organization and state in `belongsToTeam` whether it belongs to the team building and running the product.

**Rationale:** An actor answers for what it does, so software stands behind no actor: an external system stands alone as its own record, and internal machinery such as a reaction, a job, a transport or a renderer is never an actor. A person or an organization can hold a role, suffer a problem and take a journey. `belongsToTeam` reads as a predicate that is true about the record, so a reader tells a team actor from a customer at a glance, and a problem can refuse a team sufferer. Reworded by [decision/0049-use-literal-journey-words-and-concrete-consequences](decision/0049-use-literal-journey-words-and-concrete-consequences).

**Example:** The library reader is an actor of kind person with `belongsToTeam` false. The text gateway is an external system and no actor.

**Checked by:** `litespec model check` reports `record-invalid` for an actor record file with a kind outside person and organization, or with no `belongsToTeam`; `litespec model check` reports `field-invalid` for such a kind.

### State What the Product Assumes Each Actor Knows

**ID:** `litespec/core/intent#state-what-the-product-assumes-each-actor-knows`

**Strength:** required

**Applies to:** artifact: record:actor

**Statement:** An actor explanation MUST state what the product assumes the actor knows in its Knowledge section, as an assumption of the product, and states no user's knowledge as a fact about every user.

**Rationale:** What a user knows decides what the product can ask of them, so a reader needs the assumption stated where it applies. A fact such as "has no training in accounting" written in the opening paragraph reads as a claim about every user, and the product has no way to check it. Writing it as "Claimwick does not assume the employee knows the expense policy" names the product's assumption and the actor it applies to. Proposed by [decision/0053-describe-actors-by-responsibility-and-knowledge](decision/0053-describe-actors-by-responsibility-and-knowledge).

**Example:** Claimwick's employee actor states in its Knowledge section that Claimwick does not assume the employee knows the expense policy.

**Reviewed for:** Which actor explanations state a user's knowledge as a fact about every user, rather than as an assumption of the product? Look at the opening paragraph and the Knowledge section of each actor explanation.

### Name Every Actor from a Journey, a Problem or an Access Position

**ID:** `litespec/core/intent#name-every-actor-from-a-journey-a-problem-or-an-access-position`

**Strength:** required

**Applies to:** artifact: record:actor

**Statement:** Every actor MUST be named by a journey, by a problem in `affects`, or by an access position in `holds`.

**Rationale:** An actor named by nothing is a party nobody suffers as, acts as or grants to: a leftover from a system or a machine promoted beside real people. Naming each actor from the journey it takes, the pain it suffers or the role it holds keeps the actor list equal to the parties the product serves. Reworded by [decision/0049-use-literal-journey-words-and-concrete-consequences](decision/0049-use-literal-journey-words-and-concrete-consequences).

**Example:** The library reader is named by the borrow-and-return journey and by the due-date problem; the librarian is named by the holds of the librarian role.

**Checked by:** `litespec model check` reports `actor-unused` for an actor named by no journey, problem or access position.

### Define Each Recurring Capitalized Phrase

**ID:** `litespec/core/intent#define-each-recurring-capitalized-phrase`

**Strength:** required

**Applies to:** artifact: record:term

**Statement:** A capitalized phrase of two to four words named in three or more records MUST be defined by a glossary term.

**Rationale:** A phrase used across records reads as a defined term whether or not any definition exists: readers and agents build on a meaning nobody wrote. Flagging the phrase while it is still cheap to define keeps the glossary equal to the words the records actually use.

**Example:** A library phrase naming its first lending partners, used across goals and problems, takes a glossary term stating who those partners are, or the records stop using the phrase.

**Checked by:** `litespec model check` reports `term-missing` for a capitalized phrase of two to four words named in three or more records that no glossary term defines.

### Answer Every Constraint

**ID:** `litespec/core/intent#answer-every-constraint`

**Strength:** required

**Applies to:** artifact: record:constraint

**Statement:** A constraint MUST state its `authority`, `summary`, `sources`, `kind`, `applies`, and `appliesBecause` in fields, and What It Requires is generated from the requirements that cite it.

**Rationale:** An unanswered constraint is an outside rule the product can break unknowingly: a law, a regulation, or provider terms the team never examined. Recording each rule with its authority and applicability proves the team asked. The `kind` is technical, organizational, convention, or legal. `authority` names the body that issues the rule in a few words, and `summary` states the rule with its condition and whom it binds, in the form "the authority requires the subject to act when the condition holds". `sources` holds each source with its `key`, `url`, and `readOn`, the shape research uses, so each rule cites its source by key. A rule that stops binding is set `applies: false` with its reason in `appliesBecause`, and no end date is written. What It Requires lists the requirements that cite the constraint, generated at build time. A constraint can state `effectiveFrom`, the date its rule starts to bind, and can name in `basedOn` the research it rests on; a fact learned from the world, such as the payout bank lengthening its settlement window, first lands as research with its source and date, then as a constraint that names it in `basedOn`. Proposed by [decision/0047-cite-a-source-for-each-constraint-rule](decision/0047-cite-a-source-for-each-constraint-rule).

**Checked by:** `litespec model check` reports `record-invalid` for a constraint record file missing a required field or holding no source, and `field-invalid` for a `basedOn` that names no research record; the builder generates What It Requires from citing requirements.

### Cite a Source for Each Constraint Rule

**ID:** `litespec/core/intent#cite-a-source-for-each-constraint-rule`

**Strength:** required

**Applies to:** artifact: record:constraint

**Statement:** Each rule in a constraint's Rules section MUST cite one of the constraint's `sources` by key with a reference link `[text][key]`, and the explanation holds the generated source-links region that writes each source's link definition.

**Rationale:** A reader who checks a rule needs the source beside it, and a rule with two sources needs a place to show which one holds it. A law that states the rule on one public page is its own source, so the source sits on the constraint rather than in a research record of its own, as [decision/0047-cite-a-source-for-each-constraint-rule](decision/0047-cite-a-source-for-each-constraint-rule) decides. `model render` writes each source's link definition into the explanation file, so the link resolves in a plain Markdown preview and a reader needs no book. A hand edit to that region is drift, because the record file holds the source. Proposed by [decision/0047-cite-a-source-for-each-constraint-rule](decision/0047-cite-a-source-for-each-constraint-rule).

**Checked by:** `litespec model check` reports `source-link-bare` for a bare URL, `source-key-unknown` for a key the constraint does not declare, and `source-unused` for a source no rule cites, on the constraint's explanation; it reports `source-links-missing` and `source-links-drift` for the generated region.

### State Each Constraint Rule with Its Condition

**ID:** `litespec/core/intent#state-each-constraint-rule-with-its-condition`

**Strength:** required

**Applies to:** artifact: record:constraint

**Statement:** Each rule in a constraint's Rules section MUST keep the condition under which it holds, and it states only the outside obligation, with no product effect and no unverified fact stated as settled.

**Rationale:** A rule that drops its condition binds more or less than its source says, and a team builds against the wrong reach. A paragraph that states what the product does about the obligation mixes two authors in one rule, so the product's response stays in Effect on the Product, where each effect names the requirement that holds it. A fact the source does not establish, written as settled, sends the team to build on a guess, so that question stays in the research record's Open Questions. Scope says whom the rule covers, and it is separate from `applies`, which says whether the rule binds the product at all. A constraint holds rules that share a source, a scope and a change, so rules that change separately are separate constraints. Proposed by [decision/0047-cite-a-source-for-each-constraint-rule](decision/0047-cite-a-source-for-each-constraint-rule).

**Reviewed for:** Which rule drops the condition it holds under, states a product effect, or states an unverified fact as settled? Look at each paragraph of the Rules section beside its source.

### Record Research with Read Sources

**ID:** `litespec/core/intent#record-research-with-read-sources`

**Strength:** required

**Applies to:** artifact: record:research

**Statement:** A research record MUST state its `question`, state its `conclusion` in one sentence, list each source with its `key`, `url`, and `readOn`, and carry Findings in its explanation file.

**Rationale:** A fact without its source cannot be rechecked when a decision depends on it. `readOn` says when someone read the source, so a later reader knows how old the reading is and whether to read it again. It does not say when a rule took effect, because the date a rule starts or the period it covers is a claim a finding states with its source. Research without sources and read dates asks every later choice to rest on memory. The `conclusion` states in one sentence what the sources establish, with the conditions the findings carry, and it leaves out what follows: a consequence for the product belongs to the constraint, decision or requirement that cites the research. Each source carries its `key`, `url`, and `readOn`, and `publishedOn` where the source states its publication date. The explanation cites a source with a reference link `[text][key]`, and a bare URL, an unknown key, or an unused source is a finding. Findings states each claim with its source, as `litespec/core/intent#state-each-research-finding-with-its-source` requires. Open Questions holds what the sources do not establish, and Source Notes holds what a later reader needs about each source. Observed provider behavior lands here too, cited and dated, before any constraint or requirement rests on it. Proposed by [decision/0009-research-states-its-conclusion](decision/0009-research-states-its-conclusion) on the evidence in [research/research-conclusion-evidence](research/research-conclusion-evidence), and by [decision/0052-tie-each-research-finding-to-its-source](decision/0052-tie-each-research-finding-to-its-source).

**Checked by:** `litespec model check` reports `record-invalid` for a research record file without `question`, `conclusion`, or `sources`; `litespec model check` reports `explanation-missing` for a research record without Findings in its explanation file, `source-link-bare` for a bare URL, `source-key-unknown` for an unknown key, and `source-unused` for an unused source.

### State Each Research Finding with Its Source

**ID:** `litespec/core/intent#state-each-research-finding-with-its-source`

**Strength:** required

**Applies to:** artifact: record:research

**Statement:** Each paragraph under a research record's Findings MUST state one claim with its conditions, cite its source key with a reference link, and say whether the source states the claim or the writer infers it.

**Rationale:** A reader who rechecks a finding needs to know whether the source says it or the writer concluded it, so an inference is marked as one in the sentence that states it. A finding that drops a condition the source gives claims more than the source does, and one claim per paragraph keeps each source tied to one statement. The question of the record is the test of the findings: a finding that answers another question belongs in another research record, as [decision/0052-tie-each-research-finding-to-its-source](decision/0052-tie-each-research-finding-to-its-source) decides. A label such as high or medium confidence does not say what the source states, so the sentence says it instead. Proposed by [decision/0052-tie-each-research-finding-to-its-source](decision/0052-tie-each-research-finding-to-its-source).

**Reviewed for:** Which finding states an inference as a source statement, drops a condition the source gives, or answers a question other than the record's `question`? Look at each Findings paragraph beside its source. `litespec model check` reports `finding-source-missing` for a Findings paragraph with no source link, so this review covers the wording only.

### Shape Each Decision Page

**ID:** `litespec/core/intent#shape-each-decision-page`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** A decision MUST state its `choice` and `consequences` in fields and carry Context and Decision in its explanation file.

**Rationale:** Context in the explanation file states the situation that forced the choice, Options compares each real option on the same points, and Decision states the chosen option. The `choice` field names the chosen option, and `consequences` states what follows, so each decision reads the same way. Evidence holds the supporting material with its confidence where the choice rests on it. The `reopenedWhen` condition names what would reopen the choice, so a decision reopened by a condition rather than a date stays stable until the world actually changes. The `basedOn` link names the constraints and research behind it, and requirements only on a technical decision. The [model page](model.md) requires each product decision to name the records it settles in `decides`.

**Checked by:** `litespec model check` reports `record-invalid` for a decision record file missing a required field; `litespec model check` reports `explanation-missing` for a decision without Context or Decision in its explanation file.

### Record Each Decision Option with Pros and Cons

**ID:** `litespec/core/intent#record-each-decision-option-with-pros-and-cons`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** Each decision option MUST carry its `pros` and its `cons`.

**Rationale:** A choice argued from options and evidence can be revisited on its merits; one argued from authority cannot. Each option carries its name with what speaks for it in `pros` and against it in `cons`, so the comparison sits in the entries instead of a prose section.

**Checked by:** `litespec model check` reports `record-invalid` for a decision option missing `pros` or `cons`.

### Identify Every Requirement

**ID:** `litespec/core/intent#identify-every-requirement`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** Each requirement MUST carry one identifier `requirement/<feature>.<name>` whose feature segment names the feature whose folder holds it and whose name segment names its own folder.

**Rationale:** An identifier lets a Behavior record name the commitment it fulfills and lets the model resolve the name. A commitment written only as prose cannot be traced from Intent to code. The requirement's folder sits inside its feature's folder, so the folder names the parent, and the feature segment names the same feature wherever the identifier is cited. Settled by [decision/0045-keep-each-requirement-in-its-own-folder](../intent/decisions/0045-keep-each-requirement-in-its-own-folder/0045-keep-each-requirement-in-its-own-folder.md), which gives each requirement one authoritative file with room for an explanation. Proposed by [decision/0003-requirement-origin](decision/0003-requirement-origin) on the evidence in [research/intent-hierarchy-evidence](research/intent-hierarchy-evidence), [research/cross-layer-trace-evidence](research/cross-layer-trace-evidence), and [research/established-traceability-practice](research/established-traceability-practice).

**Checked by:** `litespec model check` reports `record-invalid` for a requirement without identifier; `litespec model check` reports `identifier-invalid` for a feature segment differing from the feature whose folder holds it.

### State Requirement Kind and Statement

**ID:** `litespec/core/intent#state-requirement-kind-and-statement`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** Each requirement MUST carry `kind` and `statement`.

**Rationale:** The `kind` is functional, quality, or policy: what the product does, how well it does it, or what the business holds. A policy requirement carries its `values` with effective dates. The `statement` states the commitment in one testable sentence.

**Checked by:** `litespec model check` reports `record-invalid` for a requirement without `kind` or `statement`, or a policy requirement without dated values.

### State One Testable Behavior in Each Requirement

**ID:** `litespec/core/intent#state-one-testable-behavior-in-each-requirement`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** A requirement `statement` MUST state one behavior that a tester can pass or fail on its own, with each condition, date and amount its result depends on.

**Rationale:** A statement that leaves out a condition passes in one reading and fails in another. "Each bank account shows its balance beside the closing balance of its last statement" names no date for the book balance, so today's balance meets last month's statement and correct books show a difference. Naming the date, the amount or the condition makes the result decidable. One behavior per requirement lets each one be fulfilled, proven and changed on its own. A requirement is complete when a developer can tell what to build and a tester can tell what passes and what fails.

**Example:** `requirement/approval.above-threshold-receipt` states that an expense above the receipt threshold carries a receipt, and `decision/0001-receipt-threshold` sets the threshold at 25 EUR, so a tester knows which expense passes.

**Reviewed for:** Which requirement statements join two behaviors, or leave out a condition, a date or an amount a tester needs? Look at the statements of each feature's requirements.

### Keep Each Rationale to Its Reason

**ID:** `litespec/core/intent#keep-each-rationale-to-its-reason`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** A requirement `rationale` MUST state why the requirement exists, adding no behavior and claiming no more than the reason supports.

**Rationale:** A rationale that adds a rule hides a second requirement where no check reads it. A rationale that names one cause where several exist, or claims a comparison nobody measured, misleads the reader who rejudges the requirement later. A difference between two balances can come from a missing entry, a wrong amount, a duplicate or a wrong opening balance, so "a difference means an entry is missing" is too narrow, and "the quickest sign" is unmeasured.

**Example:** The Claimwick rationale for `requirement/approval.nobody-approves-own-expense` reads "A second person checks each expense before it is paid": it states the reason and adds no rule.

**Reviewed for:** Which rationales add a rule, name one cause where several exist, or claim a comparison nobody measured? Look at the `rationale` of each requirement.

### Cite the Reasons of Every Requirement

**ID:** `litespec/core/intent#cite-the-reasons-of-every-requirement`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** Each requirement MUST name its reasons in `cites`.

**Rationale:** The `cites` list holds any of goal, problem, feature, constraint, research, and decision, in no order of precedence. The feature whose folder holds the requirement is the parent; the cited reasons are connections that explain why the commitment exists. A requirement that cites nothing reports as `requirement-cites-missing`.

**Checked by:** `litespec model check` reports `requirement-cites-missing` for a requirement naming no reason in `cites`.

### Cite Each Applying Constraint from a Requirement

**ID:** `litespec/core/intent#cite-each-applying-constraint-from-a-requirement`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** A requirement MUST cite in `cites` each constraint with `applies: true` that binds it.

**Rationale:** An applying constraint with no citing requirement has no answer the product keeps. Citing it ties the outside rule to the commitments that obey it, and What It Requires lists those commitments back. A constraint with `applies: true` that no requirement cites reports as `constraint-unreached` at check level G.

**Checked by:** `litespec model check` reports `constraint-unreached` for a constraint with `applies: true` that no requirement cites.

### Explain Each Requirement in One Line

**ID:** `litespec/core/intent#explain-each-requirement-in-one-line`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** Each requirement MUST explain its reason in a one-line `rationale`.

**Rationale:** A commitment without its reason cannot be rejudged when the world moves: the reader finds what holds but not why. One line forces the reason into the open without inviting an essay. The feature explanation carries the full story, and an optional requirement explanation carries what the statement does not make obvious; the `rationale` carries the reason beside the commitment.

**Reviewed for:** Which requirements carry no `rationale`, or one longer than one line? Look at the `rationale` of each requirement.

### State the Measure on a Counted Requirement

**ID:** `litespec/core/intent#state-the-measure-on-a-counted-requirement`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** A requirement whose statement names a number MUST carry `measure`.

**Rationale:** A number without its measure cannot be checked: the team cannot tell what is counted. The `measure` names what is counted. A requirement carrying `measure` needs a `check` and a passing run, and a statement naming a number with no `measure` reports at check level A.

**Checked by:** `litespec model check` reports `requirement-measure-missing` for a requirement carrying `measure` without `check` or a passing run, and for a statement naming a number with no `measure`.

### State Each Quality Requirement as a Scenario

**ID:** `litespec/core/intent#state-each-quality-requirement-as-a-scenario`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** A quality requirement MUST state the moment it holds for in `stimulus` and `environment`, what the product does in `response`, how that is measured in `measure`, and how that is confirmed in `check`, with `target`, `conditions`, `runsOn`, and `evidence` where they apply.

**Rationale:** "Fast", "secure", or "always available" cannot be built to or checked. A quality requirement holds only for a moment that can happen: something arrives, under some condition, and the product answers within a measure. Written that way, it tells the builder what to design for and tells a test or a probe what to look for, and a later change in the moment (a bigger on-sale, a new attack) visibly reopens the target. The four fields follow the quality attribute scenario the architecture literature uses, so a reviewer can compare one target with another. The `check` names the probe or test that confirms the measure, and it is required where the requirement carries `measure`. The `target` names the number aimed for, `conditions` names what holds while measuring, `runsOn` names where the check runs, and `evidence` names what the run leaves behind; each applies where the confirmation needs it.

**Example:** `requirement/approval-reminders.month-end-submission` has `kind: quality`, `stimulus` "Five hundred employees submit expenses in the last hour of the month", `environment` "Normal operation, one application instance down", `response` "Each submission is stored and acknowledged", `measure` "Share of submissions acknowledged within two seconds: at least 99 percent", and `check` "month-end acknowledgement probe".

**Checked by:** `litespec model check` reports `record-invalid` for a quality requirement missing `stimulus`, `environment`, `response`, `measure`, or `check`.

### Restrict Requirement Fulfillment to Behavior

**ID:** `litespec/core/intent#restrict-requirement-fulfillment-to-behavior`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** A requirement MUST be fulfilled only from Behavior records.

**Rationale:** The records that fulfill are rule, use case, reaction, job, notification, and setting. A journey names the requirements it shows in `cites`; it never fulfills, so a journey alone never clears an unfulfilled requirement. Behavior points up to the promises it keeps, and each promise traces down to the behavior that keeps it.

**Checked by:** `litespec model check` reports `requirement-fulfillment-missing` for a requirement no Behavior record fulfills.

### Order Each Journey Toward One Goal

**ID:** `litespec/core/intent#order-each-journey-toward-one-goal`

**Strength:** required

**Applies to:** artifact: record:journey

**Statement:** A journey MUST lead one actor toward one goal through ordered steps, naming the goal record in `serves` and stating each path off that order in `branches`.

**Rationale:** A journey names its `actor`, its goal record in `serves`, its `finishedWhen`, and its ordered `steps`. One actor means a step names another actor only at a handoff; one goal means a journey with several goals splits into several journeys. The order shows how the steps join toward the goal, so a step that fits nowhere is visible. The journey names in `cites` the requirements it carries, and the use cases, reactions, and jobs that serve each step point up with `serves`. `branches` holds the paths that leave the step order, each naming the step it leaves and its condition, and Example in the explanation file shows one concrete occasion. Proposed by [decision/0005-journeys-serve-a-goal](decision/0005-journeys-serve-a-goal) on the evidence in [research/intent-hierarchy-evidence](research/intent-hierarchy-evidence), [research/cross-layer-trace-evidence](research/cross-layer-trace-evidence), and [research/established-traceability-practice](research/established-traceability-practice), and reworded by [decision/0048-state-the-start-and-branches-of-each-journey](decision/0048-state-the-start-and-branches-of-each-journey). Reworded by [decision/0049-use-literal-journey-words-and-concrete-consequences](decision/0049-use-literal-journey-words-and-concrete-consequences).

**Example:** The largest consumer held a 21-step journey with five actors, from creating the organizer to closing the books. Split by goal, it is four journeys: set up and sell an event; buy a ticket; admit the crowd; close the night.

**Checked by:** `litespec model check` reports `record-invalid` for a journey record file missing its `actor`, `serves`, `summary`, `startedWhen`, `finishedWhen`, or `steps`.

### Describe Every Journey

**ID:** `litespec/core/intent#describe-every-journey`

**Strength:** required

**Applies to:** artifact: record:journey

**Statement:** A journey MUST state in `summary`, in up to four sentences, who takes the journey, what starts it, what good looks like, and the consequence at stake and for whom.

**Rationale:** A journey page opens with its steps, so a reader landing on it cold learns the mechanics before the purpose. The summary states the journey in the actor's terms first, and the book renders it first on the page. `finishedWhen` stays the checkable end state; the summary carries the purpose no field checks. Proposed by [decision/0049-use-literal-journey-words-and-concrete-consequences](decision/0049-use-literal-journey-words-and-concrete-consequences).

**Example:** The library journey to borrow and return describes a reader who holds a copy, starting at the shelf and ending with the copy back on its shelf, because a reader plans the next visit around the return.

**Checked by:** `litespec model check` reports `record-invalid` for a journey record file missing `summary`.

### Perform Each Journey Step with an Actor or a System

**ID:** `litespec/core/intent#perform-each-journey-step-with-an-actor-or-a-system`

**Strength:** required

**Applies to:** artifact: record:journey

**Statement:** A journey step MUST name at most one performer: another actor in `actor` at a handoff, or the external system, job or reaction in `system` when no person acts, and a step naming neither is performed by the actor taking the journey.

**Rationale:** A step performed by machinery forced a machine actor into Intent, where it sat beside real people and held no role. Naming the system record that performs the step keeps the actor list to people and organizations, and keeps the performing party one link away. A step naming both an actor and a system names two performers for one act.

**Example:** In the library journey, the step that sends the overdue notice names the text gateway in `system`; the steps the reader takes name no performer.

**Checked by:** `litespec model check` reports `record-invalid` for a journey step carrying both `actor` and `system`; `litespec model check` reports `field-invalid` for such a step or for a `system` naming no external system, job or reaction.

### Keep Behavior Out of Intent

**ID:** `litespec/core/intent#keep-behavior-out-of-intent`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An Intent record MUST NOT describe states, results, codes, transactions, edges, consistency modes, Behavior field names, or code.

**Rationale:** An Intent record that names Behavior detail becomes a second specification, and it drifts from the first with the next Behavior change. Two specifications force every reader to ask which one the code follows. Behavior records own behavior and point up to the requirements and steps they serve, so each fact has one home.

**Reviewed for:** Intent records under `.litespec/intent/` free of the detail the Statement names.

### Treat Generated Research as Source Material

**ID:** `litespec/core/intent#treat-generated-research-as-source-material`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST treat records generated outside `docs/` as source material until a research record or a decision states the claim.

**Rationale:** Records generated outside `docs/` mix verified facts, inferences, and recommendations in one confident voice. Treating them as source material with no authority keeps a recommendation from being built as if someone decided it. A research record or a decision restates the claim before anything rests on it.

**Reviewed for:** A research record or a decision behind each position built from generated records.

### State Each Exception with Its Fallback

**ID:** `litespec/core/intent#state-each-exception-with-its-fallback`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** A requirement that names an `except` condition MUST name its `fallback` requirement in its own record.

**Rationale:** An exception without a fallback leaves the reader at the exact moment the promise breaks: the product refuses with no stated alternative. The `condition` states in product words when the commitment does not hold, and `fallback` names the requirement that covers what happens then. The fallback carries no `except` of its own, so the chain stays one deep and every exception ends in a promise.

**Example:** A reimbursed expense lands in the employee's account, never more than they spent. Except when the payout bank's settlement window has closed for that batch, with `fallback` naming the requirement that has finance repay the employee by other means.

**Checked by:** `litespec model check` reports no `except-fallback-missing` finding.

### Write Journey Steps as Sentences with Ids

**ID:** `litespec/core/intent#write-journey-steps-as-sentences-with-ids`

**Strength:** required

**Applies to:** artifact: record:journey

**Statement:** Each journey step MUST carry its `id` and its product sentence in `statement`, naming one act of its performer and no use case.

**Rationale:** A step written as a product sentence reads in journey detail with no Behavior knowledge: it states what a person experiences, not which handler runs. The sentence may end with the product's observable response after the performer's act, as in "Finance reimburses the approved expense, and Claimwick pays the amount into the employee's account"; how the product computes that response stays in the cited requirement, and a second act of a person is a second step. Proposed by [decision/0048-state-the-start-and-branches-of-each-journey](decision/0048-state-the-start-and-branches-of-each-journey). The `id` (`s1`, `s2`, and so on) gives Behavior a stable target: use cases, reactions, and jobs point up with `serves` to `journey/<name>#s1`. A step that names its use case drags Behavior into Intent and breaks the journey detail every time Behavior renames. The performer and the failure text are optional: a step names the actor taking the journey only at a handoff in `actor`, or the system record performing it in `system` when no person acts, and carries `failure` where the journey can fail. Reworded by [decision/0049-use-literal-journey-words-and-concrete-consequences](decision/0049-use-literal-journey-words-and-concrete-consequences).

**Example:** Step `s4` states `Finance reimburses the approved expense.` in `statement` and names no use case; `use-case/spending.reimburse-expense` names `journey/submit-and-reimburse#s4` in `serves`.

**Checked by:** `litespec model check` reports `record-invalid` for a journey step missing `id` or `statement`, or naming a use case.

### State When Each Journey Starts

**ID:** `litespec/core/intent#state-when-each-journey-starts`

**Strength:** required

**Applies to:** artifact: record:journey

**Statement:** A journey MUST state the observable state in which it starts in `startedWhen`, in one sentence.

**Rationale:** A journey whose start sits in its summary, its Example and its first step has three starting points, and a reader cannot tell where the path begins. `startedWhen` gives the start one home beside `finishedWhen`, which states the end, and a check reads it. The detail of one occasion belongs in the Example, not in the field. Proposed by [decision/0048-state-the-start-and-branches-of-each-journey](decision/0048-state-the-start-and-branches-of-each-journey).

**Example:** `startedWhen`: the employee has paid a work cost and has not yet submitted it.

**Checked by:** `litespec model check` reports `record-invalid` for a journey record file missing `startedWhen`.

### Branch a Journey Only Between Its Steps

**ID:** `litespec/core/intent#branch-a-journey-only-between-its-steps`

**Strength:** required

**Applies to:** artifact: record:journey

**Statement:** A journey MUST leave the order of its steps only through `branches`, each naming the `step` it leaves, its `condition`, and the `nextStep` it continues at or `end`.

**Rationale:** A path stated in prose lets an action hide where no check reads it, and a reader cannot see which step the path leaves. A branch names both steps, so a check reads each path, and an action is always a step, never a branch. The `step` and `nextStep` name steps of the journey, so a branch that leaves a missing step is reported. Proposed by [decision/0048-state-the-start-and-branches-of-each-journey](decision/0048-state-the-start-and-branches-of-each-journey).

**Example:** A branch with `step` s3, `condition` the manager rejects the expense, and `nextStep` end ends the journey at the rejection; the act of rejecting stays the step s3.

**Checked by:** `litespec model check` reports `record-invalid` for a branch missing `step`, `condition` or `nextStep` and for a journey still holding `betweenSteps`, and `field-invalid` for a branch whose `step` or `nextStep` names no step of the journey.

### Reach Each Finish Condition Through a Step

**ID:** `litespec/core/intent#reach-each-finish-condition-through-a-step`

**Strength:** required

**Applies to:** artifact: record:journey

**Statement:** A journey MUST reach each clause of `finishedWhen` through a step, with each step performable once `startedWhen` and the steps before it hold, and its Example starting from `startedWhen` and performing the steps in order.

**Rationale:** A journey whose end no step reaches states a finish the path never gets to, and a step that needs a state no earlier step or start provides cannot be performed. An Example that skips a step, or that starts from a state other than `startedWhen`, teaches a path the journey does not hold. Proposed by [decision/0048-state-the-start-and-branches-of-each-journey](decision/0048-state-the-start-and-branches-of-each-journey).

**Example:** The journey to reimburse an expense ends in `finishedWhen` with the employee holding the money, which step 4 reaches; its Example starts at the paid cost and performs the submission, the approval and the payout in that order.

**Reviewed for:** Which clause of `finishedWhen` no step reaches, which step needs a state that no start or earlier step gives, and which Example starts elsewhere or skips a step? Compare the Example with `startedWhen`, `finishedWhen` and the steps.

### State Each Journey Blocker as a Step Failure

**ID:** `litespec/core/intent#state-each-journey-blocker-as-a-step-failure`

**Strength:** required

**Applies to:** artifact: record:journey

**Statement:** A condition that stops a journey MUST be the `failure` of the step it blocks, with the requirement that holds it cited in `cites`, and never stated only in a branch or in the explanation.

**Rationale:** A blocker stated in a branch or in the Example reads as a path the journey takes, and a blocker stated only in prose has no step a reader can find. The `failure` of the step keeps each blocker where the step fails, and `cites` names the requirement that holds the rule, so the rule is not restated in the journey. A branch from the blocked step names where the actor goes after the failure. Proposed by [decision/0048-state-the-start-and-branches-of-each-journey](decision/0048-state-the-start-and-branches-of-each-journey).

**Example:** The submission of an expense above the receipt threshold without a receipt is the `failure` of the step that submits it, and the requirement that holds the receipt rule is cited in `cites`.

**Reviewed for:** Which condition that stops the journey sits in a branch or in the explanation rather than as a `failure`, and which cited requirement holds the rule? Read each branch and each Example sentence that names a refusal or a wait.

### State When Each Journey Finishes

**ID:** `litespec/core/intent#state-when-each-journey-finishes`

**Strength:** required

**Applies to:** artifact: record:journey

**Statement:** A journey MUST state its observable end in `finishedWhen`.

**Rationale:** A journey without a stated end can stop anywhere: the reader cannot tell a finished journey from an abandoned one, and serving records cannot tell done from dropped. `finishedWhen` states the observable end in one sentence, in product words. Reworded by [decision/0049-use-literal-journey-words-and-concrete-consequences](decision/0049-use-literal-journey-words-and-concrete-consequences).

**Example:** `finishedWhen`: the employee holds the reimbursement.

**Checked by:** `litespec model check` reports `record-invalid` for a journey record file missing `finishedWhen`.

### Treat Tenant-Set Values as Data

**ID:** `litespec/core/intent#treat-tenant-set-values-as-data`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** A consumer MUST treat a `setBy: tenant` value as tenant data.

**Rationale:** Tenants choose their own values within the ranges Intent sets: a tenant holds its receipt threshold at EUR 25 by default within the EUR 0 to EUR 500 range. The requirement sets the range and the default, and each tenant's choice inside it is data. Treating tenant values as product values would bind every tenant to one tenant's choice.

**Example:** The tenant sets a receipt threshold of EUR 50; the requirement covering threshold ranges already sets the range, so the threshold itself is data.

**Reviewed for:** Each `setBy: tenant` value stored as tenant data.

## Conventions

### Group Product Records by Problem

**ID:** `litespec/core/intent#group-product-records-by-problem`

**Strength:** default

**Applies to:** artifact: record:product, record:goal, record:problem, record:constraint, record:decision, record:research, record:actor, record:role, record:journey, record:feature, record:requirement

**Default:** Keep the product record at `.litespec/intent/product.yaml`, each product-level goal, problem, constraint, decision, research record, actor and journey in its own folder under `.litespec/intent/`, each feature in the `features/` folder of the problem it answers, `.litespec/intent/problems/<problem>/features/`, and each requirement in its own folder in its feature's `requirements/` folder.

**Replacement:** A project records its own product layout in its documentation index.

**Rationale:** A problem folder keeps the features that answer it side by side. One folder per kind keeps each record findable: goals, problems, constraints, decisions, research, journeys, and actors each have one home. A newcomer learns the layout once and predicts where the next record lives. A feature names no module, because it answers a problem and Intent groups by reason. Proposed by [decision/0050-put-each-feature-inside-the-problem-it-answers](decision/0050-put-each-feature-inside-the-problem-it-answers).

**Example:**

```text
.litespec/intent/
  product.yaml
  goals/
    <name>/
      <name>.yaml
      <name>.md
  problems/
    <name>/
      <name>.yaml
      <name>.md
      features/
        <name>/
          <name>.yaml
          requirements/
            <requirement>/
              <requirement>.yaml
  constraints/
    <name>/
      <name>.yaml
  decisions/
    <number>-<name>/
      <number>-<name>.yaml
      <number>-<name>.md
  research/
    <name>/
      <name>.yaml
      <name>.md
  actors/
    <name>/
      <name>.yaml
  journeys/
    <name>/
      <name>.yaml
```

**Reviewed for:** The `.litespec/intent/` layout beside the project's recorded layout.

### Name Product Decisions in One Numbered Sequence

**ID:** `litespec/core/intent#name-product-decisions-in-one-numbered-sequence`

**Strength:** default

**Applies to:** artifact: record:decision

**Default:** Name each product decision folder `<four-digit number>-<name>/`, holding its record file `<four-digit number>-<name>.yaml`, and use `decision/<four-digit number>-<name>` as its identifier.

**Replacement:** A project records its own decision naming in its documentation index.

**Rationale:** One numbered sequence orders each decision by when the choice was proposed, giving every record a short stable name. The number counts as a word, so digits never stand alone in the identifier. Scattered names would force every citation to carry the full path.

**Reviewed for:** Each decision folder named `<number>-<name>/` holding `<number>-<name>.yaml`, with the number and name matching its identifier.

## Reference Example

This informative example demonstrates `litespec/core/intent#order-each-journey-toward-one-goal`, `litespec/core/intent#write-journey-steps-as-sentences-with-ids`, `litespec/core/intent#state-when-each-journey-starts`, `litespec/core/intent#state-when-each-journey-finishes`, `litespec/core/intent#branch-a-journey-only-between-its-steps`, `litespec/core/intent#identify-every-requirement`, `litespec/core/intent#state-each-exception-with-its-fallback`, and `litespec/core/intent#treat-tenant-set-values-as-data`.

The book shows journey `journey/submit-and-reimburse` as record detail. No field name, YAML, or Behavior page appears.

```text
Submit and reimburse

An employee submits an expense and is reimbursed after approval.

Started when: the employee has paid a work cost and has not yet submitted it.

Finished when: the employee holds the reimbursement.

1. The employee submits the expense with its amount and currency.
2. The employee attaches the receipt to the expense.
3. The manager approves or rejects the expense within five business days.
4. Finance reimburses the approved expense.

At step 3, when the manager rejects the expense, the journey ends.

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
