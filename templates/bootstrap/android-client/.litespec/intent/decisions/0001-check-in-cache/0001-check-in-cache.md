# Check-in Cache

The list opens from the cache at once and refreshes when the network answers.

## Context

Visitors open the app where the network may be slow or absent.

## Options

Live list only, or cached list with live refresh.

## Decision

The list opens from the cache at once and refreshes when the network answers.

## Consequences

The app keeps the last loaded check-ins in app-private storage.

## Evidence

The starter targets phones on the move, where the network drops.

## Revisit Conditions

Visitors report stale check-ins.
