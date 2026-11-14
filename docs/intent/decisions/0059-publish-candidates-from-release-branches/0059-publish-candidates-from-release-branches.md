# Publish Release Candidates from Release Branches

A consumer validates a LiteSpec release before the release is final, and a defect it finds must be fixed before the final tag. This decision lets a release branch publish candidate packages, so the consumer tests the packages it will install.

## Context

The release workflow published only on a `v*` tag, and its own comment said that a release is a tag on main. Nothing could be published from a `release/<version>` branch before it merged.

A consumer could therefore test a release only by building the command line from source, which is not the build it would install. Its first real run came after the final tag, so a defect it found needed another release.

## Options

LiteSpec compared 3 options for how a consumer tests a release before it is final. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Candidate tags on the release branch publish prerelease packages | A consumer tests the packages it installs, and a fix ships in the next candidate. | The workflow gains a second path, and the version readers handle a suffix. |
| Consumers build the command line from source | No workflow changes. | The tested build differs from the installed one, and the first real run is late. |
| A separate preview feed | Preview packages stay apart from the stable feed. | A second feed must be hosted and secured. |

## Decision

A tag of the form `v<version>-rc.<n>` publishes a release candidate when the tagged commit is on `release/<version>` and the version matches the manifest. A tag without a suffix still publishes only from main.

A candidate runs the same checks as a release and publishes the two packages as prerelease packages, with the same archives, checksums and attestations. Its notes are the changelog's section for the version, headed as a candidate. The committed files keep the plain version, and the workflow adds the suffix at build time.

## Consequences

Every place that reads the release version reads the full version a candidate carries: the analyzer pin written into a consumer, the agent protocol and the skill, and the schema identifier the tool reports. The version comparison that checks the reviewed release reads the base version, so a candidate counts as the release it precedes.

A candidate's site deploys to a versioned path beside the current release, so the links a candidate writes into a consumer's agent file resolve while the consumer validates it. The final release removes the candidate's site.

The release procedure runs the candidates first. A defect found by the consumer is fixed on the branch and shipped in the next candidate, and the final tag on main follows the first candidate the consumer passes.

## Revisit Conditions

This decision holds until consumers report that candidate packages reach them too late to test. It also holds until a candidate is hard to tell from a final release in the tools that consumers run. Either report reopens the choice.
