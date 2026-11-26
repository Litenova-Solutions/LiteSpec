# Writing

**Stack:** any
**Concern:** workspace

This standard states how every project writes its records, explanation files, and documentation pages, so a reader who lands on one record or page with nothing read before it still learns who acts, what is at stake, and what holds. Authors and agents apply it to every record value, explanation file, document, code comment, changelog, and decision record. The [Record Language](language.md) page states the names, values, and codes of project records; this standard states the prose.

## Concepts

### Title Forms

A title is the record's name wherever records are listed: book lists, outlines, tabs, and search results. Every title uses Title Case, carries no final period, holds at most eight words, and names its subject literally in the one form its record type takes.

| Record type | Title form | Example |
|:---|:---|:---|
| problem | A clause naming the failure or its consequence, never a theme or a feature | Expenses Wait Weeks |
| feature | A noun phrase naming the capability | Receipt Capture with Offline Upload |
| goal | An outcome noun phrase; the number and date stay in fields | Reimbursement Within Five Working Days |
| direction | A noun phrase naming the way the product may grow; no date or priority | Contractor Expenses |
| flow | A verb phrase naming the actor's task | Submit an Expense Claim with Receipts |
| step | A verb phrase naming the act | Submit Expense |
| actor | A role noun phrase | Approver |
| requirement | A phrase naming the specific behavior or fact it requires, never a bare noun | Expense Above Threshold Carries Receipt |
| constraint | A subject noun phrase; the authority stays in its field | Receipt Retention Period per Country |
| decision | The chosen outcome as a noun phrase | One Approval Chain per Cost Center |
| research | The question the record answers | How Long Employees Wait for Reimbursement |
| use case, job, runbook | A verb phrase naming the act | Approve Expense Claim |
| rule | The assertion as a clause | A Claim Never Exceeds Its Policy Limit |
| scenario | A phrase naming the case shown | Expense Above Threshold Without Receipt |
| result | A phrase naming what is now true or what the caller is told | Expense Reimbursed |
| refusal | A phrase naming the refused state | Receipt Missing |
| domain event | A noun phrase naming what happened | Expense Submitted |
| reaction | A verb phrase naming the act and its trigger | Send Receipt Reminder on Expense Submitted |
| notification | A noun phrase naming the message | Receipt Reminder |
| entry point | A noun naming the entry point | Webshop |
| screen | The place name the user sees | Claim List |
| read-model | A noun phrase for what it keeps one row per | Reimbursed Expense |

Eight words is the bound because a title stands alone where it is listed: a longer title wraps and stops scanning as one line. The longest form, a problem clause naming the failure and its consequence, fits inside eight words; a title that needs more is putting its statement in the title.

Identifiers, acronyms, and words with inner capitals keep their written form, and each major part of a hyphenated word is capitalized.

### Explanation Sections

An explanation file holds the prose beside a record file: it says what the record means, why it matters, and how it reads beside its neighbors. It opens with the record title as its one H1 and an opening paragraph, then uses the fixed sections of its record type, in this order. A project lists in `explanations.layers` the layers whose every record file carries one: `intent`, `behavior` and `governance`. Entries stored inside a parent file carry no explanation file of their own.

A section marked optional is removed when it has nothing to say; every other section stays, even when short. Each section is named by a plain noun, so a reader finds the same kind of content under the same heading on every record of one type. Each section restates and elaborates its backing fields, and an `Example` section may only use details from the cast or from cited records (`litespec/core/writing#take-each-example-detail-from-the-cast-or-a-cited-record`). An Example section holds at most 120 words. A direction's explanation file is free-form: an H1 with the record title, a lead paragraph, then any body the owner wants, with or without H2 sections.

