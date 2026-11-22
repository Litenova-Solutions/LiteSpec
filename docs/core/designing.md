# Designing the Standard

**Stack:** any

**Concern:** workspace

LiteSpec is a record model, a set of checks, a book and a command-line tool that many unrelated products adopt. A maintainer who adds a record type, a field, a check or a folder shapes how every one of those products writes its specification. This page states the design rules that keep those choices consistent: each record holds one thing, a record with reasoning of its own gets its own folder, every shape has a reader, and a shape changes everywhere at once. The rules come from the decisions under `docs/intent/decisions/`, and most of them restate provisions that other pages already own, which this page cites rather than copies.

## Concepts

### Where the Design Rules Come From

Each rule here generalizes choices LiteSpec has already made more than once. Requirements moved into their own folders because an entry in a feature file had no room to explain itself; rules, settings and domain events followed for the same reason. Long rules were split because a rule that held several conditions could not be cited precisely. A rule on this page names the general lesson, so the next record type does not have to learn it again.

### The Rules Already Owned Elsewhere

Most design rules are already provisions on the page that owns their subject. A maintainer designing LiteSpec follows them as written:

| Subject | Provisions |
|:---|:---|
| One source for each fact | `litespec/core/principles#keep-one-authored-source`, `litespec/core/model#derive-a-computed-fact-instead-of-restating-it`, `litespec/core/model#state-what-a-reader-needs-on-the-record-and-check-each-copy` |
| Facts in fields, not prose | `litespec/core/model#keep-the-whole-specification-in-the-records`, `litespec/core/writing#explain-the-same-specification-the-records-state` |
| One parent and one home | `litespec/core/model#give-every-record-one-parent`, `litespec/core/model#place-a-record-by-its-content-and-link-it-by-its-reason`, `litespec/core/model#place-child-records-in-typed-folders` |
| Links | `litespec/core/model#write-links-on-the-record-that-points`, `litespec/core/model#point-every-link-up` |
| Names | `litespec/core/principles#take-every-name-from-the-intent-above-it`, `litespec/core/authoring#use-one-term-for-one-concept`, `litespec/core/language#read-each-field-name-in-its-record-type` |
| Values that vary | `litespec/core/domain-model#cite-varying-values-from-reference-data`, `litespec/core/model#cite-reference-data-instead-of-literal-values` |
| Checks and their levels | `litespec/core/model#keep-shape-rules-in-the-schema`, `litespec/core/model#mark-every-check-with-its-level`, `litespec/core/principles#state-the-strength-of-every-provision` |
| Explanations | `litespec/core/writing#give-each-explanation-file-its-fixed-sections`, `litespec/core/writing#update-a-record-and-its-explanation-together` |
| Tools | `litespec/core/principles#keep-tools-small`, `litespec/core/principles#add-new-machinery-only-for-a-current-requirement`, `litespec/core/model#render-generated-regions-from-the-model` |
| Consumers and releases | `litespec/core/maintaining#derive-a-general-rule-from-a-consumer-need`, `litespec/core/maintaining#keep-consumers-out-of-litespec`, `litespec/core/authoring#exclude-cross-release-compatibility-work`, `litespec/core/maintaining#record-the-reasoning-behind-each-release` |

### A Record and an Entry

A record file holds one record at its top level. An entry is a record held in a list inside another record file: a result or a scenario in its use case, a permission in its module's permission set, a refusal in its refusal set, a person in the cast. An entry completes the record or list it sits in and carries no reasoning of its own. A record that a reader asks "why does this exist?" about, and that grows a rationale, sources, examples or limits, is not an entry.

### The Shape a Record Type Ships With

A record type is more than its schema definition. It ships with a home in the folder table, its explanation sections, its link phrases in both directions, its rows in the field table, its template copied from a Claimwick record, an instance in the Claimwick example, its book page, the checks that read it, and the pages that teach it. A type missing one of these teaches a consumer half a shape.

## Standards

### Hold One Thing in Each Record

**ID:** `litespec/core/designing#hold-one-thing-in-each-record`

**Strength:** required

**Statement:** A record type MUST be shaped so that each record holds one thing: one condition, one commitment, one failure case, one event or one choice, so that a second thing with its own refusal, derived value, dates or proof is a second record.

