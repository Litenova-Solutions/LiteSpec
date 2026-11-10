# {Pattern Title}

**Stack:** any
**Concern:** frontend-ui

## Intent

State the recurring interface question this page answers and the screens it covers.

## Patterns

### Record Header

**ID:** `{litespec/page#record-header}`

**Strength:** default

**Question:** How does the top of a record read?

**Default:** `one-primary`

**Compact:** Actions move under the title.

**Rationale:** One primary action answers what comes next.

**Reviewed for:** Does the top of each record page match the default option or the one its catalog records?

#### `one-primary`: One Primary Action

```wireframe
page
  header
    heading "Order 1042"
    action primary "Mark as shipped"
```

**Rationale:** The next step is plain.

**Cost:** Other commands wait in a menu.

#### `two-equal`: Two Equal Actions

```wireframe
page
  header
    heading "Order 1042"
    action "Mark as shipped"
    action "Refund"
```

**Rationale:** Both commands are one click away.

**Cost:** The reader compares labels before acting.
