# {Pattern Title}

**Stack:** any
**Concern:** frontend-ui

Name the recurring interface question this page settles and the screens it covers, so a reader facing that question recognizes the right place.

## Patterns

### Record Header

**ID:** `{litespec/page#record-header}`

**Strength:** default

**Question:** How does the top of a record read?

**Default:** `one-primary`

**Compact:** Actions move under the title.

**Rationale:** One primary action answers what comes next.

**Reviewed for:** Does the top of each record page follow the default option or the one its catalog records? Look at the record pages beside the catalog.

#### `one-primary`: One Primary Action

```wireframe
page
  header
    heading "Order 1042"
    action primary "Mark as shipped"
```

**Rationale:** The next step reads plainly.

**Cost:** The remaining commands wait in a menu.

#### `two-equal`: Two Equal Actions

```wireframe
page
  header
    heading "Order 1042"
    action "Mark as shipped"
    action "Refund"
```

**Rationale:** Both commands sit one click away.

**Cost:** The reader weighs two labels before acting.
