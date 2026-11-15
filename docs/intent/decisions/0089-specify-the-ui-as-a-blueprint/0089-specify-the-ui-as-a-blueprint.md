# Specify the UI as a Blueprint

A screen has two halves that change for different reasons. This decision states the blueprint of each screen in the records and leaves the look and the build to the implementation, with a checked binding between the two.

## Context

The `screen` record carried an address or a host, the use cases it offered and the flow steps it served. It carried no layout, no content and no wording, so a project claiming only Intent and Behavior had nowhere to write down what its screens were.

Product choices sat in the implementation. The design contract inside the shared UI package required Brand, Voice, Do and Do Not beside token values and component names. The page frame each screen composed lived in the contract's `screens` map, not on the screen. The sentence a person read for a refusal lived in a frontend dictionary file.

Tokens were pinned by value. The baseline values were fixed, so a palette more readable than the baseline still needed a departure, and the goals behind them, readable text and findable controls, were stated as limits only in part. One consumer found the baseline's light-scheme borders too faint for screens full of amounts, drew its own surfaces, and needed a departure for a palette more readable than the baseline. Its design contract drew no governed provisions, because it claimed Intent and Behavior only. Its mockup input borders measured about 2:1 against the 3:1 rule, and only a hand-run script caught it, because no record held the limit for a tool to read.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Blueprint and limits in the records, values and build in the implementation, binding checked | Intent-and-Behavior projects specify screens; readability is checked; the book shows each screen before code. | New fields, a token format, six checks, three book views; the contract shrinks. |
| Keep the UI in the implementation only | Nothing changes. | No screen blueprint; readable palettes need departures; limits live in scripts. |
| Specify the full design in the records | One place holds everything. | The specification drifts with every restyle. |
| Link out to a design tool | Designers keep their own tool. | Not readable offline, not versioned, not bound to use cases. |

## Decision

The specification states what each screen is for, what it shows, what it lets a person do, what it says, and the measurable limits it meets. The implementation states how it looks and what it is built with. A small binding connects the two, and `model check` verifies it.

The `product` record carries the brand: the reader, the one thing the product is accountable for on every screen, three words it is and three it is not, and the rules every screen keeps. The `entry-point` record carries the audience's density and voice. Each screen names its page-frame pattern with the option the product chose, draws one wireframe in its explanation file, and names the screens it leads to. Each refusal result carries the sentence a person reads, with one sentence per language beside it.

LiteSpec names each token role with the limits its values meet. The values live in one file in the W3C Design Tokens format 2025.10, and `model check` evaluates every limit in every declared color scheme. The six measurable UI targets ship as quality requirements in the consumer preset. The design contract keeps only what the implementation knows: the baseline, the token file path, the component behind each page frame, the vocabulary, the motion, and the browser target.

## Consequences

No new record type is added. The brand belongs to the product, density and voice belong to the audience an entry point models, and the blueprint belongs to the screen. A screen names its page frame by its pattern, such as `record-page`, never by a component, so the record names a product choice. The baseline palette stays as default values that pass the limits; a project's own palette needs no record while it meets them, and a departure is needed only to lower a limit.

The wireframe draws the screen's structure from the domain: each action names a use case the screen offers, each state names a glossary term of the record shown, and each refusal names a result of an offered use case. The book draws each wireframe on its screen page, each entry point's screens as a navigation map from `leadsTo`, and each flow as a strip of its screens in step order.

Money and moment forms come from the entry-point record: `languages` names the languages the entry point serves with the first as the default, `formats` names the money and moment options from the money and moment patterns, and each screen renders them with the locale data of the reader's language and the currency and country the record carries, with no format string, separator, or symbol in code.

## Evidence

The faint baseline borders, the empty `rules` output for a design contract outside the claimed layers, and the 2:1 input borders found by a hand-run script were read from one consumer's records and mockups with high confidence. The W3C Design Tokens Community Group published the first stable Design Tokens specification, version 2025.10, on 2025-10-28, as a Final Community Group Report. The navigation map follows breadboarding, which draws an interface as places, affordances and connections before visual design, as Shape Up chapter 4 describes it.

## Revisit Conditions

This decision reopens when the blueprint cannot carry a screen a consumer must specify. It also reopens when the token limits refuse a palette readers find more readable.
