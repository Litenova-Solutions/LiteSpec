# {Extension Title}

**Stack:** any
**Concern:** operations

Name the optional capability this extension adds and draw its boundary: what it covers and what remains with the baseline.

## Activation

Activation scope: `{local|project}`.

Applicable specification kinds: `<type>`.

Name the one exact current condition that activates this extension.

## Baseline Relationship

Name the baseline provision this extension replaces, `{litespec/page#state-one-action}`, or state that it replaces none. Name the `<optional type>` it switches on, or state that it switches on none.

## Standards

### State One Extension Action

**ID:** `{litespec/page#state-one-extension-action}`

**Strength:** required

**Applies to:** artifact: job

**Statement:** The activated extension MUST perform one testable action.

**Rationale:** Give the extension-specific reason behind the action.

**Example:** Show one compliant artifact or behavior the extension produces.

**Checked by:** `litespec-maintain validate` reports this provision satisfied on the page that carries it.

## Conventions

### Use One Extension Default

**ID:** `{litespec/page#use-one-extension-default}`

**Strength:** default

**Applies to:** artifact: test

**Default:** Write one replaceable default this extension sets.

**Replacement:** Name the local convention that replaces the default, and where the project records it.

**Reviewed for:** Does the page carry the default or its named local replacement? Look at the page beside the project's local conventions.

## Dependencies

Packages: `{package}` or `None.`

Related extensions: `{extension}` or `None.`

Guides: `{guide}` or `None.`
