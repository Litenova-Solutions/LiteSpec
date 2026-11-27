# Cross-Layer Trace Evidence

A trace links a goal down through the layers to the test that proves it. A reader who follows the trace can move from a promise to the check that confirms it. This research record follows that trace top down and bottom up through the largest consumer, a project that uses LiteSpec, and shows where it breaks. It matters because a team that reads only its open findings sees no break, and this record shows where a break exists.

## Findings

The trace breaks at the top, not at the bottom (`model`, `recordtypes`, `model-2`). Only 19 of 925 live requirements reach a goal, and only 18 of 1,272 use cases do (`model`, `recordtypes`, `model-2`). The links below the requirements are many and mostly checked (`model`, `recordtypes`, `model-2`).

On 30 October 2026, 12 of 40 features name a goal in `serves` (`model`, `recordtypes`, `model-2`). Features serve all 8 goals, but 28 of 40 features serve no goal (`model`, `recordtypes`, `model-2`).

All 40 features name `solves` (`model`, `recordtypes`, `model-2`). That is the one strong Intent link, and the checker requires and checks it (`model`, `model-2`). Of 40 features, some requirement names 23 (`model`, `recordtypes`, `model-2`). Of 928 requirements, 872 name no `feature`, which the record treats as intended (`model`, `recordtypes`, `model-2`).

The checker counts 763 of 925 live requirements as fulfilled (`model`, `model-2`). The record also counts 53 as exempt because they are undelivered, and 10 quality requirements as served by a technical decision (`model`, `model-2`). It counts 105 as unfulfilled (`model`, `model-2`). Waivers cover all 105, so a reader of the open findings alone sees no break (`model`, `model-2`).

All 112 flows carry `fulfills` links, which together reach 571 distinct requirements (`model`, `recordtypes`, `model-2`). The links are complete, but no check reads them (`model`, `model-2`).

All 1,272 use cases have a success result (`model`, `recordtypes`, `model-2`). Of 5,413 written results, 3,083 have a scenario (`model`, `recordtypes`, `model-2`). The other 2,330 results, mostly refusals, have none (`model`, `recordtypes`, `model-2`).

Of 6,517 scenarios, 5,426 have a confirmed `proves` link (`model`, `recordtypes`, `model-2`). Every such link has a null passing state, and the model's test run is null (`model`, `model-2`). Zero of 1,272 use cases count as verified (`model`, `model-2`).

Code implements 1,266 of 1,272 use cases (`model`, `model-2`). All 380 screens have `offers` equal to the computed `calls` (`model`, `recordtypes`, `model-2`).

End to end, only 19 of 925 live requirements trace to a goal (`model`, `recordtypes`, `model-2`). The trace uses `feature` and then `feature.serves` (`model`, `recordtypes`). The count per goal is 11, 9, 5, 1, 1, 0, 0, and 0 (`model`, `recordtypes`, `model-2`). Of the traced requirements, only 3 reach an implemented use case with a confirmed scenario proof (`model`, `recordtypes`, `model-2`).

Only 18 of 1,272 use cases trace to a goal, so only 18 of 1,251 handlers reach one (`model`, `recordtypes`, `model-2`). Every other handler reaches a requirement or a flow step and stops there (`model`, `recordtypes`, `model-2`). A flow names its goal only as a sentence, so the trace reaches no goal record (`model`, `recordtypes`).

A route connects to screens, steps, flows, and requirements (`model`, `recordtypes`, `model-2`). The flow has no goal-record field, so a route shows which promise a screen serves, but it never shows which outcome (`model`, `recordtypes`).

Of 928 requirements, 56 name a `feature`, 164 name `basedOn`, 209 name either, and 719 name neither (`model`, `recordtypes`, `model-2`). The `basedOn` count includes 139 constraint citations and 48 research citations, with some overlap (`model`, `recordtypes`, `model-2`). The record also finds that 109 live requirements name neither anchor and are also unfulfilled (`model`, `recordtypes`, `model-2`).