| Record type | Sections | Backing fields per section |
|:---|:---|:---|
| product | Scope, Users, Out of Scope | Scope: boundary; Users: users; Out of Scope: excludes |
| actor | Responsibilities, Knowledge, Example (optional) | Responsibilities: responsibilities; Knowledge: knowledge; Example: none, illustration only |
| role | Access, Excludes | Access: scope, and the permissions naming the role in `grantedTo` (computed); Excludes: excludes |
| goal | Target, Measurement | Target: target; Measurement: measure, observations |
| direction | None: free-form body after the lead | None: free-form body after the lead |
| problem | Affected Users, Failure Cases, Example (optional), Current Workaround, Cost, Evidence, Resolution Criteria | Affected Users: affects; Failure Cases: failureCases; Example: none, illustration only; Current Workaround: workaround; Cost: cost; Evidence: evidence; Resolution Criteria: resolutionCriteria |
| constraint | Scope, Rules, Effect on the Product, Open Points (optional) | Scope: coverage; Rules: obligations; Effect on the Product: productEffect; Open Points: openPoints |
| research | Findings, Open Questions (optional), Source Notes (optional) | Findings: findings; Open Questions: openQuestions; Source Notes: sources (their `note`) |
| decision | Context, Options, Decision, Consequences, Evidence (optional), Revisit Conditions | Context: context; Options: options; Decision: choice; Consequences: consequences; Evidence: basedOn; Revisit Conditions: reopenedWhen |
| feature | Capabilities, Success Criteria, Boundaries, Example (optional) | Capabilities: the feature's requirements; Success Criteria: successCriteria; Boundaries: excludes; Example: none, illustration only |
| requirement | Example (optional) | Example: none, illustration only |
| flow | Example, Branches (optional) | Example: none, illustration only; Branches: steps.branches |
| module | Covers, Boundaries, Example (optional) | Covers: covers; Boundaries: excludes; Example: none, illustration only |
| aggregate | Lifecycle, Data and Retention, Example (optional), Excludes (optional) | Lifecycle: states, transitions; Data and Retention: data, dataClasses; Example: none, illustration only; Excludes: excludes |
| use-case | Flow, Example (optional), Edge Cases (optional) | Flow: steps; Example: none, illustration only; Edge Cases: edgeCases |
| rule | Rationale, Example (optional), Limits (optional) | Rationale: rationale, basedOn, cites; Example: none, illustration only; Limits: none, illustration only |
| domain-event | Example (optional) | Example: none, illustration only |
| reaction | Trigger and Effect, Failure Handling, Example (optional) | Trigger and Effect: triggeredBy, effects, outcomes; Failure Handling: onFailure; Example: none, illustration only |
| job | Invocation, Failure Handling, Example (optional) | Invocation: kind, schedule, runsWhen; Failure Handling: onFailure; Example: none, illustration only |
| read-model | Sources, Freshness, Example (optional) | Sources: fields, keyedBy; Freshness: freshness; Example: none, illustration only |
| notification | Content, Delivery, Example | Content: template, parameters, locale; Delivery: channel, deliveredThrough, frequency; Example: none, illustration only |
| module-event | Consumers, Compatibility, Example (optional) | Consumers: the records that read the event (computed); Compatibility: compatibility; Example: none, illustration only |
| integration-event | Consumers, Compatibility, Example | Consumers: the records that read the event (computed); Compatibility: compatibility; Example: none, illustration only |
| external-system | Use, Data Shared, Failure Handling | Use: summary; Data Shared: processes, erasure; Failure Handling: operations |
| entry-point | Audience, Authentication, Excludes (optional) | Audience: audience; Authentication: authentication, session, reauthenticationTriggers; Excludes: excludes |
| screen | Task, States, Example | Task: task; States: displayStates; Example: none, illustration only |
| permission-set | Risk, Boundaries (optional) | Risk: the risk of each permission; Boundaries: excludes |
| setting | Defaults, Changes (optional) | Defaults: default, range, unit; Changes: setBy |
| runbook | Prerequisites, Steps, Verification, Recovery | Prerequisites: prerequisites; Steps: procedure; Verification: verification; Recovery: recovery |
| waiver | Provision, Reason, Risk, Expiry | Provision: waives; Reason: reason; Risk: consequences; Expiry: expiresOn, exitWhen |

A decision record compares its options with pros and cons, as `litespec/core/intent#record-each-decision-option-with-pros-and-cons` states, with its context and choice as `litespec/core/intent#state-each-decision-with-context-and-choice` states.

