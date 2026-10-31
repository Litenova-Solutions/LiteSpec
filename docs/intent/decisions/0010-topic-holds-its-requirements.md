# Topic Holds Its Requirements

## Context

Requirements are grouped three ways at once: the page groups them by topic area, the parent rule points each at a feature, and the journey that walks them sits under a goal. Most requirements name no parent at all, so one offer's story spreads over three files and the parent rule reports gaps the authors never felt as mistakes.

## Decision

The topic page becomes a record type, `topic`, identifier `topic/<name>`, and the parent of its requirements by identifier containment: a requirement's identifier `requirement/<topic>.<name>` names its topic, and a topic segment that differs from its page's name is an `identifier-invalid` finding. A requirement names its reasons in one unordered list, `cites`, holding any of goal, problem, feature, constraint, research and decision. A requirement that cites nothing is `requirement-cites-missing`, at check level A, or G when the requirement carries `risk`. Only Behavior records fulfill a requirement: rule, use case, reaction, job, notification and setting. A journey names the requirements it shows in `cites`. A constraint's What It Requires section becomes a generated region listing the requirements that cite it, and a constraint with `applies: true` that no requirement cites is `constraint-unreached` at check level G.

## Evidence with Confidence

`research/intent-hierarchy-practice` holds the outside comparison and the measured counts over the largest consumer behind D1 to D5, high confidence.
