# Hierarchy Release: Follow-Up Handover

**Written:** 2026-10-30, at the end of the hierarchy release run.
**Repositories:** LiteSpec `D:/Personal-Projects/LiteSpec`, branch `v3`; NightHall `D:/Personal-Projects/Nighthall`, branch `v1`.

## Where Things Stand

The hierarchy release is implemented, verified and pushed:

| Repository | Commits | What they hold |
|:---|:---|:---|
| LiteSpec `v3` | `ecf2ed8` | Contract, checks, book, approval fixes |
| LiteSpec `v3` | `14ca1b2` | Approval-at-scale fixes |
| NightHall `v1` | `6ade2568d9` | Mechanical renames |
| NightHall `v1` | `d9aa1a51fc` | Agent-decided parents |
| NightHall `v1` | `b6f1cc77ce` | Approval |
| NightHall `v1` | `7eae17c383` | Regenerated frontend record lists |

Read these first, in this order:

1. `LiteSpec/AGENTS.md` and `Nighthall/AGENTS.md`.
2. In `LiteSpec/.agents/records/hierarchy-release/`:
   - `spec.md`: Part 0 (lines 7 to 35) is binding. Decision 14 says agents research, decide and approve NightHall's gaps.
   - `decisions.md`, `progress.md`, `contracts.md`, `ledger.tsv`.
3. In NightHall, `.agents/records/traceability/judge-decisions.md`: entries J38 to J49 and the "For the Founder" list. Then `hierarchy-judgements.md` beside it.
4. `Nighthall/docs/proof/reviews/2026-10-30-hierarchy-*.md`: the five agent reviews.
5. `~/.agents/skills/plan-execute/references/lessons.md` and the "Field notes from a two-repository swarm" section of `~/.agents/skills/delegate-free/references/muse.md`: what the last run learned about cutting waves, running the swarm, verifying and stamping tools.

Current state:

- **LiteSpec:** `bash .agents/records/hierarchy-release/verify.sh <out>` is green: build, provisions, validate, 2,382 tests, whitespace and book smoke. The site tests (`cd apps/site && npm test`) pass 45 of 45.
- **NightHall:** `dotnet run --project D:/Personal-Projects/LiteSpec/apps/cli/src/LiteSpec.Cli --no-build -- model check --root D:/Personal-Projects/Nighthall` exits 0, with 4,781 advisory findings and 407 known.
- **NightHall records:** 1,846 approved, 14,943 reviewed, 2,746 held in draft.

## The Follow-Ups

### F1. NightHall Consumer Validation

`litespec consumer --root D:/Personal-Projects/Nighthall` exits 1 with about 2,900 failures.

First establish the baseline. Run the published 3.0.0 tool NightHall pins (`dotnet litespec consumer`) in a `git worktree` at commit `895820fd5d`, and diff its failure set against the current CLI on `HEAD`. Fix whatever this release introduced in the same wave as the rest.

The known categories:

- **UI pattern catalog** (`docs/implementation/ui/patterns/catalog.json`):
  - option names no longer match the LiteSpec pattern options, for example 'Upcoming Payouts with the Expenses Each One Holds';
  - some library IDs are no longer patterns on a LiteSpec pattern page;
  - some component paths do not exist, for example `apps/admin/src/app/format.ts`.

  Follow `litespec/frontend/patterns#keep-one-pattern-catalog` and `#start-from-a-litespec-pattern`.
- **Title Case:** H1 and H2 headings that break it, such as 'Read User Account By Login' and 'Query the model'.
- **Requirement pages** missing required H2 sections: 'What We Do', 'Why It Is Better', 'Example', 'Costs and Risks', 'How Sure We Are', 'Position', 'Refusals' and 'The Situation'.
- **Aggregate pages** whose `dataClasses[]` entries lack `basis`. 545 module pages fail the schema.
- **Integration events** without `translates` (Part F measured 10 of 29). Read the schema and the provision, then write the link or the exemption the provision allows.
- **Screens** that fail the `address`/`host` choice, and schema failures on external systems and runbooks.

Give each category its own Muse slice, split by module where it is large. Prose and section work comes from the page's real content, never placeholders. Verify with `litespec consumer` and `model check`. NightHall's tests change only by mechanical call-site replacement.

### F2. The 2,434 Held Records with Open Code Findings

