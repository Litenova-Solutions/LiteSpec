# Writing

## Intent

Writing states how every project writes its records, explanation files and documentation pages, so a reader who arrives cold on one page still learns who acts, what is at stake and what holds. Authors and agents apply it to every record value, explanation file, document, code comment, changelog and decision record. The [Record Language](language.md) page states the names, values and codes of project records; this page states the prose.

## Concepts

### Title Forms

Every record title uses Title Case, carries no final period, and holds at most eight words. The title names its subject literally, in the one form its record type takes.

| Record type | Title form | Example |
|:---|:---|:---|
| problem | A clause naming who has the pain, then the pain | Employees Wait Weeks for Reimbursement |
| feature | A noun phrase naming the capability | Receipt Capture with Offline Upload |
| goal | An outcome noun phrase; the number and date stay in fields | Reimbursement Within Five Working Days |
| journey | A verb plus its object: the walker's task | Submit an Expense Claim with Receipts |
| actor | A role noun phrase | Approver |
| requirement | A noun phrase naming the assertion | Per-Diem Meal Allowance |
| constraint | A subject noun phrase; the authority stays in its field | Receipt Retention Period per Country |
| decision | The chosen outcome as a noun phrase | One Approval Chain per Cost Center |
| research | The question the record answers | How Long Employees Wait for Reimbursement |
| use case, job, runbook | Verb first | Approve Expense Claim |
| rule | The assertion as a clause | A Claim Never Exceeds Its Policy Limit |
| screen | The place name the user sees | Claim Review Queue |

Eight words is the bound because a title stands alone in book lists, outlines, tabs and search results, where a longer title wraps and stops scanning as one line. The longest form, a problem clause naming who and the pain, fits inside eight words; a title that needs more is stating its statement in the title. Identifiers, acronyms and words with inner capitals keep their written form, and each major part of a hyphenated word is capitalized.

### Explanation Sections

An explanation file beside a record file uses the fixed sections of its record type, in this order, with the record title as its one H1. A section marked optional is removed when it has nothing to say; every other section stays, even when short.

| Record type | Sections, in order |
|:---|:---|
| problem | Who Has It, What They Do Today, Evidence, What Would Retire It |
| journey | Who Walks and Why, Before It Starts (optional), Example, What It Connects To, Open Questions |
| feature | Who It Serves and Why, What It Offers, What Good Looks Like, Open Questions |
| goal | Why It Matters, What Counts as Done, What It Connects To, Open Questions |
| actor | Who They Are, What They Do, What They Can Reach, Open Questions |
| decision | Context, Options Considered, Decision, Evidence with Confidence (optional), What Would Reopen It |
| research | Answer, Notes on Sources (optional) |
| topic | The Situation, What We Do, Why It Is Better, Example, Costs and Risks, How Sure We Are |

### Documentation Kinds

A documentation page does one of four jobs. A page that tries to do two jobs serves neither reader well, so each kind links to the kind that holds the rest.

| Kind | The reader wants to | The page opens with |
|:---|:---|:---|
| Tutorial | Learn by doing, on one path to a first working result | The one working result the reader holds at the end |
| How-to | Reach one goal they already have | The one problem the page solves, for a reader who already has a working result |
| Reference | Look up one exact fact | The values the page publishes and who looks them up |
| Explanation | Understand why a thing is built this way | What the subject does, then why it matters |

A README is the first screen of its folder: it states what the folder holds, who reads it, and where each kind of reader goes next, before it lists any file. Every documentation page states its reader and its purpose in its first lines, so a reader who lands from a search result or a link knows within two sentences whether the page answers their question.

### Code Documentation

A code comment states why the code is as it is, or what the code cannot express on its own. API documentation states the contract a caller relies on: what a member does, what it needs, what it returns, and which failures are part of that contract. The form that contract takes in one stack lives on that stack's pages: the .NET stack states its XML documentation form on its own pages, and this page adds no second form beside it.

## Standards

### Write for the Reader Who Arrives Cold

**ID:** `litespec/core/writing#write-for-the-reader-who-arrives-cold`