### Documentation Kinds

A documentation page does one of four jobs. A page that tries two serves neither reader well, so each kind links to the kind that holds the rest.

| Kind | The reader wants to | The page opens with |
|:---|:---|:---|
| Tutorial | Learn by doing, on one path to a first working result | The one working result the reader holds at the end |
| How-to | Reach one goal they already have | The one problem the page solves, for a reader who already has a working result |
| Reference | Look up one exact fact | The values the page publishes and who looks them up |
| Explanation | Understand why a thing is built this way | What the subject does, then why it matters |

A README is the first screen of its folder: it states what the folder holds, who reads it, and where each kind of reader goes next, before it lists any file.

Every documentation page states its reader and its purpose in its first lines, so a reader who lands from a search result or a link knows within two sentences whether the page answers their question.

### Code Documentation

A code comment states why the code is as it is, or what the code cannot express on its own. API documentation states the contract a caller relies on: what a member does, what it needs, what it returns, and which failures are part of that contract. For example, a Claimwick handler comment states why the approval check runs before the payout call, naming no step the code below it already shows.

The form that contract takes in one stack lives on that stack's pages: the .NET stack states its XML documentation form on its own pages, and this standard adds no second form beside it.

## Standards

### Write for the Reader Who Arrives Cold

**ID:** `litespec/core/writing#write-for-the-reader-who-arrives-cold`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The opening paragraph of every record, explanation file and documentation page MUST name who it concerns and what is at stake, before any pointer to another page.

**Rationale:** Most readers land on one page from a list, a search result, or a link, with nothing read before it. A page that opens with its subject and its stake answers that reader on its own; a page that opens with a pointer sends them elsewhere before giving them anything. Each section after the opening answers its own question instead of repeating the opening. The tone throughout is a product explaining itself plainly: plain words, checkable facts, and honest roles, with no selling and no decoration. Proposed by [decision/0057-narrow-the-writing-rules-to-meaning](../intent/decisions/0057-narrow-the-writing-rules-to-meaning/0057-narrow-the-writing-rules-to-meaning.md).

**Example:** A Claimwick problem opens "Employees wait three weeks for reimbursement while approvers batch decisions monthly." A reader who has opened no other page knows who waits and what it costs them.

**Reviewed for:** Does every record, explanation file, and page open with who and what is at stake, with no later section repeating the opening? Check the first sentence of each page the change adds.

### Put the Actor First

**ID:** `litespec/core/writing#put-the-actor-first`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every sentence in a record that describes an action MUST name its actor as its subject, in the active voice and the present tense.

**Rationale:** A sentence that opens with its actor states who does what before the reader reaches the verb, and the present tense states what holds today rather than what a plan intends. Passive forms hide the actor, and future forms promise a behavior no record states. Second person belongs only on tutorial and how-to pages, where the reader is the actor; records stay in the third person, as the text-kind table on the [Record Language](language.md) page states. A sentence that states a fact, a state, or a calculation may take its object or its condition as its subject, as in "The statement closing date determines the comparison date." Passive voice that hides who acts is still a finding. Proposed by [decision/0057-narrow-the-writing-rules-to-meaning](../intent/decisions/0057-narrow-the-writing-rules-to-meaning/0057-narrow-the-writing-rules-to-meaning.md).

**Example:** Claimwick writes "The approver approves the expense within five business days." The actor opens the sentence, and the present tense states what holds.

**Reviewed for:** Does every sentence that describes an action open with its actor, in the active voice and the present tense? Check the statements and summaries the change adds.

### Use Literal Words

**ID:** `litespec/core/writing#use-literal-words`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every title and sentence MUST use the literal word for what it names, with each term defined on first use or linked to the glossary.

