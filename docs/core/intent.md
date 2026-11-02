# Intent Layer

## Intent

Intent is why the product exists and what it promises: the product, actors, goals, problems, features, topics, requirements, journeys, constraints, research, decisions, and terms. Agents draft these records and the decider decides them, before Behavior states how the system keeps the promises. This page states where Intent lives, what shape each record takes, and what Intent never holds. The [record language page](language.md) states how each field, value and record name is written.

## Concepts

### The Questions Intent Answers

| Question | Record | Write It When | Skip It When | Familiar As |
|:---|:---|:---|:---|:---|
| What is the product, for whom, and what will it never do? | product | Always, once | Never | Product brief |
| Who touches the product? | actor | A person or organization appears in a problem, a journey or an access position | The "actor" is a role or a system; roles are Behavior and systems stand alone | Stakeholder |
| Which number in the world do we want to move? | goal | You can name what you count | The system controls it directly: that is a quality requirement | Business objective |
| What pain do people outside the team have today? | problem | You can name who suffers, what it costs them and what they do instead, with evidence | Only one solution is imaginable: it is a feature | Pain point |
| What do we offer against a problem, and when? | feature | Scope and timing need tracking | The commitment is permanent and no offer carries it | Capability |
| Which requirements share one story? | topic | Always, for requirements | Never | Chapter |
| What exactly must hold? | requirement | Always | Never | User story |
| Can a person get from start to finish? | journey | A task crosses several actions | It is one action | User journey |
| Which outside rule binds us? | constraint | An outside party can hold us to it | It is our own choice: a decision | Mandated constraint |
| Which outside fact did we rely on, and when was it true? | research | A choice rests on it and it could change | It binds us: a constraint | Spike |
| Which option did we choose, and what reopens it? | decision | There were real options | There was nothing to choose | Decision record |
| Which word means what? | term | A word could mean two things | Never skipped for a domain word | Glossary term |

### Product Intent and Its Parts

