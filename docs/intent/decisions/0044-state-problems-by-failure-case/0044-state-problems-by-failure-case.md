# State Each Problem by Its Failure Cases

A problem explanation tells a reader what goes wrong for the people a product serves. Until now, it described what those people do by hand and left the failures behind that work implied. This decision gives every problem a list of failure cases, an optional example and one resolution criterion for each failure, and it states how a problem opens, claims and cites its evidence. A reader can then judge the problem and propose a different solution without asking its author.

## Context

A problem explanation carried five sections: Affected Users, Current Workaround, Cost, Evidence and Resolution. A review of problem explanations written to those sections found the same faults again and again.

The opening paragraph often started with the workaround. It said that a task is done by hand, but never why the task is needed, so the reader had to guess the cause.

Two failures often shared one sentence. When two records should agree, an entry missing from the first and an entry missing from the second are found by different checks, and a solution that runs only one check leaves the other failure in place. The Resolution section then covered only the failure the author thought of first.

The evidence often held feasibility: a file format or an API that shows a solution can be built. Claims were often wider than their evidence, with one person's estimate written as a fact about everyone.

## Options

LiteSpec compared 3 options for the shape of a problem explanation. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Keep the five sections | No problem explanation is rewritten again. | Failures share a sentence, a resolution can end half the problem, and feasibility passes as evidence. |
| Failure cases as bullets in the opening, an optional Example, and Resolution kept | A cold reader meets each failure first, and the change adds one optional section. | No check sees whether failure cases are listed, and nothing ties a resolution to each failure. |
| A Failure Cases section, an optional Example, Resolution Criteria, and rules for the opening, claims, evidence and resolution | Each failure has its own entry and its own criterion, and the check sees the section. | Every problem explanation is rewritten in 5.3.0, and a problem with one failure carries a one-entry list. |

## Decision

A problem explanation carries Affected Users, Failure Cases, Example (optional), Current Workaround, Cost, Evidence and Resolution Criteria, in that order.

Its opening paragraph states what the sufferer cannot reliably do, the gap that causes it and what they lose or risk. Each failure that can happen on its own gets its own entry, with how the sufferer finds it and what follows when they do not.

Each claim stays within its evidence, an estimate is named as an estimate, and an evidence entry shows the pain, never only that a solution can be built. Each failure case gets one resolution criterion: an observable outcome for the sufferer that names no feature or implementation.

A problem title names the failure or its consequence, never a theme or a feature.

## Consequences

`explanation-missing` reports a problem explanation without the new sections in order. Five new provisions on the intent page state the opening, failure case, claim, evidence and resolution rules, and the cost and workaround provision now asks for the burden of the workaround and keeps time, money and risk apart.

The provision that lists the sections keeps its ID, because a minor release renames no ID, and takes the heading Explain Every Problem in Its Fixed Sections.

The problem template and the Claimwick problem follow the new sections. Each consumer rewrites its problem explanations by hand, as the changelog's upgrade step describes.

## Revisit Conditions

This decision holds until consumers report that most problems have one failure case, so the Failure Cases section repeats the opening paragraph. It also holds until consumers report that resolution criteria cannot be written without naming the feature that delivers them. Either report reopens the choice.