**Rationale:** A metaphor reads smoothly while hiding what the project means: each reader pictures a different act behind the same phrase, and a metaphor title hides its subject in every list that shows it. A literal phrase names the act itself, so no reader has to decode it. A loose verb such as handles, holds, touches, or lands hides the act the same way: "one transfer entry touches both accounts" leaves open what happens to each balance, while "one transfer entry reduces the sending account's balance and increases the receiving account's balance" states it. Plain wording keeps the precise term of the domain, such as opening balance or closing date, where a looser word would lose the meaning. A word is loose when it leaves the act unclear in its sentence; a field or domain term used in its defined sense, such as an actor that holds a role, is not loose. A stock phrase is the same loose form: "It matters because the figures must be correct" names no consequence, while "A wrong box amount means a correction return, and a late return means a fine" names the consequence and who bears it. Each concept keeps its one glossary term, as `litespec/core/authoring#use-one-term-for-one-concept` states; this provision adds only that a term a cold reader may not know is defined where it first appears or linked to its glossary entry. Proposed by [decision/0049-literal-flow-words-concrete-consequences](../intent/decisions/0049-literal-flow-words-concrete-consequences/0049-literal-flow-words-concrete-consequences.md) and [decision/0057-narrow-the-writing-rules-to-meaning](../intent/decisions/0057-narrow-the-writing-rules-to-meaning/0057-narrow-the-writing-rules-to-meaning.md).

**Example:** A Claimwick problem is titled "Expenses Wait Weeks". The title names the wait and the delay literally, with no figure of speech to decode.

**Reviewed for:** Does every title and sentence use the literal word, with each term defined or linked? Check the titles, verbs, and first uses the change adds against the glossary.

### Name Observable Behavior Instead of Intensifiers

**ID:** `litespec/core/writing#name-observable-behavior-instead-of-intensifiers`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every record and explanation file MUST state the observable behavior or the measured target in place of an unsupported intensifier or experience claim, such as seamlessly, easily, instantly, at once, or quickest.

**Rationale:** The reader of a record has to build or test what it says. An intensifier reads as a promise while hiding the criterion behind it: "the owner sees at once which account differs" names no behavior a tester can check, and "quickest sign" claims a comparison nobody measured. Stating what the product shows, or the number it meets, gives the builder something to build and the tester something to pass or fail.

**Example:** A Claimwick feature writes "The approver sees each waiting expense with the number of business days it has waited", not "The approver instantly spots late expenses".

**Reviewed for:** Does any sentence use an intensifier or claim an experience where the behavior or a measured target belongs? Check the records and explanation files the change adds.

### Keep Each Record Sentence Within Twenty-Five Words

**ID:** `litespec/core/writing#keep-each-record-sentence-within-twenty-five-words`

**Strength:** advisory

**Applies to:** artifact: any

**Statement:** Every sentence in a record `statement` or `summary` SHOULD hold at most 25 words.

**Deviation:** A longer sentence that keeps a condition the shorter form would drop, as `litespec/core/writing#keep-the-conditions-when-shortening` requires.

**Rationale:** A record sentence carries one fact a builder implements. A sentence past 25 words often carries two facts joined by a comma, so the reader must hold both at once or misses the second. Twenty-five words is a house limit chosen for scanning, not a measured limit of what readers parse, and the count is mechanical so every record is counted the same way. The glossary `meaning` bound is the same bound, checked by `litespec model review` as `term-meaning-invalid` (level R). A longer explanation belongs in the explanation file, split into sentences that each carry one fact. Proposed by [decision/0057-narrow-the-writing-rules-to-meaning](../intent/decisions/0057-narrow-the-writing-rules-to-meaning/0057-narrow-the-writing-rules-to-meaning.md).

**Example:** "Employees wait three weeks for reimbursement while approvers batch decisions monthly." The sentence holds 11 words and one fact.

**Reviewed for:** Does any record statement or summary carry a sentence past 25 words that holds no condition a shorter sentence would drop? Check the statements and summaries the change adds.

### Keep the Conditions When Shortening

**ID:** `litespec/core/writing#keep-the-conditions-when-shortening`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every `summary`, `conclusion`, `statement` and opening paragraph that shortens a claim MUST keep each condition of the claim it shortens, such as where entitled to deduct, for quarterly filers or to the extent allowed.

