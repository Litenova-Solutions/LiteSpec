# Book Follows Record Layers

The book is the documentation that LiteSpec generates from its model. The model holds the records, grouped into record layers such as Intent, Behavior, Implementation and Proof. This decision sets how the book places each record. It matters because a record shown in the wrong layer, or shown twice, misleads the reader.

## Context

The current outline places Behavior records correctly. It invents groupings in Intent, splits the glossary, and deletes the Implementation and Proof layers that the generator builds.

## Options

LiteSpec compares 4 ways to fix the outline.

| Option | Strengths | Drawbacks |
|---|---|---|
| Rebuild the generator from exported parents | One exported parent per record ends file-path parents, arbitrary placement and invented nodes, in one mechanism the checker already proves | The generator, its outline fixtures and the book page contract all change at once |
| Patch the current outline | Smaller diffs land sooner, and existing fixtures keep passing | The defects are structural: file parents, first-match placement, invented groupings and two deleted layers cannot be fixed one at a time |
| Keep file-grouped requirements | Placement matches authoring, and no new field is needed | A page style is not a record type, and the grouping breaks the outline and label provisions at once |
| Keep the stray sweep | No record goes unplaced, so the book always renders complete | Unplaced records become roots beside modules, which hides misplacement instead of reporting it |

The chosen option rebuilds the generator. The defects are structural, so a patch to the outline cannot fix them one at a time.

## Decision

The book generator is rebuilt to the accepted mockup. It generates the book from the model into one offline shell, with the data split into shards.

Every record is placed once, under its exported parent. A record that cannot be placed is reported as a finding. It is not moved to a root.

## Consequences

Six provisions on the model page carry the rebuilt contract. They cover layer placement, book labels, explanations drawn from LiteSpec definitions, computed judgements, missing facts, and the source of every book entry.

## Evidence

`research/book-outline-evidence` is read from the builder source. Its placement finding has high confidence, and its full-size behavior has medium confidence, because the sample is partial. `research/behavior-containment-evidence` and `research/check-coverage-evidence` are measured over the largest consumer on 2026-10-30, with high confidence.

## Revisit Conditions

This decision reopens when the accepted mockup is replaced by a newer accepted shape.