**Statement:** Every record, explanation section and documentation page MUST open by naming who it concerns and what is at stake, before any pointer to another page.

**Rationale:** Most readers land on one page from a list, a search result or a link, with nothing read before it. A page that opens with its subject and its stake answers that reader on its own; a page that opens with a pointer sends the reader elsewhere before it has given them anything. The tone throughout is a business explaining itself to its owner: plain words, checkable facts and honest roles, with no selling and no decoration.

**Example:** A Claimwick problem opens "Employees wait three weeks for reimbursement while approvers batch decisions monthly." A reader who has opened no other page knows who waits and what it costs them.

**Reviewed for:** Which records, sections and pages open with a pointer, a code or a field name instead of who and what is at stake? Look at the first sentence of each page the change adds.

### Put the Actor First

**ID:** `litespec/core/writing#put-the-actor-first`

**Statement:** Every sentence in a record MUST name its actor as its subject, in the active voice and the present tense.

**Rationale:** A sentence that opens with its actor states who does what before the reader reaches the verb, and the present tense states what holds today rather than what a plan intends. Passive and future forms hide the actor or promise a behavior nobody has approved. Second person belongs only on tutorial and how-to pages, where the reader is the actor; records stay in the third person, as the text-kind table on the [Record Language](language.md) page states.

**Example:** Claimwick writes "Finance reimburses the approved expense." The actor Finance opens the sentence, and the present tense states what holds.

**Reviewed for:** Which sentences open with a thing instead of an actor, use the passive voice, or promise in the future tense? Look at the statements and summaries the change adds.

### Use Literal Words

**ID:** `litespec/core/writing#use-literal-words`

**Statement:** Every title and sentence MUST use the literal word for what it names, with each term defined on first use or linked to the glossary.

**Rationale:** A metaphor reads smoothly while hiding what the project means: each reader pictures a different act behind the same flourish, and a metaphor title hides its subject in every list that shows it. A literal phrase names the act itself, so no reader has to decode it. Each concept keeps its one glossary term, as `litespec/core/authoring#use-one-term-for-one-concept` states; this page adds only that a term a cold reader may not know is defined where it first appears or linked to its glossary entry.

**Example:** A Claimwick problem is titled "Expenses Wait Weeks for Reimbursement". The title names the wait and the delay literally, with no figure of speech to decode.

**Reviewed for:** Which titles or sentences use a metaphor, a stock phrase or an undefined term where a literal phrase exists? Look at the titles and first uses the change adds, beside the glossary.

### Keep Each Record Sentence Within Twenty-Five Words

**ID:** `litespec/core/writing#keep-each-record-sentence-within-twenty-five-words`

**Statement:** Every sentence in a record `statement`, `summary`, `situation` or `description` MUST hold at most 25 words.

**Rationale:** A record sentence carries one fact a reviewer approves or a builder implements, and a sentence past 25 words usually carries two facts joined by a comma, so the reader weighs both at once or misses the second. Twenty-five words is the longest sentence a reader parses once, and the bound is mechanical, so a check counts it the same way on every record. A longer explanation belongs in the explanation file, split into sentences that each carry one fact.

**Example:** "Employees wait three weeks for reimbursement while approvers batch decisions monthly." The sentence holds 11 words and one fact.

**Checked by:** `litespec model check` reports `value-invalid` for a `statement`, `summary`, `situation` or `description` sentence over 25 words.

### Give Each Paragraph One Idea

**ID:** `litespec/core/writing#give-each-paragraph-one-idea`

**Statement:** Every paragraph in an explanation file or document MUST carry one idea, on one line in Markdown, with a list only where the items are truly parallel.

**Rationale:** A paragraph that holds a rule, an exception and a fallback in one block forces the reader hunting for one of them to untangle all three. One idea per paragraph puts each fact where a scanning reader finds it, and one line per paragraph keeps diffs readable, so a changed sentence shows as one changed line. A list drawn from prose that is not parallel hides the reasoning that connects its items; connected reasoning stays in prose, and only parallel items become a list.