**Rationale:** A shortened form that drops a condition states a wider claim than its source, and a reader builds or decides against the wider claim. Shortening stays the aim, so a form keeps the words that carry a condition and drops the rest. This rule outranks the sentence bound in `litespec/core/writing#keep-each-record-sentence-within-twenty-five-words` whenever the two pull apart. Proposed by [decision/0054-write-each-shared-record-rule-once](../intent/decisions/0054-write-each-shared-record-rule-once/0054-write-each-shared-record-rule-once.md).

**Example:** "Finance reimburses the expense" states more than Claimwick does. "Finance reimburses the approved expense" keeps the condition the policy holds.

**Reviewed for:** Does every shortened form keep each condition of the claim it shortens? Check the summaries, conclusions, statements, and opening paragraphs the change adds against the claims they shorten.

### Give Each Paragraph One Idea

**ID:** `litespec/core/writing#give-each-paragraph-one-idea`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every paragraph in an explanation file or document MUST carry one idea, on one line in Markdown, with a list only where the items are truly parallel.

**Rationale:** A reader hunting for one fact should not have to untangle a paragraph that holds a rule, an exception, and a fallback in one block. One idea per paragraph puts each fact where a scanning reader finds it, and one line per paragraph keeps diffs readable, so a changed sentence shows as one changed line. A list drawn from prose that is not parallel hides the reasoning that connects its items: connected reasoning stays in prose, and only parallel items become a list.

**Example:** The Claimwick decision explanation gives Revisit Conditions its own paragraph. A reader looking for what would reopen the choice finds that paragraph without reading the example above it.

**Reviewed for:** Does every paragraph carry one idea on one line, with lists only for parallel items? Check the explanation files and documents the change adds.

### Use One Mechanical Form for Each Fact

**ID:** `litespec/core/writing#use-one-mechanical-form-for-each-fact`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every page MUST use American spelling, the serial comma, numerals for numbers of 10 and above and for every quantity with a unit, ISO dates in fields and long-form dates in prose, straight quotes, and ASCII characters except in exact quotations and names, which keep their characters.

**Rationale:** One mechanical form per fact keeps every search, diff, and listing exact: `license` on one page and `licence` on another splits every lookup, and a curly quote or a typographic dash renders differently in terminals, generated context, and plain-text tools. Numerals for measured quantities keep a count scannable beside its unit, while small counts in prose stay words that read without stopping. An ISO date in a field sorts and checks as data; the same date in prose reads "2 November 2026", in the order a person says it. Contractions belong on documentation pages, where the voice is a person explaining; records hold none, because a record states a fact in its shortest checkable form. ASCII is LiteSpec's preference for its own pages, as `litespec/core/authoring#keep-authored-prose-ascii-safe` states; exact quotations and names keep their characters.

**Example:** Claimwick prose writes "The tenant keeps receipts for 7 years, files them by month, quarter, and year, and closes the period on 2 November 2026." The field beside it holds `2026-11-02`.

**Reviewed for:** Does every page use American spelling, the serial comma, numerals for measured quantities, long-form prose dates, and straight quotes, with no contraction inside a record? Check the prose the change adds.

### Write Every Record Title in Title Case

**ID:** `litespec/core/writing#write-every-record-title-in-title-case`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every record title MUST use Title Case, carry no final period, and hold at most eight words.

**Rationale:** A title is the record's name in book lists, outlines, tabs, and search results, so it reads as a name, never as a sentence. Title Case capitalizes every major word and lowercases a, an, the, and, but, or, nor, for, so, yet, as, at, by, in, of, off, on, per, to, up, via, with, from, into, over and than unless the word stands first or last. One casing rule lets a check enforce every title the same way; mixed casing makes two records of one kind read as two kinds. Eight words keeps the whole title on one scannable line in a list while leaving room for the longest form, a problem clause naming the failure and its consequence. LiteSpec headings follow the same casing under `litespec/core/authoring#use-controlled-capitalization`.

**Example:** The Claimwick requirement title reads "Expense Above Threshold Carries Receipt", five words in Title Case with no final period, naming the fact the requirement holds rather than a bare noun such as "Receipts".

**Reviewed for:** Does every record title use Title Case, carry no final period, and hold at most eight words? Check the titles the change adds.

### Write Each Statement as a Fact Without Modal Words

