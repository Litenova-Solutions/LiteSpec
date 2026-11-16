# Make the Book an Explorer

A reader opens the book as the model made readable. This decision makes it an explorer for desktop and light mode only: record content without teaching text, and the blueprint drawn on the pages it describes.

## Context

The book taught LiteSpec on every page. Type definitions appeared under headings, captions explained what each part meant, and hints repeated from page to page. A reader who knew the records reread the teaching on each visit, and the pages grew long with words that were not record content.

The book also kept every layout and scheme: narrow-screen layouts beside the desktop ones, and a dark scheme beside the light one. Each page had to stay readable in each, while readers opened the book on desktop. The book drew none of the three views the UI blueprint added: the wireframe on each screen, the navigation map on each entry point, and the storyboard on each flow.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Desktop and light only, teaching text out, blueprint views | One calm layout; pages hold records only; blueprint drawn before code. | No narrow layout, no dark scheme; teaching moves to the lexicon. |
| Keep the responsive tutoring book | Every size keeps working; nothing is removed. | Teaching repeats on every page; every layout is kept. |
| Restyle without removing the teaching text | Calmer pages with nothing removed. | Definitions still repeat; pages stay long. |

## Decision

The book assumes a minimum content width of 1024 CSS pixels and ships a light scheme only. The dark scheme, the theme toggle, and the narrow-screen layouts are removed. Its surfaces are a tinted page behind white cards with a visible edge and a soft shadow, a rule under each card header and between rows, and a filled table header. Its text is near-black with muted secondary text, one blue accent for links and the current item, and a visible focus ring.

Teaching text that explains LiteSpec leaves the pages: the type definitions under headings, the captions explaining what each part means, and the repeated hints. The pages keep record content, labels, counts, and states. A reader finds a term's definition on hover and in the lexicon page, not printed beside every use.

Three views draw the blueprint. The screen page draws its wireframe. Each entry point page draws its navigation map from `leadsTo`. Each flow page draws its storyboard: its steps' screens in step order.

## Consequences

The book keeps one layout and one scheme. A page that taught a concept links to the lexicon or drops the teaching where the records need no teaching. The blueprint views read the records decision 0089 adds, so a product owner reviews each screen, each entry point's map, and each flow's strip before any code exists.

## Evidence

A read-only audit of one consumer's book read its performance, navigation, layout, and helper text and found the teaching text repeated beside record content on page after page, with high confidence. The audit also found the narrow-screen layouts and the dark scheme unused by the readers it watched. The blueprint views answer decision 0089, which names them as the book's part of the UI blueprint.

## Revisit Conditions

This decision reopens when readers need the book on narrow screens or in a dark room. It also reopens when readers need the removed teaching text where it stood.