**Rationale:** A citation, a scenario step or a code mark can rest on exactly the thing it depends on only when each record holds one thing. A record holding several makes every citation vague and reopens all of them when one changes. Settled by [decision/0116-write-the-design-rules-down](decision/0116-write-the-design-rules-down), which generalizes [decision/0114-one-condition-per-rule](decision/0114-one-condition-per-rule).

**Example:** A Claimwick rule that held both "an expense is reimbursed at most once" and "an expense is reimbursed only after approval" is two rules, because each has its own refusal.

**Reviewed for:** Does each record of a new or changed type hold one thing? Read the type's statement field and ask whether a second sentence could carry its own refusal, value or scenario.

### Give a Record with Its Own Reasoning Its Own Folder

**ID:** `litespec/core/designing#give-a-record-with-its-own-reasoning-its-own-folder`

**Strength:** required

**Statement:** A record type whose records carry reasoning of their own, such as a rationale, sources, an example or limits, MUST give each record its own folder holding its record file and its explanation file, and only a closed list whose entries carry no reasoning of their own MAY hold its records as entries in a set or in their parent's file.

**Rationale:** An entry has no explanation file, so its reasons end up in the parent's explanation, mixed with every other entry, or get lost. A shared file also grows with every entry, and a change to one entry rewrites the whole file and its history. Settled by [decision/0115-give-reasoned-records-their-own-folder](decision/0115-give-reasoned-records-their-own-folder), following [decision/0045-keep-each-requirement-in-its-own-folder](decision/0045-keep-each-requirement-in-its-own-folder).

**Example:** A Claimwick rule sits at `aggregates/expenses/rules/reimbursed-once/reimbursed-once.yaml` with its explanation beside it, while the refusal codes the spending module raises stay as entries in its `refusals.yaml`.

**Reviewed for:** Does a new or changed record type that carries a rationale, sources or examples have a named home in the folder table, and does every type left as an entry carry no reasoning of its own? Read `RecordFolders.cs` and the type's schema definition.

### Shape Each Record Type for Its Next Field

**ID:** `litespec/core/designing#shape-each-record-type-for-its-next-field`

**Strength:** advisory

**Statement:** A record type SHOULD be shaped so that adding a field, a link or an explanation section to it changes one record's file, never a whole set of records in one file.

**Deviation:** A closed list whose entries carry no reasoning of their own, such as permissions, refusal codes or cast entries, stays in one file, because its entries grow by count and not by depth.

**Rationale:** LiteSpec keeps adding facts to its record types. A type held in sets makes each addition lengthen every set for every consumer, and a type held one per file lets the record grow where it lives.

**Example:** Adding an optional `limits` explanation section to Claimwick's rules touched only the rules that have limits to state.

**Reviewed for:** Would adding a field to the type change one record's file or a shared one? Read the type's home in `RecordFolders.cs`.

### Give Every Type, Field and Value a Reader

**ID:** `litespec/core/designing#give-every-type-field-and-value-a-reader`

**Strength:** required

**Statement:** A new record type, field or enumeration value MUST have a reader (a check, the book, a command or a citing record), carry no shape an existing type already carries, and go in the same change that removes its last reader.

**Rationale:** A shape nothing reads teaches consumers to write facts no tool will ever hold them to, and a second shape for one concept makes them choose between two answers. Settled by [decision/0025-remove-fields-nothing-reads](decision/0025-remove-fields-nothing-reads) and [decision/0076-remove-shapes-nothing-needs](decision/0076-remove-shapes-nothing-needs).

**Example:** A `priority` field on Claimwick's requirements that no check, book page or citing record reads is not added; a field the book shows on the requirement page is.

**Reviewed for:** Which check, book page, command or citing record reads each new type, field and value, and does an existing type already carry it? Search the tools for the field name.

### Deliver Each Record Type Complete

**ID:** `litespec/core/designing#deliver-each-record-type-complete`

**Strength:** required

**Statement:** A change that adds a record type or moves it to another home MUST ship, in the same change, its schema definition, its home in the folder table, its explanation sections, its link phrases, its field-table rows, its template copied from a Claimwick record, an instance in the Claimwick example, its book placement, the checks that read it, and the pages that teach it.