**ID:** `litespec/core/writing#write-each-statement-as-a-fact-without-modal-words`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every record `statement` MUST hold one sentence in the present tense with no modal word.

**Rationale:** A statement states what holds, as a fact a builder implements. A modal word such as must, should, shall, may, can, or will turns the fact into an intention, a permission, or a promise, and each reader grades its strength differently. One sentence keeps one commitment per statement, so a change to one commitment changes one statement. The value style on the [Record Language](language.md) page states the same form; this provision states the prose reason and the check covers every record type.

**Example:** Claimwick states "An expense above the threshold carries a receipt." The sentence states what holds, with no modal word grading it.

**Reviewed for:** Does every record statement hold one present-tense sentence with no modal word? Check the statements the change adds.

### Give Each Explanation File Its Fixed Sections

**ID:** `litespec/core/writing#give-each-explanation-file-its-fixed-sections`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each explanation file MUST hold the fixed sections of its record type whose backing fields hold a value, in the order of the section table, each section explaining the fields the table pairs it with.

**Rationale:** A fixed section list answers the same questions on every record of one type, so a reader who learned one problem page reads the next without relearning where the evidence sits. Each list answers what a cold reader needs: who has the need, what it costs, what the record connects to, and what good looks like. A problem holds its sections because its reader judges a pain: who has it, each way it fails them, what they do instead, what it costs them, the evidence for it, and what shows each failure has ended. Each section is a plain noun, so the heading tells a reader what the section holds before they read it. Each section is the human version of the fields the table pairs it with, so every page of a type keeps the same reading order. A section whose backing fields are all empty on its record has nothing to explain, so the file leaves it out. Free sections may follow the fixed ones; they never replace one. Settled by [decision/0043-use-plain-noun-sections](../intent/decisions/0043-use-plain-noun-sections/0043-use-plain-noun-sections.md), which gives each record type one list of plain noun sections.

**Example:** The Claimwick problem explanation for waiting expenses holds Affected Users, Failure Cases, Current Workaround, Cost, Evidence, Resolution Criteria. The evidence stands in its own section instead of inside a pointer to another page.

**Reviewed for:** Does every explanation file carry its fixed sections in order, with each section backed by filled fields? Check the explanation files the change adds against the section table.

### Open Each Explanation File with a Paragraph

**ID:** `litespec/core/writing#open-each-explanation-file-with-a-paragraph`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every explanation file MUST hold one paragraph of prose between its H1 title and its first H2 section.

**Rationale:** A reader who opens an explanation from a link reads the opening paragraph first and decides from it whether the record concerns them. The paragraph states what the record is in plain words, who it concerns and the consequence at stake, as `litespec/core/writing#write-for-the-reader-who-arrives-cold` states, so the record is understood before any section is read. A heading with no paragraph under it sends the reader straight into a section that assumes context they do not have yet. Proposed by [decision/0049-literal-flow-words-concrete-consequences](../intent/decisions/0049-literal-flow-words-concrete-consequences/0049-literal-flow-words-concrete-consequences.md).

**Example:** The Claimwick problem explanation for waiting expenses opens with one paragraph: employees wait three weeks for reimbursement, approvers batch decisions monthly, and the wait costs the company more than the money it holds. Only then does its Affected Users section begin.

**Reviewed for:** Does every explanation file hold one paragraph of prose between its H1 title and its first H2 section? Check the explanation files the change adds.

### Explain the Same Specification the Records State

**ID:** `litespec/core/writing#explain-the-same-specification-the-records-state`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every explanation file MUST state the same specification as its record, restated and elaborated for a person, with no fact that its record and the records it cites lack and no sentence that contradicts one of their fields.

**Rationale:** The record is the primary source an agent builds from; the explanation is the same specification written for a person who arrives cold. Restating a field in plain words, with its reason, its context and an example, is what makes the record readable, so repetition is expected. A fact that appears only in the explanation splits the specification in two, and a sentence that contradicts a field leaves the reader unsure which to trust. When they disagree, the record wins and the explanation is corrected. Settled by [decision/0095-keep-the-whole-specification-in-the-records](../intent/decisions/0095-keep-the-whole-specification-in-the-records/0095-keep-the-whole-specification-in-the-records.md).