Product intent is what the product commits to: what it does, for whom, and where it stops. Running the company is no part of it. [Keep Company Operation Out of Intent](#keep-company-operation-out-of-intent) states the boundary.

Intent is not Behavior. A rule or use case states what the system does today and matches the code. A requirement states what the business decided and why, and it can describe a position the code does not carry yet. The two connect in one direction: Behavior records name the requirements they fulfill and the steps they serve, and an Intent record never describes Behavior, except a decision naming the records it settles in `decides` and an actor naming its roles in `holds`.

The [model page](model.md) defines the record types and links. A topic page keeps the present and the future in its chapter and its commitments, while the book lists the requirement entries and the decisions behind them.

### How Intent Reaches Behavior

The [model page](model.md) states the authority order that settles conflicts between records. Goals, problems, features, journeys, actors, research, and prose carry no authority, and decisions win between peers.

A decision wins its choice, and a topic page states it as requirements. Behavior records name the requirements they fulfill and the journey steps they serve, and the code carries what happens today. When Behavior contradicts a requirement, either the code carries a defect or the business changed. A changed business means a new decision, approved by a person, before any page or code moves.

What a lower record layer needs and the upper record layer does not say travels up as a draft question with a decider and a date (`litespec/core/layers#raise-open-needs-as-draft-questions`). A fact learned from the world enters Intent as research or a constraint, cited and dated (`litespec/core/layers#enter-world-facts-as-cited-research-or-constraints`). Nothing travels up from code, except a transcribed draft written from code during adoption (`litespec/core/model#hold-transcribed-records-in-draft`): where Intent is silent, working code is evidence for a question, not a defect to delete and not a source to transcribe silently.

### Requirements with Except and Fallback

A requirement carries `kind` (functional, quality, or policy), `statement`, `rationale`, and `measure` with `dueBy` where its statement names a number. `setBy` names who sets its value: `decider`, `operator`, or `tenant`. `risk` names the risk classes the record carries: `money`, `physical-access`, `personal-data`, or `access`, each shown by name. A record with no risk shows no risk and carries no approval rule from risk.

An `except` entry states in product words when the commitment does not hold, and `fallback` names the requirement that covers what happens then. The fallback carries no `except` of its own: exceptions stay one deep, so every exception ends in a promise.

Approved requirements carry `approvedBy`, `approvedOn`, and the builder-written `approvedHash`. A record whose risk class the project approval table names needs the approval that table states for that class; only questions auto-approve, as `litespec/core/model#answer-each-question-on-its-record` states. The [model page](model.md) states the approval table.

### Journeys as Product Sentences

A journey carries `actor`, `serves`, `description`, `finishedWhen`, ordered `steps`, `betweenSteps`, and `cites`. `serves` names the goal record. `description` states in up to four sentences who walks, what starts the walk, what good looks like, and why it matters, and the book renders it first on the page. `finishedWhen` states the observable end in one sentence. Each step carries its `id` (`s1`, `s2`, and so on) and its product sentence in `statement`, names the walking actor only at a handoff in `actor`, or names the system record performing it in `system` when no person acts, and carries its failure text in `failure` where the walk can fail. A step carries exactly one of `actor` and `system`, or neither when the walking actor performs it. `betweenSteps` states the branch order between the steps. A step id reads `journey/<name>#s1`.

Steps name no use case. Use cases, reactions, and jobs point up with `serves`, naming the steps they serve. The book shows each journey as record detail: `finishedWhen`, the steps as sentences with `failure`, the requirements it cites with their statements and their `except` and fallback inline, the serving use case goals, the Then clauses of their scenarios as context, a consequences box as context, and the open questions. Field names never render literally in journey detail.

A journey walks one actor toward one goal. A step names another actor only at a handoff, and a walk with several goals splits into several journeys.

### Staff Journeys

Journeys are not only employee walks. A staff journey walks a staff actor, such as finance exporting the approved batch, through the same shape of serves, `finishedWhen`, steps, and `betweenSteps`. A scheduled promise is a requirement that a job fulfills, not a journey. Staff steps read as product sentences too, so the decider approves them the same way.

### Questions and Ballots

A question carries `question`, `decider`, `decideBy`, `ballot`, and `recommendation`, with `answer`, `answeredBy`, and `answeredOn` once answered. Questions ask in product words, so the decider answers without opening a Behavior page. The `ballot` groups questions in the inbox by a decision id or a short topic slug, so the decider answers one ballot at a time. The question lifecycle lives once in `litespec/core/model#answer-each-question-on-its-record`.

### The Consequences Summary

Intent is enough to approve. Each ballot and journey detail carries a consequences summary rendered from Behavior: new refusals in product words, data kept and for how long, and money effects. The summary is marked as context and stays outside the approval hash. The [approval hash guide](../guide/approval-hash.md) lists which fields and which explanation files feed the hash per record type.

### What Intent Never Contains

Intent never holds Behavior detail. `litespec/core/intent#keep-behavior-out-of-intent` names what stays out.

## Standards

### Keep Intent in Its Homes

**ID:** `litespec/core/intent#keep-intent-in-its-homes`

**Statement:** A consumer MUST keep its brief, topic pages, goals, problems, features, constraints, product decisions, actors, and journeys under `.litespec/intent/`, and its research under `.litespec/intent/research/`.

**Rationale:** Intent split across notes and books gives no single answer to the question an agent asks before building. One home per kind, with the brief, topic pages, goals, problems, features, constraints, product decisions, actors, and journeys under `.litespec/intent/` and research under `.litespec/intent/research/`, gives every reader one place to look. A second home for any kind starts a second truth about the business. An actor is a person or an organization, and an actor record carries `summary`.

**Reviewed for:** Each brief, topic page, goal, problem, feature, constraint, product decision, actor, and journey under `.litespec/intent/`, and each research record under `.litespec/intent/research/`.

### State One Product

**ID:** `litespec/core/intent#state-one-product`

**Statement:** A consumer MUST state exactly one product record in its brief, with `boundary`, `exclusions`, `addresses`, and `summary`.

**Rationale:** One product record gives every reader one answer to what the business is, who it serves, and where it stops. The `boundary` and `addresses` focus every later choice; `exclusions` refuses what the product does not become, so tempting expansions meet a written refusal. Two product statements let each plan cite the one it prefers. `summary` states what the product is in one place.

**Checked by:** `litespec model check` reports no `product-count-invalid` finding, and `litespec consumer` validates the brief fields against the record schema.

### Keep Company Operation Out of Intent

**ID:** `litespec/core/intent#keep-company-operation-out-of-intent`

**Statement:** An Intent or Behavior record MUST state only behavior the application performs, data it holds, or a limit it enforces, so a company fact enters only as a constraint or as dated research.

**Rationale:** LiteSpec specifies the product, not the company. Prices as an offer, markets, hiring, sales actions, launch calendars, company goals and founder risks can be checked only against a bank account, a diary or a contract, so they sit outside the records a reader reviews as the product. A company fact that binds the product still enters: as a constraint when an authority or a limit binds the application, and as dated research when a choice rests on it. Plan entitlements the software enforces stay, as settings and permissions; the price list as a company offer leaves.

**Example:** A library holds its lending limit as a setting the application enforces, so the limit stays as Behavior. The membership price the library charges as an offer is a company fact: it enters only as dated research behind the decision that sets the limit.

**Reviewed for:** Intent and Behavior records stating company operation beside the constraints and research that carry the binding facts.

### State Each Goal with Its Measure

**ID:** `litespec/core/intent#state-each-goal-with-its-measure`

**Statement:** A goal MUST state its `statement`, `measure`, `target`, `dueBy`, and `whyItMatters` in fields.

**Rationale:** A goal without a measure is a wish no check can confirm: the team cannot tell whether the product serves it. The `statement` names the outcome, `measure` and `target` name what is counted and the number aimed for, and `dueBy` names the date or milestone. `whyItMatters` explains why the outcome matters, so a later reader can judge whether a cheaper goal serves as well. A goal carries no authority: it explains why features exist and never decides a conflict.

**Checked by:** `litespec consumer` fails a goal record file missing a required field.

### Move Every Goal with a Journey or a Quality Requirement

**ID:** `litespec/core/intent#move-every-goal-with-a-journey-or-a-quality-requirement`

**Statement:** A goal MUST be moved by a journey serving it or by a quality requirement citing it.

**Rationale:** A goal no journey serves and no quality requirement confirms names an outcome the product never touches: a company hope filed as a product goal. A journey serving the goal shows the walk that moves the counted outcome, and a quality requirement citing it states the measure that confirms the move. An outcome the system controls directly is a quality requirement, and an outcome only the company controls is no goal at all.

**Example:** The library goal of faster pickup is moved by the borrow-and-return journey serving it; a goal of higher membership sales is moved by no journey and is no product goal.

**Checked by:** `litespec model check` reports `goal-mover-missing` for a goal moved by no journey and no quality requirement.

### Admit Only Outside Pains as Problems

**ID:** `litespec/core/intent#admit-only-outside-pains-as-problems`

**Statement:** A problem MUST name in `affects` a primary sufferer that is a person or organization actor outside the team building the product, qualify that sufferer in `affectsSegment`, and state the pain in `statement` and `situation` in fields.

**Rationale:** A problem is a pain someone outside the team has today. The first `affects` entry is the primary sufferer, so who has the pain is the first fact a reader meets, and `affectsSegment` qualifies that sufferer with its segment, its size if known, and its geography. `statement` states the sufferer, the condition and the consequence in one sentence, with the product never as its subject. `situation` states when it happens and what the person was trying to do, and never stands as a bare pointer to another page. What is no problem leaves for its own record: a solution in disguise becomes a feature, a risk that might happen becomes a decision or a goal, an internal constraint becomes a constraint on the plan written outside Intent, a market fact becomes research, and a legal duty becomes a constraint.

**Example:** In the library product, readers losing track of due dates is a problem whose primary sufferer is the reader. The library having too few staff for the summer is an internal constraint and no problem.

**Checked by:** `litespec model check` reports `problem-sufferer-invalid` for a problem whose primary sufferer is no person or organization outside the team; `litespec consumer` fails a problem record file missing `affects`, `affectsSegment`, `statement` or `situation`.

### State the Cost and Workaround of Every Problem

**ID:** `litespec/core/intent#state-the-cost-and-workaround-of-every-problem`

**Statement:** A problem MUST state what the pain costs its sufferer today in `costToday` and what the sufferer does instead in `workaroundToday`, each in the sufferer's own units, or the word `unknown` with an open question naming the source that would price it.

**Rationale:** A pain with no cost and no workaround is a slogan: no reader can tell a daily loss from a passing annoyance, and no feature can be judged against the price of standing still. The workaround shows the pain is real enough to work around, and why that workaround fails. Where nobody has priced the cost or named the workaround, `unknown` states the gap honestly and the open question names who or what would close it.

**Example:** A reader who loses track of due dates pays late fees each month in `costToday`, and keeps a paper list of due dates in `workaroundToday`, which fails when the list stays at home.

**Checked by:** `litespec model check` reports `problem-cost-missing` for a problem stating no cost today, and `problem-workaround-missing` for a problem stating no workaround today, each allowing `unknown` only with an open question on the record.

### Grade Problem Evidence by Kind

**ID:** `litespec/core/intent#grade-problem-evidence-by-kind`

**Statement:** A problem MUST list each evidence entry with its `claim`, its `source` as a record id or a URL, its `readOn` date, and its `kind` as observed, reported or assumed, and carries at least one observed or reported entry before it is approved.

**Rationale:** Evidence states how anyone knows the pain is real: observed means a sufferer was watched, reported means a sufferer said it, and assumed means nobody outside the team has confirmed it. The strength of a problem is the strongest kind present, computed by the builder and never written, so it cannot drift from the entries. Approval certifies a signature over evidence, so an approved problem resting on assumption alone, or on no entry, stays draft until a sufferer is watched or heard.

**Example:** The library problem of readers losing due dates carries a reported entry from the desk log, read on its date, so its computed strength is reported and it may be approved.

**Checked by:** `litespec model check` reports `problem-evidence-missing` for an approved problem with only assumed evidence or none. The builder writes `evidenceStrength` on each problem in the model.

### Name No Solution in a Problem Statement

**ID:** `litespec/core/intent#name-no-solution-in-a-problem-statement`

**Statement:** A problem `statement` MUST name no product, feature or solution.

**Rationale:** A statement naming a solution files a missing feature as a pain: the reader approves the answer before weighing the pain, and every alternative answer reads as off-topic. A statement naming the sufferer, the condition and the consequence leaves the answer to the feature that solves it.

**Example:** "Readers lose track of due dates" names a pain. "Readers need a reminder dashboard" names a solution and is reported.

**Checked by:** `litespec model check` reports `problem-statement-invalid` for a problem statement naming a solution word.

### Name Each Sibling Problem or Merge It

**ID:** `litespec/core/intent#name-each-sibling-problem-or-merge-it`

**Statement:** Two problems with the same primary sufferer cited from the same topic MUST name each other or merge into one problem.

**Rationale:** Two pains of one sufferer under one topic, written in different words, hide that they are one pain or two: features solve each half, and the book lists the sufferer twice with no sign of the overlap. Naming the sibling states the split where a reader meets it; merging keeps one pain with one sufferer.

**Example:** Two library problems of the reader cited from the borrowing topic, one on due dates and one on renewals, name each other, so a reader sees both before a feature solves either.

**Checked by:** `litespec model check` reports `problem-duplicate` for two problems with the same primary sufferer cited from the same topic that name neither each other.

### Explain Every Problem in Four Sections

**ID:** `litespec/core/intent#explain-every-problem-in-four-sections`

**Statement:** A problem explanation file MUST carry Who Has It, What They Do Today, Evidence, and What Would Retire It, in that order.

**Rationale:** The fields state who suffers, what it costs and what they do instead; the explanation carries the one concrete occasion, the workaround in use, the evidence behind each claim, and what change in the world would retire the pain. A fixed order means a reader who learned one problem page finds each part on every problem page.

**Example:** The library problem on due dates opens Who Has It with one reader at the desk with three overdue copies, and closes with What Would Retire It: readers holding their due dates without a paper list.

**Reviewed for:** The four sections, in order, in each problem explanation file.

### State Each Problem with Its Evidence

**ID:** `litespec/core/intent#state-each-problem-with-its-evidence`

**Statement:** A problem MUST state its `statement` and `situation` in fields, with its evidence graded under [Grade Problem Evidence by Kind](#grade-problem-evidence-by-kind).

**Rationale:** A problem that names what its people face points to the pain its solution lifts. `situation` describes the pain in the people's own terms, and the graded evidence shows it is real. Problems without sufferers attract solutions nobody needs. The `affects` link names the actors who face it, so each pain traces to the people a solution must serve.

**Checked by:** `litespec consumer` fails a problem record file missing its `statement` or `situation`.

### State Each Feature with Its Lifecycle

**ID:** `litespec/core/intent#state-each-feature-with-its-lifecycle`

**Statement:** A feature MUST state its `lifecycle` and `offering`, name the problems it solves in `solves`, and, unless its lifecycle is refused, name the goals it moves in `advances`, with a missing goal link reported as coverage.

**Rationale:** A feature names the problems it solves in `solves` and, unless its lifecycle is refused, the goals it moves in `advances`, so each offer traces to a pain and an outcome. Its `lifecycle` is planned, shipped, later, or refused. A refused feature names the problem it declines in `solves` and keeps its reason in `refusedBecause`, so the team never relitigates the refusal, and a planned feature names its milestone or date in `plannedFor`. `offering` states what is in and what is out, so the scope of the offer is written where the lifecycle is. Proposed by [decision/0004-features-advance-goals](decision/0004-features-advance-goals) on the evidence in [research/intent-hierarchy-evidence](research/intent-hierarchy-evidence), [research/cross-layer-trace-evidence](research/cross-layer-trace-evidence), and [research/established-traceability-practice](research/established-traceability-practice).

**Checked by:** `litespec consumer` fails a feature record file missing its `lifecycle`, `offering`, refused reason, or planned milestone or date; `litespec model check` reports `feature-goal-missing` for a feature naming no goal.

### Keep Actors to People and Organizations

**ID:** `litespec/core/intent#keep-actors-to-people-and-organizations`

**Statement:** An actor MUST carry a `kind` of person or organization and state in `belongsToTeam` whether it belongs to the team building and running the product.

**Rationale:** An actor answers for what it does, so software stands behind no actor: an external system stands alone as its own record, and internal machinery such as a reaction, a job, a transport or a renderer is never an actor. A person or an organization can hold a role, suffer a problem and walk a journey. `belongsToTeam` reads as a predicate that is true about the record, so a reader tells a team actor from a customer at a glance, and a problem can refuse a team sufferer.

**Example:** The library reader is an actor of kind person with `belongsToTeam` false. The text gateway is an external system and no actor.

**Checked by:** `litespec consumer` fails an actor record file with a kind outside person and organization, or with no `belongsToTeam`; `litespec model check` reports `field-invalid` for such a kind.

### Name Every Actor from a Journey, a Problem or an Access Position

**ID:** `litespec/core/intent#name-every-actor-from-a-journey-a-problem-or-an-access-position`

**Statement:** Every actor MUST be named by a journey, by a problem in `affects`, or by an access position in `holds`.

**Rationale:** An actor named by nothing is a party nobody suffers as, walks as or grants to: a leftover from a system or a machine promoted beside real people. Naming each actor from the walk it walks, the pain it suffers or the role it holds keeps the actor list equal to the parties the product serves.

**Example:** The library reader is named by the borrow-and-return journey and by the due-date problem; the librarian is named by the holds of the librarian role.

**Checked by:** `litespec model check` reports `actor-unused` for an actor named by no journey, problem or access position.

### Define Each Recurring Capitalized Phrase

**ID:** `litespec/core/intent#define-each-recurring-capitalized-phrase`

**Statement:** A capitalized phrase of two to four words named in three or more records MUST be defined by a glossary term.

**Rationale:** A phrase used across records reads as a defined term whether or not any definition exists: readers and agents build on a meaning nobody wrote. Flagging the phrase while it is still cheap to define keeps the glossary equal to the words the records actually use.

**Example:** A library phrase naming its first lending partners, used across goals and problems, takes a glossary term stating who those partners are, or the records stop using the phrase.

**Checked by:** `litespec model check` reports `term-missing` for a capitalized phrase of two to four words named in three or more records that no glossary term defines.

### Answer Every Constraint

**ID:** `litespec/core/intent#answer-every-constraint`

**Statement:** A constraint MUST state its `authority`, `kind`, `applies`, `appliesBecause`, and `summary` in fields, with What It Requires generated from the requirements that cite it.

**Rationale:** An unanswered constraint is an outside rule the product can break unknowingly: a law, a regulation, or provider terms the team never examined. Recording each rule with its authority and applicability proves the team asked. A project with no outside rules records that answer in `constraints.answered` in `.litespec/project.json`, as the [scope page](scope.md) states, so a later reader can tell examined from ignored. The `kind` is technical, organizational, convention, or legal. `summary` states the rule in the authority's own words, and What It Requires lists the requirements that cite the constraint, generated at build time. A fact learned from the world, such as the payout bank lengthening its settlement window, first lands as research with its source and date, then as a constraint draft.

**Checked by:** `litespec consumer` fails a constraint record file missing a required field; the builder generates What It Requires from citing requirements.

### Record Research with Read Sources

**ID:** `litespec/core/intent#record-research-with-read-sources`

**Statement:** A research record MUST state its `question`, state its `conclusion` in one sentence, list each source with its `key`, `url`, and `readOn`, and carry Answer in its explanation file.

**Rationale:** A fact without its source cannot be rechecked when a decision depends on it. The read date shows when the outside fact was true, so a later reader knows whether to trust or refresh it. Research without sources and dates asks every later choice to rest on memory. The `conclusion` states in one sentence what the sources establish and what follows, so a decision can cite it as data. Each source carries its `key`, `url`, and `readOn`; the explanation cites a source with a reference link `[text][key]`, and a bare URL, an unknown key, or an unused source is a finding. Answer expands the `conclusion`: what the sources say, claim by claim, with the confidence behind each claim, and Notes on Sources holds what a later reader needs about each source. Observed provider behavior lands here too, cited and dated, before any constraint or requirement rests on it. Proposed by [decision/0009-research-states-its-conclusion](decision/0009-research-states-its-conclusion) on the evidence in [research/research-conclusion-evidence](research/research-conclusion-evidence).

**Checked by:** `litespec consumer` fails a research record file without `question`, `conclusion`, or `sources`; `litespec model check` reports `explanation-missing` for a research record without Answer in its explanation file, `source-link-bare` for a bare URL, `source-key-unknown` for an unknown key, and `source-unused` for an unused source.

### Shape Each Decision Page

**ID:** `litespec/core/intent#shape-each-decision-page`

**Statement:** A decision MUST state its `choice` and `consequences` in fields and carry Context in its explanation file.

**Rationale:** Context in the explanation file states the choice, `choice` names the chosen option, and `consequences` states what follows, so each decision reads the same way. Decision in the explanation file expands the chosen option, and Evidence with Confidence holds the supporting material with its confidence where the choice rests on it. The `reopenedWhen` condition names what would reopen the choice, so a decision reopened by a condition rather than a date stays stable until the world actually changes. The `basedOn` link names the constraints and research behind it, and requirements only on a technical decision. The [model page](model.md) requires each product decision to name the records it settles in `decides`.

**Checked by:** `litespec consumer` fails a decision record file missing a required field; `litespec model check` reports `explanation-missing` for a decision without Context in its explanation file.

### Record Each Decision Option with Pros and Cons

**ID:** `litespec/core/intent#record-each-decision-option-with-pros-and-cons`

**Statement:** Each decision option MUST carry its `pros` and its `cons`.

**Rationale:** A business choice argued from options and evidence can be revisited on its merits; one argued from authority cannot. Each option carries its name with what speaks for it in `pros` and against it in `cons`, so the review content sits in the entries instead of a prose section.

**Checked by:** `litespec consumer` fails a decision option missing `pros` or `cons`.

### Name the Person Who Approved a Decision

**ID:** `litespec/core/intent#name-the-person-who-approved-a-decision`

**Statement:** An approved decision MUST carry `approvedBy` with its decider, `approvedOn` with its date, and `approvedHash` over its semantic fields and its explanation file.

**Rationale:** Product, commercial, and legal choices belong to an accountable person. Naming the decider while the decision is a draft shows who approves before release. Building can start on a draft while approval gates the release, so agent work never stalls on one person's queue. The `approvedBy` name and `approvedOn` date prove the approval happened, and the builder writes `approvedHash` over the semantic fields and the explanation file so a later change reopens the approval. The [approval hash guide](../guide/approval-hash.md) lists which fields and which explanation files feed the hash per record type.

**Reviewed for:** The `approvedBy` name, `approvedOn` date, and `approvedHash` on the approved decision beside the approval from its named decider.

### Replace a Decision with a New Record

**ID:** `litespec/core/intent#replace-a-decision-with-a-new-record`

**Statement:** A changed choice MUST be a new decision naming the old one in `replaces`, with the old one retired in the same change.

**Rationale:** A decision edited in place loses the reason it once held, and every topic page that cited it silently changes meaning. A new decision naming the old one in `replaces`, with the old one retired in the same change, keeps the reasoning chain intact. The replaced record stays readable as the past, so a later reader can follow why the position moved.

**Reviewed for:** The `replaces` name on the new decision and the retired status on the old one, in the same change.

### Shape Each Topic Page

**ID:** `litespec/core/intent#shape-each-topic-page`

**Statement:** A topic MUST state its `summary`, `position`, `reviewBy`, `owner`, and `status` in fields and carry The Situation, What We Do, Why It Is Better, Example, Costs and Risks, and How Sure We Are in its explanation file.

**Rationale:** The fixed shape puts the present and the future of one area in one record file and its explanation file. The chapter carries the story for people: `summary` states it in one place, and The Situation, What We Do, Why It Is Better, and Example in the explanation file tell it. The commitments carry the testable promises and the choices behind them: Costs and Risks and How Sure We Are in the explanation file, and `position` in fields. `position` holds choices with their decisions, including choices no system carries, and `refusals` holds what the topic refuses. A reader learns where each answer sits once and finds it on every page. The `reviewBy` field dates the next reread against decisions and evidence. Requirements are entries because small commitments sharing one story read better together than alone. The topic is a record with `topic/<name>` as its identifier and `.litespec/intent/<area>/<topic>.yaml` as its record file, under the product; the area is a folder only. The book lists the requirement entries with their statements and their `except` and fallback inline.

**Checked by:** `litespec consumer` fails a topic record file missing a required field; `litespec model check` reports `explanation-missing` for a topic without its explanation sections.

### Identify Every Requirement

**ID:** `litespec/core/intent#identify-every-requirement`

**Statement:** Each requirement entry MUST carry one identifier `requirement/<topic>.<name>` naming its own page.

**Rationale:** An identifier lets a Behavior record name the commitment it fulfills and lets the model resolve the name. A commitment written only as prose cannot be traced from Intent to code. The topic segment names the page holding the entry, and the topic is the parent by identifier containment. Proposed by [decision/0003-requirement-origin](decision/0003-requirement-origin) on the evidence in [research/intent-hierarchy-evidence](research/intent-hierarchy-evidence), [research/cross-layer-trace-evidence](research/cross-layer-trace-evidence), and [research/established-traceability-practice](research/established-traceability-practice).

**Checked by:** `litespec consumer` fails a requirement entry without identifier; `litespec model check` reports `identifier-invalid` for a topic segment differing from its page.

### State Requirement Kind and Statement

**ID:** `litespec/core/intent#state-requirement-kind-and-statement`

**Statement:** Each requirement entry MUST carry `kind` and `statement`.

**Rationale:** The `kind` is functional, quality, or policy: what the product does, how well it does it, or what the business holds. A policy requirement carries its `values` with effective dates. The `statement` states the commitment in one testable sentence.

**Checked by:** `litespec consumer` fails a requirement entry without `kind` or `statement`, or a policy requirement without dated values.

### Cite the Reasons of Every Requirement

**ID:** `litespec/core/intent#cite-the-reasons-of-every-requirement`

**Statement:** Each requirement entry MUST name its reasons in `cites`.

**Rationale:** The `cites` list holds any of goal, problem, feature, constraint, research, and decision, in no order of precedence. The topic holding the entry is the parent; the cited reasons are connections that explain why the commitment exists. A requirement that cites nothing reports as `requirement-cites-missing`, at check level A, or check level G when the requirement carries `risk`.

**Checked by:** `litespec model check` reports `requirement-cites-missing` for a requirement entry naming no reason in `cites`.

### Cite Each Applying Constraint from a Requirement

**ID:** `litespec/core/intent#cite-each-applying-constraint-from-a-requirement`

**Statement:** A requirement MUST cite in `cites` each constraint with `applies: true` that binds it.

**Rationale:** An applying constraint with no citing requirement has no answer the product keeps. Citing it ties the outside rule to the commitments that obey it, and What It Requires lists those commitments back. A constraint with `applies: true` that no requirement cites reports as `constraint-unreached` at check level G.

**Checked by:** `litespec model check` reports `constraint-unreached` for a constraint with `applies: true` that no requirement cites.

### Explain Each Requirement in One Line

**ID:** `litespec/core/intent#explain-each-requirement-in-one-line`

**Statement:** Each requirement entry MUST explain its reason in a one-line `rationale`.

**Rationale:** A commitment without its reason cannot be rejudged when the world moves: the reader finds what holds but not why. One line forces the reason into the open without inviting an essay. The chapter carries the full story; the `rationale` carries the reason beside the commitment.

**Reviewed for:** The one-line `rationale` on each requirement entry.

### State the Measure on a Counted Requirement

**ID:** `litespec/core/intent#state-the-measure-on-a-counted-requirement`

**Statement:** A requirement whose statement names a number MUST carry `measure` and `dueBy`.

**Rationale:** A number without its measure cannot be checked: the team cannot tell what is counted or when the count holds. The `measure` names what is counted, and `dueBy` names the date or milestone. The schema pairs the two, so a requirement carries both or neither. A requirement carrying `measure` needs a `check` and a passing run, and a statement naming a number with no `measure` reports at check level A.

**Checked by:** `litespec model check` reports `requirement-measure-missing` for a requirement carrying `measure` without `check` or a passing run, and for a statement naming a number with no `measure`.

### State Each Quality Requirement as a Scenario

**ID:** `litespec/core/intent#state-each-quality-requirement-as-a-scenario`

**Statement:** A quality requirement MUST state the moment it holds for in `stimulus` and `environment`, what the product does in `response`, how that is measured in `measure` and `dueBy`, and how that is confirmed in `check`, with `target`, `conditions`, `runsOn`, and `evidence` where they apply.

**Rationale:** "Fast", "secure", or "always available" cannot be built to or checked. A quality requirement holds only for a moment that can happen: something arrives, under some condition, and the product answers within a measure. Written that way, it tells the builder what to design for and tells a test or a probe what to look for, and a later change in the moment (a bigger on-sale, a new attack) visibly reopens the target. The four fields follow the quality attribute scenario the architecture literature uses, so a reviewer can compare one target with another. The `check` names the probe or test that confirms the measure, and it is required where the requirement carries `measure`. The `target` names the number aimed for, `conditions` names what holds while measuring, `runsOn` names where the check runs, and `evidence` names what the run leaves behind; each applies where the confirmation needs it.

**Example:** `requirement/spending.month-end-submissions` has `kind: quality`, `stimulus` "Five hundred employees submit expenses in the last hour of the month", `environment` "Normal operation, one application instance down", `response` "Each submission is stored and acknowledged", `measure` "Share of submissions acknowledged within two seconds: at least 99 percent", `dueBy` "Pilot launch", and `check` "month-end acknowledgement probe".

**Checked by:** `litespec consumer` fails a quality requirement missing `stimulus`, `environment`, `response`, `measure`, `dueBy`, or `check`.

### Restrict Requirement Fulfillment to Behavior

**ID:** `litespec/core/intent#restrict-requirement-fulfillment-to-behavior`

**Statement:** A requirement MUST be fulfilled only from Behavior records.

**Rationale:** The records that fulfill are rule, use case, reaction, job, notification, and setting. A journey names the requirements it shows in `cites`; it never fulfills, so a journey alone never clears an unfulfilled requirement. Behavior points up to the promises it keeps, and each promise traces down to the behavior that keeps it.

**Checked by:** `litespec model check` reports `requirement-fulfillment-missing` for a requirement no Behavior record fulfills.

### Order Each Journey Toward One Goal

**ID:** `litespec/core/intent#order-each-journey-toward-one-goal`

**Statement:** A journey MUST walk one actor toward one goal through ordered steps, naming the goal record in `serves` and stating `betweenSteps` in fields.

**Rationale:** A journey names its `actor`, its goal record in `serves`, its `finishedWhen`, and its ordered `steps`. One actor means a step names another actor only at a handoff; one goal means a walk with several goals splits into several journeys. The order shows how the steps join toward the goal, so a step that fits nowhere is visible. The journey names in `cites` the requirements it carries, and the use cases, reactions, and jobs that serve each step point up with `serves`. `betweenSteps` holds branch order only, and Example in the explanation file shows one concrete occasion. Proposed by [decision/0005-journeys-serve-a-goal](decision/0005-journeys-serve-a-goal) on the evidence in [research/intent-hierarchy-evidence](research/intent-hierarchy-evidence), [research/cross-layer-trace-evidence](research/cross-layer-trace-evidence), and [research/established-traceability-practice](research/established-traceability-practice).

**Example:** The largest consumer held a 21-step journey with five actors, from creating the organizer to closing the books. Split by goal, it is four journeys: set up and sell an event; buy a ticket; admit the crowd; close the night.

**Checked by:** `litespec consumer` fails a journey record file missing its `actor`, `serves`, `description`, `finishedWhen`, `steps`, or `betweenSteps`.

### Describe Every Journey

**ID:** `litespec/core/intent#describe-every-journey`

**Statement:** A journey MUST state in `description`, in up to four sentences, who walks, what starts the walk, what good looks like, and why it matters.

**Rationale:** A journey page opens with its approval facts and its steps, so a reader landing on it cold learns the mechanics before the purpose. The description states the walk in the walker's terms first, and the book renders it first on the page. `finishedWhen` stays the checkable end state; the description carries the purpose no field checks.

**Example:** The library journey to borrow and return describes a reader who holds a copy, starting at the shelf and ending with the copy back on its shelf, because a reader plans the next visit around the return.

**Checked by:** `litespec consumer` fails a journey record file missing `description`.

### Perform Each Journey Step with an Actor or a System

**ID:** `litespec/core/intent#perform-each-journey-step-with-an-actor-or-a-system`

**Statement:** A journey step MUST name at most one performer: another actor in `actor` at a handoff, or the external system, job or reaction in `system` when no person acts, and a step naming neither is performed by the walking actor.

**Rationale:** A step performed by machinery forced a machine actor into Intent, where it sat beside real people and held no role. Naming the system record that performs the step keeps the actor list to people and organizations, and keeps the performing party one link away. A step naming both an actor and a system names two performers for one act.

**Example:** In the library journey, the step that sends the overdue notice names the text gateway in `system`; the steps the reader walks name no performer.

**Checked by:** `litespec consumer` fails a journey step carrying both `actor` and `system`; `litespec model check` reports `field-invalid` for such a step or for a `system` naming no external system, job or reaction.

### Keep Behavior Out of Intent

**ID:** `litespec/core/intent#keep-behavior-out-of-intent`

**Statement:** An Intent record MUST NOT describe states, results, codes, transactions, edges, consistency modes, Behavior field names, or code.

**Rationale:** An Intent record that names Behavior detail becomes a second specification, and it drifts from the first with the next Behavior change. Two specifications force every reader to ask which one the code follows. Behavior records own behavior and point up to the requirements and steps they serve, so each fact has one home.

**Reviewed for:** Intent records under `.litespec/intent/` and `.litespec/intent/research/` free of the detail the Statement names.

### Treat Generated Research as Source Material

**ID:** `litespec/core/intent#treat-generated-research-as-source-material`

**Statement:** An agent MUST treat records generated outside `docs/` as source material until a research record or an approved decision states the claim.

**Rationale:** Records generated outside `docs/` mix verified facts, inferences, and recommendations in one confident voice. Treating them as source material with no authority keeps a recommendation from being built as if someone decided it. A research record or an approved decision restates the claim before anything rests on it.

**Reviewed for:** A research record or an approved decision behind each position built from generated records.

### State Each Exception with Its Fallback

**ID:** `litespec/core/intent#state-each-exception-with-its-fallback`

**Statement:** A requirement that names an `except` condition MUST name its `fallback` requirement in the same entry.

**Rationale:** An exception without a fallback leaves the reader at the exact moment the promise breaks: the product refuses with no stated alternative. The `condition` states in product words when the commitment does not hold, and `fallback` names the requirement that covers what happens then. The fallback carries no `except` of its own, so the chain stays one deep and every exception ends in a promise.

**Example:** A reimbursed expense lands in the employee's account, never more than they spent. Except when the payout bank's settlement window has closed for that batch, with `fallback` naming the requirement that has finance repay the employee by other means.

**Checked by:** `litespec model check` reports no `except-fallback-missing` finding.

### Write Journey Steps as Sentences with Ids

**ID:** `litespec/core/intent#write-journey-steps-as-sentences-with-ids`

**Statement:** Each journey step MUST carry its `id` and its product sentence in `statement`, and name no use case.

**Rationale:** A step written as a product sentence reads in journey detail with no Behavior knowledge: the decider approves what a person experiences, not which handler runs. The `id` (`s1`, `s2`, and so on) gives Behavior a stable target: use cases, reactions, and jobs point up with `serves` to `journey/<name>#s1`. A step that names its use case drags Behavior into Intent and breaks the journey detail every time Behavior renames. The performer and the failure text are optional: a step names the walking actor only at a handoff in `actor`, or the system record performing it in `system` when no person acts, and carries `failure` where the walk can fail.

**Example:** Step `s4` states `Finance reimburses the approved expense.` in `statement` and names no use case; `use-case/spending.reimburse-expense` names `journey/submit-and-reimburse#s4` in `serves`.

**Checked by:** `litespec consumer` fails a journey step missing `id` or `statement`, or naming a use case.

### State When Each Journey Finishes

**ID:** `litespec/core/intent#state-when-each-journey-finishes`

**Statement:** A journey MUST state its observable end in `finishedWhen`.

**Rationale:** A journey without a stated end can stop anywhere: the reader cannot tell a finished walk from an abandoned one, and serving records cannot tell done from dropped. `finishedWhen` states the observable end in one sentence, in product words a decider can check.

**Example:** `finishedWhen`: the employee holds the reimbursement.

**Checked by:** `litespec consumer` fails a journey record file missing `finishedWhen`.

### Group Questions into Ballots

**ID:** `litespec/core/intent#group-questions-into-ballots`

**Statement:** Each draft question MUST name its `ballot`.

**Rationale:** A question without a decider and a date waits on nobody: work below it guesses while nobody is asked. The `ballot` groups questions in the inbox by a decision id or a short topic slug, so the decider answers one ballot at a time. The question lifecycle lives once in `litespec/core/model#answer-each-question-on-its-record`.

**Example:** Ballot `approval-terms`, due 2026-11-01: above which amount an expense needs a receipt, recommended EUR 25 as a policy setting; and within how many business days each submitted expense is decided, recommended 5, counted from submission.

**Checked by:** `litespec consumer` fails a draft question missing its `ballot`.

### Leave Business Choices to the Decider

**ID:** `litespec/core/intent#leave-business-choices-to-the-decider`

**Statement:** The decider MUST decide each draft question that sets a decider-set figure.

**Rationale:** Agents draft Intent and the decider decides it: a price or term chosen in Behavior would bind the business without its decider. Each figure the [model page](model.md) lists as decider-set travels up as a draft question with the decider named, so Behavior never silently fixes what the business owns. Money and personal-data questions carry `risk` and need explicit approval. The founder is one decider a project names.

**Example:** The receipt threshold moves from EUR 25 to EUR 50 when the decider answers the ballot; the policy setting and its scenarios follow the approved requirement.

**Reviewed for:** The decider's answer on each business draft question beside its `approvedBy`.

### Treat Tenant-Set Values as Data

**ID:** `litespec/core/intent#treat-tenant-set-values-as-data`

**Statement:** A consumer MUST treat a `setBy: tenant` value as tenant data outside decider approval.

**Rationale:** Tenants choose their own values within the ranges Intent sets: a tenant holds its receipt threshold at EUR 25 by default within the EUR 0 to EUR 500 range. The decider approves the requirement that sets the range and the default, and each tenant's choice inside it needs no ballot. Treating tenant values as approvals would ask the decider to approve choices that are never theirs.

**Example:** The tenant sets a receipt threshold of EUR 50; the requirement covering threshold ranges already holds the decider's approval, so the threshold itself is data.

**Reviewed for:** Each `setBy: tenant` value stored as tenant data with no decider approval beside it.

## Conventions

### Group Product Records by Area

**ID:** `litespec/core/intent#group-product-records-by-area`

**Default:** Keep the brief at `.litespec/intent/brief.yaml`, topic record files in one folder per area, each other kind in its folder, and research under `.litespec/intent/research/`.

**Replacement:** A project records its own product layout in its documentation index.

**Rationale:** An area folder keeps related pages together, so the pages of one product area sit side by side. One folder per kind keeps each record findable: goals, problems, features, constraints, decisions, journeys, and actors each have one home. A newcomer learns the layout once and predicts where the next record lives. An area names a product area, never the company: a folder named `business`, `company`, `go-to-market`, `markets` or `roadmap` holds company operation, which [Keep Company Operation Out of Intent](#keep-company-operation-out-of-intent) keeps out of Intent.

**Example:**

```text
.litespec/intent/
  brief.yaml
  spending/
    approval.yaml
  actors/
    <name>.yaml
  constraints/
    <name>.yaml
  decisions/
    0001-receipt-threshold.yaml
  goals/
    <name>.yaml
  problems/
    <name>.yaml
  features/
    <name>.yaml
  journeys/
    <name>.yaml
  research/
    <name>.yaml
```

**Reviewed for:** The `.litespec/intent/` layout beside the project's recorded layout.

### Name Product Decisions in One Numbered Sequence

**ID:** `litespec/core/intent#name-product-decisions-in-one-numbered-sequence`

**Default:** Name each product decision `<four-digit number>-<name>.yaml` and use `decision/<four-digit number>-<name>` as its identifier.

**Replacement:** A project records its own decision naming in its documentation index.

**Rationale:** One numbered sequence orders each decision by when the choice was proposed, giving every record a short stable name. The number counts as a word, so digits never stand alone in the identifier. Scattered names would force every citation to carry the full path.

**Reviewed for:** Each decision file named `<number>-<name>.yaml` beside its matching identifier.

## Reference Example

This informative example demonstrates `litespec/core/intent#order-each-journey-toward-one-goal`, `litespec/core/intent#write-journey-steps-as-sentences-with-ids`, `litespec/core/intent#state-when-each-journey-finishes`, `litespec/core/intent#identify-every-requirement`, `litespec/core/intent#state-each-exception-with-its-fallback`, `litespec/core/intent#group-questions-into-ballots`, `litespec/core/intent#leave-business-choices-to-the-decider`, and `litespec/core/intent#treat-tenant-set-values-as-data`.

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

Consequences (context, not part of what you approve)
- New refusal an employee can meet: "This expense needs a receipt."
- Personal data kept: the employee's name on the reimbursed expense line,
  for the statutory seven years.

Open questions (ballot "Approval terms", due 2026-11-01)
1. Above which amount must an expense carry a receipt?
   Recommended: EUR 25, held as a policy setting.
2. Within how many business days is each submitted expense decided?
   Recommended: 5, counted from submission.
```

For step 4, the serving use case `use-case/spending.reimburse-expense` contributes its goal in one line, and its scenarios contribute their Then clauses as context. The decider approves the promises or answers the ballot from this record detail alone.
