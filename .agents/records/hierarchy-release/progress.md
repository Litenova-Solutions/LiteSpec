# Progress

## Tech Profile

- LiteSpec: .NET 10 (`LiteSpec.slnx`), xUnit through `dotnet test --solution`, the maintainer validator, a SvelteKit site under `apps/site`. Verify ladder: `verify.sh`.
- NightHall: consumer of LiteSpec 3.0.0, branch `v1`; its model is built and checked with this repository's CLI (`dotnet run --project apps/cli/src/LiteSpec.Cli -- model check --root D:/Personal-Projects/Nighthall`). Its own tests are not run by this plan.
- Tiers: free skipped (probe returned no model); Muse `muse-spark-1.3-contributor` at max; judge.

## Log

- 16:00 Start. Ledger re-cut (decisions.md): wave 1 holds 17 slices, 15 on Muse.
- 16:05 Judge: checker foundations (TierOverride, Tiered, three codes, call sites) written before launch.
- 16:06 Wave 1 started, 15 Muse workers at once.
- 16:20 Judge: schema and templates (Part B 3 and 6, Part A Decision 1), checker renames (Part D 8), link targets, derived result form, `follows` export, builder tests. The CLI builds except the three check methods the workers write.
- 16:25 NightHall: N2 to N4 by script, committed `6ade2568d9` on `v1`. NightHall wave 1 started: 9 Muse slices (journey goals, feature goals, refused-feature problems, four origin pages groups, research conclusions, deviation scopes).
- 16:28 Wave 1 written: 14 slices by Muse, book.client accepted by the judge (the stub detector matched the UI label "not implemented"). Validate green after the judge's fixes: provision marks renamed, Claimwick record ids resolvable from LiteSpec pages, normative words in record prose.
- 16:30 approval.tool-fixes written (started early, 16:16). Judge: `RecordTypes.LayerByType`, `decidedOn` written on decision approval, `transcribed-reviewed` reads only parents that exist, `identifier-form` home arm moved to `record-without-parent`.
- 16:33 First full test run: 59 failed of 2,353. Judge fixed the unowned test literals and fixtures, re-stamped fixture and Claimwick approvals, gated `result-without-scenario` on the large size (decisions.md). Second run: 19 failed. Fix round 1 on Muse: book core, approval tests, record checks, Claimwick scenarios; all written by 16:49.
- 16:43 Wave 2 written: book.shell-and-shards, book.docs. Wave 3 started: book.hygiene-and-scale on Muse; judge retired the old pipeline (BookPages*, BookDiagrams, Mermaid, shell and nav code). Muse took the old book test rewrite (fix.book-tests) and the citation sweep of fifteen pages (fix.book-citations).
- 16:36 NightHall wave 1 written: 9 slices. Judge found the rename script had dropped nested renames (requirement `by` and `feature`, `sentBy`, `writes`) and re-ran it fixed; NightHall's blocking findings fell to `result-without-scenario` 62, `approval-reopened` 42 and `widening-unjustified` 6 (the last two clear on re-approval). NightHall wave 2 (Muse): scenarios for the 62 success results. Decision log J40 to J48 and `hierarchy-judgements.md` written.
- 17:13 fix.client-edges written by Muse (accepted by the judge; the stub detector matched the UI label "not implemented" again). The client reads edge keys, resolves Links table rows as `BookLexicon.TryPhrase` does, and shows the not-run wording only without a test run.
- 18:30 Verify (waves 1 to 3 together): build pass, provisions pass (once staged), validate pass, tests 2,378 of 2,378 pass, whitespace pass, book-smoke pass; site tests 45 of 45 after the judge made the site parser skip record frontmatter. The retired Names page test was removed with the page. Commit follows on `v3`.
- 18:40 LiteSpec waves 1 to 3 committed `ecf2ed8` and pushed; NightHall close-out committed `d9aa1a51fc` and pushed.
- 19:00 NightHall wave 9: five Muse second reviews (journeys, features, two requirement groups, research and scenarios) all approve, with a few fixes in place. Approval passes showed four more stamp defects; the judge fixed each in LiteSpec (journeys batch by their goal; a page stamp keeps an unstamped entry's inherited status; a decision without `chosen` is refused; a transcribed record behind a draft journey is refused), with tests, and withdrew the stamps they had written. Approving lifted the draft softening from 261 code findings on 260 records, which stay draft. Result: 1,846 approved, 14,943 reviewed, 2,746 held; `litespec model check` on NightHall exits 0. NightHall `b6f1cc77ce`, `7eae17c383` pushed; decision log J49, and J39 closed.
- 19:20 Final LiteSpec verify: build, provisions (staged), validate, tests 2,382 of 2,382, whitespace, book-smoke all pass.