**Example:** A Claimwick feature explanation restates the approval reminders' `offering`, explains why a reminder for expenses waiting past three business days ends the wait that costs employees weeks, and walks through one reminder with the cast. It names no behavior the feature's requirements do not state.

**Reviewed for:** Does any explanation file add a fact its record and the records it cites do not state, or contradict one of their fields? Compare each explanation file with its record.

### Update a Record and Its Explanation Together

**ID:** `litespec/core/writing#update-a-record-and-its-explanation-together`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A change to a record field or to its explanation file MUST change both in the same change, so that the explanation states what the record states.

**Rationale:** An explanation repeats its record on purpose, so every copy drifts unless the change that edits one edits the other. Making the update part of the same change keeps the two equal at every commit, which is when people read them. Settled by [decision/0095-keep-the-whole-specification-in-the-records](../intent/decisions/0095-keep-the-whole-specification-in-the-records/0095-keep-the-whole-specification-in-the-records.md).

**Example:** A Claimwick change that shortens `setting/spending.approval-reminder-delay` from `PT72H` to `PT48H` edits the setting's `default` and the sentence in its explanation that says when a manager gets a reminder, in one commit.

**Reviewed for:** Does a change that edits a record field leave its explanation file stating the old value or behavior? Compare the changed records with their explanation files.

### Take Each Example Detail from the Cast or a Cited Record

**ID:** `litespec/core/writing#take-each-example-detail-from-the-cast-or-a-cited-record`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every name, date and number in an `Example` section MUST come from the cast record, `.litespec/cast.yaml`, or from a record the explained record cites, each appears only where it shows something about that record, and the `Example` states no rule of its own.

**Rationale:** An Example that brings in a figure no record holds shows the reader a number that rests on a rule nobody states, and the reader takes the number for the rule. The cast record is shared across every Example, so one name means one person across the documentation. An Example that states a rule would be a second copy of a record's statement, which drifts. Proposed by [decision/0054-write-each-shared-record-rule-once](../intent/decisions/0054-write-each-shared-record-rule-once/0054-write-each-shared-record-rule-once.md).

**Example:** A Claimwick module example uses the 48.90 EUR fare and the 25 EUR receipt threshold, both as the cast record and the policy record state them, and adds no third amount.

**Reviewed for:** Does every name, date, and number in each Example come from the cast record or a cited record, with no rule of its own? Check the Example sections the change adds against `.litespec/cast.yaml`.

### Keep the Cast Out of Production Code

**ID:** `litespec/core/writing#keep-the-cast-out-of-production-code`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Names and values from the cast record MAY appear in development seed data, demo data, fixtures and tests, and MUST NOT appear in production code paths or production data.

**Rationale:** The cast's people, parties, occasions and amounts are examples. A production path that carries one ships example data to real users, and a test that reads the cast passes on the example rather than on the rule. A seed or fixture that uses a cast entry marks it with `<seeds>party/harbor-and-pine</seeds>` in C# or `// litespec: seeds party/harbor-and-pine` in TypeScript, so the model reads which entries each seed uses and reports a mark that names none. Proposed by [decision/0112-cast-as-a-record](../intent/decisions/0112-cast-as-a-record/0112-cast-as-a-record.md).

**Example:** A seed that writes Ava's train fare into the development database carries `<seeds>person/ava</seeds>` and `<seeds>figure/train-fare</seeds>`, and the production expense handler carries no name or amount from the cast.

**Reviewed for:** Does each cast name or value that appears in code sit in a seed, demo, fixture or test file, with a seeds mark naming its entry? No check reads this yet; review the changed files against `.litespec/cast.yaml`.

### Keep Release Phases and Plans Out of Records

**ID:** `litespec/core/writing#keep-release-phases-and-plans-out-of-records`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every record MUST state the product as it stands, with no release phase, later step or future plan in its prose.