The link from a flow to its goal is undefined, not merely empty (`model`, `recordtypes`). A goal record has no outgoing link field (`model`, `recordtypes`). The `flow.goal` value is a sentence in all 112 cases, and LiteSpec has no flow-to-goal link (`model`, `recordtypes`).

The rest of the flow chain has no gaps (`model`, `recordtypes`, `model-2`). All 1,077 steps carry an identity and an actor, and all are served (`model`, `recordtypes`, `model-2`). Screens serve 817 of them (`model`, `recordtypes`, `model-2`). The model contains 91 known `step-screen-missing` findings, and none is open (`model`, `model-2`).

The specifications disagree in 3 places (`model`, `recordtypes`). Of the use cases, 11 serve a whole flow instead of a step (`model`, `recordtypes`, `model-2`). The link-type table allows `serves` to name a flow, but the schema's `servesArray` allows only a step or a goal (`model`, `recordtypes`). Written result identifiers drop the module, while derived ones keep it, so a reader must learn 2 containment shapes (`model`, `recordtypes`). Route links name screens by frontend and route text, not by `screen/` identifiers (`model`, `recordtypes`). Joining a call to its screen therefore needs the computed `screens` array (`model`, `recordtypes`, `model-2`).

Governance splits sharply by home (`model`, `recordtypes`, `model-2`). All 3 product decisions carry `decides` (`model`, `recordtypes`, `model-2`). Of 71 technical decisions, which govern through `follows` and `basedOn`, 56 name a basis (`model`, `recordtypes`, `model-2`). Of them, 15 show the advisory `decision-basis-missing`, and 8 show `decision-unimplemented` (`model`, `model-2`).

The tooling breaks the `follows` link (`model`, `model-2`). The model writer exports only `implements`, `calls`, and `proves` (`model`, `model-2`). LiteSpec defines and checks code-to-decision traceability, but the built model and the book omit it (`model`, `model-2`).

`basedOn` reaches constraints and research unevenly: 24 of 29 constraints and 14 of 29 research records (`model`, `recordtypes`, `model-2`). Of 29 constraints, 25 cite research as free text in `source`, and no link table validates that text (`model`, `recordtypes`). Of 22 waivers, 5 name neither `appliesTo` nor `code`, so they govern nothing that a tool can name (`model`, `recordtypes`, `model-2`). No waiver appliesTo grammar covers one flow step (`model`, `recordtypes`).

The investigation argues against making `requirement.feature` mandatory as written (`model`, `recordtypes`). The field's meaning is narrow, and a mandatory field would invert that meaning and repeat the requirement page's own rationale (`model`, `recordtypes`). The investigation supports an anchor set instead (`model`, `recordtypes`). Each live requirement must be fulfilled, undelivered through its feature, served by a technical decision for a quality requirement, or covered by an unexpired waiver (`model`, `recordtypes`). One advisory check flags a requirement that has no `feature`, no `basedOn`, and no deciding decision (`model`, `model-2`).

The record agrees that every feature should name its goal beside the existing `solves` rule (`model`, `recordtypes`). It agrees that the flow needs an identifier-typed goal link, and that checks should start advisory (`model`, `recordtypes`). The waivers cover 105 fulfilled-check results, which are invisible in the open findings (`model`, `model-2`). So a new advisory check needs a named promotion test, or it will end up in the waiver list instead of raising compliance (`model`, `model-2`).

## Source Notes

The record names 3 repository sources, all read on 30 October 2026. They are the core model page (`docs/core/model.md`), the record types code (`apps/model/src/LiteSpec.Model/Records/RecordTypes.cs`), and the model tool page (`docs/tools/model.md`). Each finding cites the sources that support it.

The Answer cites the schema's `servesArray`, but the record schema is not in the sources list. The specifications disagree on `serves`. The link-type table allows a flow, and the schema allows only a step or a goal. The record reports this disagreement and does not settle it.

The record lists no claim that it could not confirm. Its counts come from one consumer's built model on one day.