**Example:** The Claimwick topic page gives Costs and Risks its own paragraph. A reader looking for what the choice gives up finds that paragraph without reading the example above it.

**Reviewed for:** Which paragraphs carry two ideas, wrap across hard line breaks, or list items that are not parallel? Look at the explanation files and documents the change adds.

### Apply Plain Mechanics

**ID:** `litespec/core/writing#apply-plain-mechanics`

**Statement:** Every page MUST use American spelling, the serial comma, numerals for numbers of 10 and above and for every quantity with a unit, ISO dates in fields and long-form dates in prose, straight quotes, and ASCII characters only.

**Rationale:** One mechanical form per fact keeps every search, diff and listing exact: `license` in one page and `licence` in another split every lookup, and a curly quote or a typographic dash renders differently in terminals, generated context and plain-text tools. Numerals for measured quantities keep a count scannable beside its unit, while small counts in prose stay words a reader absorbs without stopping. An ISO date in a field sorts and checks as data; the same date in prose reads "2 November 2026", in the order a person says it. Contractions belong on documentation pages, where the voice is a person explaining; records hold none, because a record states a fact in its shortest checkable form. ASCII prose follows `litespec/core/authoring#keep-authored-prose-ascii-safe`.

**Example:** Claimwick prose writes "The tenant keeps receipts for 7 years, files them by month, quarter, and year, and closes the period on 2 November 2026." The field beside it holds `2026-11-02`.

**Reviewed for:** Which pages use British spelling, drop the serial comma, spell out a measured quantity, shorten a prose date, or carry a contraction inside a record? Look at the prose the change adds.

### Write Every Record Title in Title Case

**ID:** `litespec/core/writing#title-every-record-in-title-case`

**Statement:** Every record title MUST use Title Case, carry no final period, and hold at most eight words.

**Rationale:** A title is the record's name in book lists, outlines, tabs and search results, so it reads as a name, never as a sentence: Title Case capitalizes every major word and lowercases a, an, the, and, but, or, nor, for, so, yet, as, at, by, in, of, off, on, per, to, up, via, with, from, into, over and than unless the word stands first or last. One casing rule for every title is the only casing rule a check enforces the same way everywhere; mixed casing makes two records of one kind read as two kinds. Eight words keeps the whole title on one scannable line in a list while leaving room for the longest form, a problem clause naming who and the pain. LiteSpec headings follow the same casing under `litespec/core/authoring#use-controlled-capitalization`.

**Example:** The Claimwick requirement title reads "Expense Above Threshold Carries Receipt", five words in Title Case with no final period.

**Checked by:** `litespec model check` reports `value-invalid` for a record title outside Title Case, ending in a period, or over eight words.

### Name Each Title in Its Record Type Form

**ID:** `litespec/core/writing#name-each-title-in-its-record-type-form`

**Statement:** Every record title MUST name its subject literally in the one title form the title-form table gives its record type.

**Rationale:** One form per type makes a list of mixed records scannable: the form tells the reader what kind of thing the record is before the title is weighed, and a literal subject tells them what it is about. A feature titled with one word, a goal titled with a milestone code, or a problem titled with a metaphor gives the reader neither, and a problem title that names no person leaves the reader guessing who has the pain. The number and date of a goal stay in its fields, where a check reads them; the title names the outcome alone.

**Example:** A Claimwick feature is titled "Approval Reminders for Waiting Expenses". The noun phrase names the capability; no date, milestone or promise sits in the title.

**Reviewed for:** Which titles miss the form of their type, name no subject, or carry a code, date or metaphor? Look at the titles the change adds, beside the title-form table.

### State Each Statement Without Modal Words

**ID:** `litespec/core/writing#state-each-statement-without-modal-words`

**Statement:** Every record `statement` MUST hold one sentence in the present tense with no modal word.

**Rationale:** A statement states what holds, as a fact a reviewer approves and a builder implements. A modal word such as must, should, shall, may, can or will turns the fact into an intention, a permission or a promise, and each reader grades its strength differently. One sentence keeps one commitment per statement, so a change to one commitment reopens one approval. The value style on the [Record Language](language.md) page states the same form; this provision states the prose reason and the check covers every record type.