**Rationale:** A phrase such as "in the first release" or "a later step" dates the record the day the release ships, and it promises a behavior no requirement holds. A way the product may grow is a direction, an open choice is an owner question or a backlog item, and a limit of the current product is stated as a limit, not as a plan to lift it. Proposed by [decision/0054-write-each-shared-record-rule-once](../intent/decisions/0054-write-each-shared-record-rule-once/0054-write-each-shared-record-rule-once.md).

**Example:** A permission record states "The role cannot be limited to one business", not that staff will narrow the role in a later release.

**Reviewed for:** Does any record name a release phase, a later step, or a future plan where the product as it stands, a direction, an owner question, or a backlog item belongs? Check the records the change adds.

### Write Each Documentation Page as One Kind

**ID:** `litespec/core/writing#write-each-documentation-page-as-one-kind`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every documentation page MUST do one of the four documentation jobs, open as that kind opens, and state its reader and its purpose in its first lines.

**Rationale:** A tutorial that stops to explain a trade-off loses the reader mid-path, and a reference page that teaches leaves the reader hunting for the value they came to look up. One job per page puts each fact where its reader looks for it, and the opening tells a reader from a search result within two sentences whether this page answers their question. A README earns its first screen the same way: what the folder holds, who reads it, and where each reader goes next, before any file list. The page styles and their sections live in the [Authoring Standard](authoring.md); this provision states the prose each kind opens with.

**Example:** A Claimwick how-to page opens "Remind an approver who has held an expense past three business days." The reader and the one problem solved stand before the first step.

**Reviewed for:** Does every documentation page do one job, open as its kind opens, and state its reader and purpose in its first lines? Check the opening lines of each documentation page the change adds against the documentation-kind table.

### Comment on Why, Not What

**ID:** `litespec/core/writing#comment-on-why-not-what`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every code comment and API summary MUST state why the code is as it is or what the code cannot express, with no commented-out code and no history.

**Rationale:** The code already states what it does, so a comment that restates it adds words a maintainer must keep true without adding knowledge. The reason a boundary exists, the constraint a caller must respect, or the failure a member reports is what the next reader cannot recover from the code alone. History belongs in the commit that made the change, where a reader looking for it finds it; commented-out code is history with no commit to explain it. An API summary states the contract first: what the member does, what it needs, what it returns, and which failures belong to the contract, in the form the stack's pages state.

**Example:** A Claimwick handler comment states why the approval check runs before the payout call. It names no step the code below it already shows, and holds no earlier version of the call.

**Reviewed for:** Does any comment restate the code, hold commented-out code, or recount history? Check the comments and API summaries the change adds.

### Write Each Changelog as a Release Comparison

**ID:** `litespec/core/writing#write-each-changelog-as-a-release-comparison`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every changelog entry MUST state the present behavior, who it affects and what the reader does, comparing with the previous release only inside the changelog.

**Rationale:** A changelog serves a reader deciding whether to upgrade and a reader checking what changed after upgrading. Both need the observable change, its condition, and the required action, never an inventory of the work. Stating the present state first lets every other page describe what is true today without carrying a former behavior; the changelog is the one place where the comparison with the previous release belongs. A breaking change, a removal, and a security fix each state what stops working, who is affected, and the replacement or action, in the entry itself rather than behind a link.

**Example:** A Claimwick changelog entry states "Submitting an expense without a receipt above the threshold is refused. Employees attach the receipt before submitting." The behavior, the reader, and the action stand in the entry.

**Reviewed for:** Does every entry state the present behavior, who it affects, and what the reader does, with no former behavior described outside the changelog? Check the entries the change adds.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/core/writing#write-for-the-reader-who-arrives-cold`, `litespec/core/writing#use-literal-words`, and `litespec/core/writing#give-each-explanation-file-its-fixed-sections`.

A Claimwick problem states the pain of waiting for reimbursement. Its title reads "Expenses Wait Weeks", its `summary` opens with who waits and what the wait costs, and its explanation file holds Affected Users, Failure Cases, Current Workaround, Cost, Evidence, Resolution Criteria.

The record needs no other page open beside it: the sufferer, each failure, the workaround in use today, the evidence, and the outcome that ends each failure stand in their fixed places.
