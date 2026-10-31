# Book Follows Record Layers

## Context

The outline places Behavior correctly but invents Intent groupings, splits the glossary, and deletes the Implementation and Proof layers it builds.

## Decision

The book generator is rebuilt to the approved mockup, generated from the model: one offline shell with sharded data, every record placed once under its exported parent, and every unplaceable record reported as a finding instead of swept to a root.

## Evidence with Confidence

`research/book-outline-evidence` is read from the builder source, high confidence on placement, medium on full-size behavior since the sample is partial. `research/behavior-containment-evidence` and `research/check-coverage-evidence` are measured over the largest consumer on 2026-10-30, high confidence.