**Example:** Claimwick states "An expense above the threshold carries a receipt." The sentence states what holds, with no modal word grading it.

**Checked by:** `litespec model check` reports `field-invalid` for a modal word in a `statement` outside a requirement or rule, and `value-invalid` for one inside a requirement or rule.

### Open Each Summary with Who and What Is at Stake

**ID:** `litespec/core/writing#open-each-summary-with-who-and-what-is-at-stake`

**Statement:** Every `summary`, `situation` and `description` MUST restate who it concerns and what is at stake in its first sentence, with any pointer to another page after that content.

**Rationale:** These fields are the lead a cold reader meets in the book, before any field table or link. A first sentence that restates who and what is at stake lets that reader judge the record without opening its links; a bare pointer such as "described on the launch page" gives them nothing to judge and breaks the moment the pointed page moves. A pointer that follows the content helps the reader who wants more; a pointer that replaces the content helps no one.

**Example:** A Claimwick problem `situation` opens "Employees wait three weeks for reimbursement while approvers batch decisions monthly." Who waits and what it costs stand in the first sentence, and any link follows after it.

**Reviewed for:** Which summaries, situations and descriptions open with a pointer, a field name or background instead of who and what is at stake? Look at the first sentence of each one the change adds.

### Keep Company Facts Out of Intent Prose

**ID:** `litespec/core/writing#keep-company-facts-out-of-intent-prose`

**Statement:** Intent prose MUST state only behavior the application performs, data it holds, or a limit it enforces, with no schedule, effort, headcount, price as a company offer, market fact or launch date.

**Rationale:** Intent records state what the product promises, and a promise mixed with how the company runs buries the product fact a builder or reviewer came for. A launch date in a problem, a headcount in a goal, or a price list in a feature reads as part of the promise and reopens with every company change, even when the product behavior stands still. Company facts enter only as a constraint the application enforces or as dated research, where their form states what they are. The scope boundary itself lives on the [Scope](scope.md) and [Intent](intent.md) pages; this provision states the prose consequence.

**Example:** A Claimwick goal states the reimbursement target and its date in `target` and `dueBy` fields. Its prose names no sprint, no headcount and no launch calendar.

**Reviewed for:** Which Intent records carry a schedule, an effort estimate, a headcount, an offered price, a market fact or a launch date in prose? Look at the Intent records the change adds.

### Give Each Explanation File Its Fixed Sections

**ID:** `litespec/core/writing#give-each-explanation-file-its-fixed-sections`

**Statement:** Every explanation file MUST use the fixed sections of its record type, in the order the explanation-section table gives, with the record title as its one H1.

**Rationale:** A fixed section list answers the same questions on every record of one type, so a reader who learned one problem page reads the next without relearning where the evidence sits. Each list answers what a cold reader needs: who has the need, why it matters, what good looks like, what the record connects to, and what stays open. A problem earns four sections because its reader judges a pain: who has it, what they do today, the evidence for it, and what would retire it. Free sections may follow the fixed ones; they never replace one.

**Example:** The Claimwick problem explanation for waiting expenses runs Who Has It, What They Do Today, Evidence, What Would Retire It. The evidence stands in its own section instead of inside a pointer to another page.

**Reviewed for:** Which explanation files miss a fixed section, reorder the list, or open without their H1 title? Look at the explanation files beside the records the change adds, beside the explanation-section table.

### Write Each Documentation Page as One Kind

**ID:** `litespec/core/writing#write-each-documentation-page-as-one-kind`

**Statement:** Every documentation page MUST do one of the four documentation jobs, open as that kind opens, and state its reader and its purpose in its first lines.

**Rationale:** A tutorial that stops to explain a trade-off loses the reader mid-path, and a reference page that teaches leaves the reader hunting for the value they came to look up. One job per page puts each fact where its reader looks for it, and the opening tells a reader from a search result within two sentences whether this page answers their question. A README earns its first screen the same way: what the folder holds, who reads it, and where each reader goes next, before any file list. The page styles and their sections live in the [Authoring Standard](authoring.md); this provision states the prose each kind opens with.

