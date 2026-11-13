# Use Plain Noun Sections and an Opening Paragraph

Explanation files tell a reader what each record means. Until now, every record type had its own headings, and some of them were questions or stories. This decision gives each record type one fixed list of plain noun sections, and it has each explanation file open with one paragraph before the first section. A reader who knows one record knows where to look on every other record of the type.

## Context

Before this release, each record type had its own headings. Eight types had a fixed list, such as Who Walks and Why or What Would Retire It, and every other type had one generic heading, What It Means. Some headings were questions, and some were stories, so the same kind of content sat under different words on each type. The check for explanation files knew only the eight lists, so it could not require the same content on the other types.

Three options were weighed. Keeping the old headings preserves the author's voice, but a reader cannot predict a section's name, and the check must list every phrasing. Keeping only the H1 and letting each file choose its sections gives authors the most freedom, but no check can tell whether a limit, a cost or an example is present. A fixed list of plain nouns for each type gives every reader the same map, and the one paragraph before the first section tells a cold reader what the record is.

## Options

LiteSpec compared 3 options for the headings of explanation files. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Keep the headings each type had, phrased as questions and stories | Each heading reads as the question a reader asks, in the author's voice. | Headings differ from type to type, a check must list every phrasing, and the types without a list share one generic heading. |
| One fixed list of plain nouns per type, with one opening paragraph | Each name says what its section holds, and a cold reader learns what the record is before any section. | Every explanation written under the old headings must be rewritten, and a noun heading carries less voice. |
| The H1 only, with each file choosing its own sections | No table to keep, and each author writes the sections a record needs. | No check can see whether a limit, a cost or an example is present, and the same content gets a different name on each record. |

## Decision

LiteSpec gives each record type one fixed list of plain noun sections, in the order the writing standard's section table gives. Each section is named by one or two plain nouns, such as Cost, Evidence or Revisit Conditions. Questions and stories are not section names.

Each explanation file opens with its H1 and one paragraph of prose, before the first H2. The paragraph says what the record is in plain words, who it concerns and why it matters.

## Consequences

The checks read the new lists. `explanation-missing` reports a missing section, and it also reports sections that come out of order. The new `explanation-opening-missing` finding reports an explanation with no paragraph before its first section.

`litespec new` writes the template for every type that has one, with the title filled in. The generic explanation template is removed, because every type now has its own.

The old headings are invalid in LiteSpec 5.3.0. No conversion tool ships, so each consumer rewrites its explanation files by hand, as the changelog's upgrade step describes.

## Revisit Conditions

This decision holds until a record type needs a section that no plain noun can name. It also holds until consumers report that the opening paragraph repeats the summary field so often that it adds nothing to the reading. Either report reopens the choice.
