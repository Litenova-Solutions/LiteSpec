# Bind the Docs to the Tools and the Example

## Context

The docs stated tool behavior that the tools did not have, such as a `validate` option and a `new decision` number form. Five of six sampled record IDs in the example did not exist. Nothing checked either.

## Decision

Each citation of a tool fact is checked by `validate` against data the tools hold, and the reference tables are generated from that data. Explanatory prose stays hand-written.