**Example:** A Claimwick how-to page opens "Remind an approver who has held an expense past three business days." The reader and the one problem solved stand before the first step.

**Reviewed for:** Which pages mix two jobs, open with background instead of their kind's opening, or state no reader and purpose in their first lines? Look at the opening lines of each documentation page the change adds, beside the documentation-kind table.

### Comment on Why, Not What

**ID:** `litespec/core/writing#comment-on-why-not-what`

**Statement:** Every code comment and API summary MUST state why the code is as it is or what the code cannot express, with no commented-out code and no history.

**Rationale:** The code already states what it does, so a comment that restates it adds words a maintainer must keep true without adding knowledge. The reason a boundary exists, the constraint a caller must respect, or the failure a member reports is what the next reader cannot recover from the code alone. History belongs in the commit that made the change, where a reader looking for it finds it; commented-out code is history with no commit to explain it. An API summary states the contract first: what the member does, what it needs, what it returns, and which failures belong to the contract, in the form the stack's pages state.

**Example:** A Claimwick handler comment states why the approval check runs before the payout call. It names no step the code below it already shows, and holds no earlier version of the call.

**Reviewed for:** Which comments restate the code, hold commented-out code, or recount how the code arrived? Look at the comments and API summaries the change adds.

### Write Each Changelog as a Release Comparison

**ID:** `litespec/core/writing#write-each-changelog-as-a-release-comparison`

**Statement:** Every changelog entry MUST state the present behavior, who it affects and what the reader does, comparing with the previous release only inside the changelog.

**Rationale:** A changelog serves a reader deciding whether to upgrade and a reader checking what changed after upgrading, and both need the observable change, its condition and the required action, never an inventory of the work. Stating the present state first lets every other page describe what is true today without carrying a former behavior; the changelog is the one place where the comparison with the previous release belongs. A breaking change, a removal and a security fix each state what stops working, who is affected, and the replacement or action, in the entry itself rather than behind a link.

**Example:** A Claimwick changelog entry states "Submitting an expense without a receipt above the threshold is refused. Employees attach the receipt before submitting." The behavior, the reader and the action stand in the entry.

**Reviewed for:** Which entries inventory files or work, bury their condition or action, or describe a former behavior outside the changelog? Look at the entries the change adds.

### Compare Real Options in Each Decision Record

**ID:** `litespec/core/writing#compare-real-options-in-each-decision-record`

**Statement:** Every decision record MUST compare its real options on their merits in Context and Options Considered, then state the chosen outcome and what would reopen it.

**Rationale:** A decision record exists for a reader who was not there when the choice was made, and that reader needs the forces, the alternatives and the consequences, not the discussion that produced them. Options compared on the same points let a later reader rejudge the choice when the world moves; a record that names only the winner forces the whole argument to run again. What would reopen the choice belongs in the record beside the choice, so a stable decision stays stable until the named condition holds. Any option singled out from the decision carries its own sentence saying why it stands apart.

**Example:** A Claimwick decision compares a receipt threshold of EUR 25 with EUR 50 on refusal rate and approver load, chooses EUR 25, and names a refusal rate above the stated bound as what would reopen it.

**Reviewed for:** Which decision records name no real alternative, compare options on different points, recount the discussion, or omit what would reopen the choice? Look at the decision records the change adds.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/core/writing#write-for-the-reader-who-arrives-cold`, `litespec/core/writing#use-literal-words`, `litespec/core/writing#give-each-explanation-file-its-fixed-sections`, and `litespec/core/writing#open-each-summary-with-who-and-what-is-at-stake`.

A Claimwick problem states the pain of waiting for reimbursement. Its title reads "Employees Wait Weeks for Reimbursement", its `situation` opens with who waits and what the wait costs, and its explanation file runs Who Has It, What They Do Today, Evidence, What Would Retire It. The record needs no other page open beside it: the sufferer, the workaround in use today, the evidence and the retirement condition each stand in their fixed place.