Approving a record lifts the draft softening of its code findings. That exposed 261 tier S findings on 260 records:

| Code | Findings |
|:---|---:|
| `use-case-act-uncalled` | 159 |
| `use-case-unexposed` | 58 |
| `aggregate-unimplemented` | 21 |
| `use-case-unimplemented` | 8 |
| `decision-unfollowed` | 8 |
| `reaction-unimplemented` | 4 |
| `setting-key-drift` | 1 |
| `refusal-not-thrown` | 1 |
| `external-system-unimplemented` | 1 |

These records, with their results and scenarios, stay in draft. The lists were in `LiteSpec/.agents/delegate/nh-approve/held-subjects.txt` and `held-subjects-all.txt`, which are gitignored. If they are gone, regenerate them: run a dry-run approval, then take the blocking findings from `model check --format json`.

For each finding:

- Research whether the code or the record is wrong.
- Fix whichever is wrong: the aggregate method call, endpoint, mark or handler, or the record. Follow NightHall's architecture rules and LiteSpec's dotnet provisions.
- Group the work by module, one Muse slice per module.

After each wave, re-approve the batches that are now clean, using the fixed tool:

1. Run `model review --format json`.
2. Map each batch to its review report. Reuse the module reports `docs/proof/reviews/2026-10-30-<module>[-n].md` (J49 describes the mapping).
3. Run `model approve --batches <file> --by "NightHall founder" --second-reviewer agent --on <date>`, in passes, until no new batch approves.
4. Write a new agent review report for any batch whose records changed in substance.
5. Confirm `model check` still exits 0.

### F3. Decision 0084 and the End of Transcription

`decision/0084-promote-to-production-decision` is NightHall's `adoption.transcriptionEndsWith` decision. It has options but no `chosen`, so it stays in draft.

`litespec/core/model#end-transcription-at-the-named-decision` requires every transcribed record to clear before that decision is approved. A record clears when it earns its Intent parent and drops `"source": "transcribed"`, or when it retires.

1. Measure how many records still carry the mark (about 2,835 did).
2. Clear the mark on every record whose parents are approved. This is mechanical once the parent check passes; use Muse slices by module.
3. Leave the remaining records as the evidence says.
4. Decide 0084's `chosen` from its options and the founder's own words (the brief, the roadmap, the J-entries), and record the judgement.
5. Approve 0084 only when `transcribed-after-adoption` would report nothing. Otherwise hold it and record why.

### F4. The Three Open Questions: Not Yours to Answer

The tax adviser and counsel own three tier 1 questions (J38). Do not answer them:

- `constraint/platform-hosting-duties`
- `requirement/pricing.pay-as-you-go-fee`
- `requirement/quality.no-oversell-under-concurrency`

Confirm that the "For the Founder" list names them and everything that rests on them:

- `journey/choose-a-plan` and `journey/serve-the-shop-on-own-domain`;
- the screens that serve those journeys' steps, which hold the `admin/screen` and `control-panel/screen` batches;
- `use-case/billing.draft-plan`.

**Optional LiteSpec improvement**, decided on evidence and recorded in `decisions.md`: a module-type batch could hold only its blocked records instead of being refused as a whole. The code is in `BatchApprover.cs`; the provision is "approve drafts in bounded batches" in `docs/core/release.md`; the tool page is `docs/tools/model.md` line 34. If you change it, replace every citation, add tests, and run the NightHall approval passes again.

### F5. Book Visual Verification

The checklist is spec Part E, Section 9, lines 856 to 864.

**Generate the book from two models:**

- the fixture, `apps/cli/tests/LiteSpec.Cli.Tests/Fixtures/Book/review.json`;
- NightHall, `.model/model.json`.

For each, run it three ways: with `--root` and the default editor, with `--editor none`, and with a custom editor template.

**Drive it with the playwright-cli skill** and compare against the approved shots in `LiteSpec/.agents/mockups/book-v6/shots/pass/` (one per record type). Check:

- each record type's facts, with the status reason, the Why block and the rail;
- the outline accordion and its status colors;
- the Intent tree;
- the Review queue, Security and Ballots views;
- hover definitions, in light and dark themes;
- the keyboard keys `/ j k u [ ] n a 1-4 e c Esc`;
- source links;
- at 21,548 records, no visible stall, and shard requests within the Section 6 caps.