**Rationale:** A type that ships part of its shape teaches consumers a half: a schema with no home, a home with no template, or a template no example shows. Each missing part is a question every consumer answers on their own. Settled by [decision/0116-write-the-design-rules-down](decision/0116-write-the-design-rules-down).

**Example:** The rule type's move into its own folders shipped the `rulePage` schema definition, its homes, its Rationale, Example and Limits sections, the `rule.yaml` and `rule.md` templates, Claimwick's rules split into folders, and the pages that describe them.

**Reviewed for:** Does the change that adds or moves a record type touch every part this provision lists? Walk the list against the diff.

### Change Every Instance of a Shape in One Change

**ID:** `litespec/core/designing#change-every-instance-of-a-shape-in-one-change`

**Strength:** required

**Statement:** A change to a shape, such as a field, a folder, a section or a name, MUST change every instance of it in LiteSpec in the same change (the schema, the tools, the templates, the Claimwick example, the test fixtures and the pages) and add a check or a validator where one is cheap and sure, so the old shape cannot come back.

**Rationale:** A shape changed in some places and not others leaves two answers to one question, and the next maintainer copies whichever one they find first. LiteSpec ships no compatibility path, so the change is whole or it is not made. Settled by [decision/0116-write-the-design-rules-down](decision/0116-write-the-design-rules-down).

**Example:** When rule sets were removed, the schema, the folder table, the scaffolder, the templates, Claimwick, every test fixture and every page that showed a rule-set file changed together, and the schema rejects a rule-set file.

**Reviewed for:** Does a search of the repository for the old shape's name return nothing after the change? Run `git grep` for the old field, folder or type name.

### State Each Deviation from a Design Rule

**ID:** `litespec/core/designing#state-each-deviation-from-a-design-rule`

**Strength:** required

**Statement:** A change that sets a design rule aside MUST say so in the decision record behind it, naming the provision and why the rule does not fit.

**Rationale:** A rule broken without a word teaches the next maintainer that the rule does not hold. A stated deviation keeps the rule and records its known exception.

**Example:** A decision that keeps Claimwick's refusal codes in one refusal set names `litespec/core/designing#give-a-record-with-its-own-reasoning-its-own-folder` and states that a refusal code carries no reasoning of its own.

**Reviewed for:** Does each decision behind a change that departs from a rule on this page name the rule and the reason? Read the decision's Decision section.

### Update a Design Rule That Keeps Bending

**ID:** `litespec/core/designing#update-a-design-rule-that-keeps-bending`

**Strength:** advisory

**Statement:** When two changes have set the same design rule aside, the next change SHOULD amend the rule on this page, or record why it still holds, rather than set it aside a third time.

**Deviation:** A rule set aside for two unrelated reasons, each recorded, may stand until a third reason shows the pattern.

**Rationale:** A rule bent again and again no longer describes how LiteSpec is designed. Amending it keeps this page true; recording why it holds keeps the exceptions from becoming the rule.

**Example:** If two decisions each keep a reasoned record type as entries, the next maintainer amends the folder rule to name the case, or records why both were exceptions.

**Reviewed for:** Has this rule been set aside by earlier decisions? Search `docs/intent/decisions/` for the provision ID.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/core/designing#give-a-record-with-its-own-reasoning-its-own-folder` and `litespec/core/designing#deliver-each-record-type-complete`.

Claimwick's expense rules sat together in one rule-set file. The rule that an expense is reimbursed at most once had a statement and a refusal, but nowhere to say why finance pays once or what a second payment would cost. A maintainer applies the folder rule: a rule carries a rationale, so each rule gets its own folder.

```text
spending/aggregates/expenses/rules/
  reimbursed-once/
    reimbursed-once.yaml
    reimbursed-once.md
  approved-before-reimbursement/
    approved-before-reimbursement.yaml
    approved-before-reimbursement.md
```

The maintainer then ships the type complete: the schema takes a `type: rule` file, the folder table lists the home, the explanation table lists Rationale, Example and Limits, the templates show one rule, Claimwick's rules move, and the pages that drew a rule-set file draw the folders. A search for `rule-set` returns nothing.
