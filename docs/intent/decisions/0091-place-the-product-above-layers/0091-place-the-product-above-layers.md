# Place the Product Above the Layers

The product is the root of one tree, and its file now says so. This decision moves the product record out of Intent to the root of `.litespec/`, gives it the layer key `root`, and takes its node out of the book's Intent layer.

## Context

The model already calls the product the root: every record has exactly one parent, and only the product has none. But the placement table stored the product at `intent/product.yaml` with the Intent layer, so the book drew it as the one node above everything in Intent.

That placement said the wrong thing twice. It put a single node above the whole Intent layer in the book, where every other layer tops its own records directly. And it filed the product as intent only, while its summary, boundary and excludes scope the whole application: what is built, for whom, and what stays out.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Product above the layers at the record root with the layer key root | File, layer key and book match the root the model already states. | Every consumer moves one file; the book names the product without a node. |
| Product stays in Intent | No file moves and no outline changes. | One node stays above Intent, and the product still reads as intent only. |
| Product as a fifth claimed layer | The product keeps a layer of its own. | One mandatory record is not a claimed layer; every claim carries an exception. |

## Decision

The product is the root record, above the record layers. It lives at `.litespec/product.yaml`, with its explanation file `.litespec/product.md` beside it, and carries the layer key `root`. It is always present whatever layers the project claims in `project.yaml`, it belongs to no layer group, and no layer count treats it as Intent.

## Consequences

The folder check reads a product file left at `intent/product.yaml` as a record in the wrong place, through the existing placement finding. Scaffolding, renames and the folder reference all name the root home.

The book holds no product node. The product's Intent children top the Intent layer directly, in record type order. The product record still ships in the record shards with its fields, its type and its `root` layer. The manifest names it in `productId`, the lexicon labels `root` as Product, and the parent of every record still ends at the product.

## Evidence

Principle 1 on the model page already states one tree from the product, with the product as the only record without a parent. The book outline before this change held the product as the single node above every Intent record, which the outline code has now removed.

## Revisit Conditions

This decision reopens when a consumer shows work that needs the product inside a claimed layer. It also reopens when readers cannot find the product without its Intent node.