Also confirm two things are gone on purpose: greyed "outside this sample" records, and Mermaid.

**Then:**

- Critique every screenshot for mess and redundancy before reporting.
- Fix `apps/cli/src/LiteSpec.Cli/Book/Assets/book.js` and its CSS, or the C# shard writers, wherever they differ from the mockup or from `contracts.md` Section 4.
- The client fix `fix.client-edges` landed late and has no browser test yet. Confirm that edges, link phrases and the not-run wording render.
- Audit spec Part E items 1 to 9 against the tests that exist. In particular, item 5's `BookContentTests` must show that the asset holds a detail renderer for every record type in `RecordTypes.All` and none of the purged sample strings. Add any test that is missing.
- Add no package without approval.

### F6. LiteSpec Research Sources

`docs/intent/research/established-traceability-practice.md` cites bare domains, because its source report `.agents/delegate/hier/done/h-practice.md` named only domains:

- technav.ieee.org
- jamasoftware.com
- vector.com
- link.springer.com
- wer.inf.puc-rio.br
- Microsoft Learn
- Polarion
- Linear

Search the web for the exact page behind each claim, read it, and record its exact URL with `read` set to the date you read it. If a claim no longer holds, correct the Answer and the `conclusion`. Run `litespec-maintain validate`.

### F7. Release Hygiene in LiteSpec

- Add the hierarchy release to `CHANGELOG.md` for the release being prepared, in the house style, with no history phrasing.
- Confirm `litespec.manifest.json` and the consumer template's `litespec.project.json` need no change.
- NightHall pins the published `litespec.cli` 3.0.0 in `.config/dotnet-tools.json`, and that tool does not know the new schema. Find out what NightHall's CI gates run, and record in both decision logs how CI runs until a release is tagged.
- Do not tag. Do not move the pin to an unpublished version without recording why.

### F8. A Conformance Review of the Whole Release

Run the review-conformance skill against `spec.md`, over two diffs:

- `git diff 242459d..HEAD` in LiteSpec;
- `git diff 895820fd5d..HEAD` in NightHall.

Use Muse explore workers in parallel, one per spec part (A to G). Turn every confirmed finding into a slice and fix it in the same run.

## Lessons to Apply

These are kept in `~/.agents/skills/plan-execute/references/lessons.md`. The ones that matter most here:

- **Never move HEAD in a repository while Muse workers run there.** Commit between waves only.
- **Give every worker exclusive paths.** Fix shared shapes in a `contracts.md` before launch, copying names from the code. The book code's namespace is `LiteSpec.Book`.
- **NightHall's model check and approve take 3 to 10 minutes.** Run them in the background and keep working.
- **The provisions step diffs against the git index.** Stage the regenerated index before judging the step.
- **Node on Windows reads `/tmp` as `D:\tmp`.** Pass paths through `cygpath -w`.
- **Never put backticks in bash-quoted text.** Write such text with the file tool instead.
- **Mechanical JSON rewrites:** take the "before" text from a fresh parse, never from the mutated object. Dry-run, and compare the counts against the measured ones.
- **Approval tools:**
  - Dry-run first, then approve in passes.
  - Diff the check's finding codes before and after each pass.
  - Withdraw any stamp that adds a blocking finding: restore its fields from HEAD and keep the manifests true.
  - Fix the tool and add a test.
- **The free tier has no working model.** Run workers on Muse with `WAVE_SKIP_TIERS=free SWARM_MEM_RESERVE_GB=3 SWARM_MEM_PER_WORKER_GB=0.4`.

## Done Means

- LiteSpec's `verify.sh` and the site tests are green.
- NightHall's `model check` exits 0.
- `litespec consumer` exits 0, or each remaining failure is recorded in the decision log with its reason.
- The held count is down to what the open questions, and any other recorded hold, still justify.
- Every judgement is in NightHall's decision log or LiteSpec's `decisions.md`, with its evidence.
- Every wave is committed and pushed to `v3` and `v1`.
- The final report lists:
  - each follow-up with its result;
  - the commits;
  - every skipped check, with its reason;
  - whatever is still held, and why.

## Stop and Ask Only If

- A change would contradict `spec.md` Part 0 or a LiteSpec provision, and nothing decides it.
- An action cannot be undone beyond a git commit: tagging, publishing, deleting data or spending money.
- The work would require answering one of the three questions the tax adviser and counsel own.
