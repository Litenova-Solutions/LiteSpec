# Cross-Layer Trace Evidence

A trace links a goal down through the layers to the test that proves it. A reader who follows the trace moves from a promise to the check that confirms it. This research record follows that trace top down and bottom up through the largest consumer, a project that uses LiteSpec, and shows where it breaks. It matters because a team that reads only its open findings sees no break, and this record shows where a break exists.

## Findings

The trace breaks at the top, not at the bottom. Only 19 of 925 live requirements reach a goal, and only 18 of 1,272 use cases do. The links below the requirements are many and mostly checked.

On 30 October 2026, 12 of 40 features name a goal in `serves`. Features serve all 8 goals, but 28 of 40 features serve no goal. The record gives no confidence for this claim.

All 40 features name `solves`. That is the one strong Intent link, and the checker requires and checks it. Of 40 features, some requirement names 23. Of 928 requirements, 872 name no feature, which the record treats as intended. The record gives no confidence for this claim.

The checker counts 763 of 925 live requirements as fulfilled. The record also counts 53 as exempt because they are undelivered, and 10 quality requirements as served by a technical decision. It counts 105 as unfulfilled. A waiver covers all 105, so a reader of the open findings alone sees no break. The record gives no confidence for this claim.

All 112 flows carry `fulfills` links, which together reach 571 distinct requirements. The links are complete, but no check reads them. The record gives no confidence for this claim.

All 1,272 use cases have a success result. Of 5,413 written results, 3,083 have a scenario. The other 2,330 results, mostly refusals, have none. The record gives no confidence for this claim.

Of 6,517 scenarios, 5,426 have a confirmed `proves` link. Every such link has a null passing state, and the model's test run is null. Zero of 1,272 use cases count as verified. The record gives no confidence for this claim.

Code implements 1,266 of 1,272 use cases. All 380 screens have `offers` equal to the computed `calls`. The record gives no confidence for this claim.

End to end, only 19 of 925 live requirements trace to a goal. The trace uses `feature` and then `feature.serves`. The count per goal is 11, 9, 5, 1, 1, 0, 0, and 0. Of the traced requirements, only 3 reach an implemented use case with a confirmed scenario proof. The record gives no confidence for this claim.

Only 18 of 1,272 use cases trace to a goal, so only 18 of 1,251 handlers reach one. Every other handler reaches a requirement or a flow step and stops there. A flow names its goal only as a sentence, so the trace reaches no goal record. The record gives no confidence for this claim.

A route connects to screens, steps, flows, and requirements. The flow has no goal-record field, so a route shows which promise a screen serves, but it never shows which outcome. The record gives no confidence for this claim.

Of 928 requirements, 56 name a `feature`, 164 name `basedOn`, 209 name either, and 719 name neither. The `basedOn` count includes 139 constraint citations and 48 research citations, with some overlap. The record also finds that 109 live requirements name neither anchor and are also unfulfilled. The record gives no confidence for this claim.

The link from a flow to its goal is undefined, not merely empty. A goal record has no outgoing link field. The `flow.goal` value is a sentence in all 112 cases, and LiteSpec has no flow-to-goal link. The record gives no confidence for this claim.

The rest of the flow chain has no gaps. All 1,077 steps carry an identity and an actor, and all are served. Screens serve 817 of them. The model contains 91 known `step-screen-missing` findings, and none is open. The record gives no confidence for this claim.

The specifications disagree in 3 places. Of the use cases, 11 serve a whole flow instead of a step. The link-type table allows `serves` to name a flow, but the schema's `servesArray` allows only a step or a goal. Written result identifiers drop the module, while derived ones keep it, so a reader learns 2 containment shapes. Route links name screens by frontend and route text, not by `screen/` identifiers. Joining a call to its screen therefore needs the computed `screens` array. The record gives no confidence for these claims.

Governance splits by home. All 3 product decisions carry `decides`. Of 71 technical decisions, which govern through `follows` and `basedOn`, 56 name a basis. Of them, 15 show the advisory `decision-basis-missing`, and 8 show `decision-unimplemented`. The record gives no confidence for this claim.

The tooling breaks the `follows` link. The model writer exports only `implements`, `calls`, and `proves`. LiteSpec defines and checks code-to-decision traceability, but the built model and the book omit it. The record gives no confidence for this claim.

`basedOn` reaches constraints and research unevenly: 24 of 29 constraints and 14 of 29 research records. Of 29 constraints, 25 cite research as free text in `source`, and no link table validates that text. Of 22 waivers, 5 name neither `subject` nor `code`, so they govern nothing that a tool names. No waiver subject grammar covers one flow step. The record gives no confidence for these claims.

The investigation argues against making `requirement.feature` mandatory as written. The field's meaning is narrow, and a mandatory field inverts that meaning and repeats the requirement page's own rationale. The investigation supports an anchor set instead. Each live requirement is fulfilled, undelivered through its feature, served by a technical decision for a quality requirement, or covered by an unexpired waiver. One advisory check flags a requirement that has no `feature`, no `basedOn`, and no deciding decision. The record gives no confidence for this proposal.

The record agrees that every feature names its goal beside the existing `solves` rule. It agrees that the flow needs an identifier-typed goal link, and that checks start advisory. The waivers cover 105 fulfilled-check results, which are invisible in the open findings. So a new advisory check needs a named promotion test, or it ends up in the waiver list instead of raising compliance. The record gives no confidence for this agreement.

## Source Notes

The record names 3 repository sources, all read on 30 October 2026. They are the core model page (`docs/core/model.md`), the record types code (`apps/model/src/LiteSpec.Model/Records/RecordTypes.cs`), and the model tool page (`docs/tools/model.md`). The record does not say which claim each source supports.

The Answer cites the schema's `servesArray`, but the record schema is not in the sources list. The specifications disagree on `serves`. The link-type table allows a flow, and the schema allows only a step or a goal. The record reports this disagreement and does not settle it.

The record names no claim it did not confirm. Its counts come from one consumer's built model on one day.
