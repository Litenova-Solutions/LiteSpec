# Layer Surfaces by Role

LiteSpec addresses surfaces as layers by role: the page, the layer on it, the inset layer, and the accent fill. Each layer differs measurably from the surface it sits on, so neighbouring surfaces read apart in every scheme.

## Context

The token roles page, card, card-edge and separator cannot describe stacked surfaces. A table header takes the fill of the page, a field takes the fill of whatever it sits on, and a selected row reads as its neighbour, because no role names the fill each one takes. A card inside a card reads as one surface where the structure says two.

Each new pair of neighbours needs its own role, so the role list grows by accident instead of by design. No rule says which layer a new surface takes.

## Options

LiteSpec compared 3 options. The table sets each against what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Keep page and card and add a role per neighbour pair | The existing roles stay, and each new pair gets a narrowly named role. | The list grows with every pair anyone notices, and no rule places a new surface. |
| Address surfaces as layers by role | Four roles name the whole stack, each differs measurably from its surface, and a new surface takes the layer its role names. | The card and card-edge roles are renamed, so every token file, check and fixture moves in one change. |
| Leave surfaces to the design contract with no token roles | No roles to keep, and each product describes its own surfaces. | Two products share no vocabulary for layers, and no check can judge neighbours apart. |

## Decision

The roles are `page`, `layer-1`, `layer-2` and `layer-accent`. `layer-1` sits on `page` and holds sections, cards and dialogs. `layer-2` sits on `layer-1` and holds an inset where one is truly needed. `layer-accent` is the fill of a table header, a field and a selected row on whichever layer holds them.

Each layer differs from the surface it sits on by at least 1.06:1 relative luminance in every declared scheme, `layer-accent` against both `layer-1` and `layer-2`. The `card` role is renamed to `layer-1` and `card-edge` to `layer-edge` everywhere with no alias. A composite drawn inside a surface draws no surface of its own.

## Consequences

The token table on `docs/frontend/ui.md` states the layer limits, and the Surfaces and Layers concepts name the stack. The rendered-page audit holds neighbouring fills apart where the token file cannot see them. This decision supersedes `decision/0102-draw-platform-composites-with-native-elements` for composites on surfaces: a composite takes the layer its role names and draws no surface of its own.

## Revisit Conditions

Reopen this when products need a surface the four roles cannot place, or when the 1.06:1 step proves too small to read apart or too large to theme around.
