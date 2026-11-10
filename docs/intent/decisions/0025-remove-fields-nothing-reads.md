# Remove Fields Nothing Reads

## Context

Several fields in the project file, the release manifest, the pattern catalog and the design contract had no reader in any tool. Some repeated a value kept elsewhere, so the two copies could disagree.

## Decision

A field that no tool reads leaves its file. A value a page cites, such as a toolchain pin that a stack page names, stays, because the page reads it.
