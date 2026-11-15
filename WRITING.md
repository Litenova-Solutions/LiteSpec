# LiteSpec Writing and Tone Guidelines

Use these guidelines when writing or editing LiteSpec's website, documentation, examples, and tool messages. They apply to every page under `docs/`, the site under `apps/site/`, the READMEs, the templates, the Claimwick example, the finding meanings and fixes in `FindingCodes.cs`, and the messages the tools print. A consumer's own record files follow the [writing standard](docs/core/writing.md), which LiteSpec publishes for them; these guidelines govern how LiteSpec itself is written.

The target style is **plainspoken technical writing**: direct, concrete, calm, and precise.

## 1. Your Role

Write like an experienced developer explaining a tool to a capable colleague who is new to it.

Your job is to help the reader understand something or complete a task. It is not to make LiteSpec sound impressive.

When editing:

- Preserve the technical meaning.
- Improve the order and clarity of the explanation.
- Remove unnecessary wording and repetition.
- Keep the details the reader needs.
- Flag uncertainties rather than filling them with plausible claims.

**Accuracy comes first, clarity second, brevity third.** Shorter is not better if it makes the reader guess.

## 2. Audience

Readers may include technical leads, developers, product people, and AI agents.

Assume they are capable but may not know LiteSpec's terminology, structure, or conventions.

- Don't explain common technical concepts unless the page requires it.
- Explain LiteSpec-specific concepts when readers first need them.
- Don't assume someone has read every preceding page.
- Give enough local context to make a section understandable without repeating the entire product overview.

Write primarily for people. Make rules explicit enough that agents can follow them too. Do not turn explanatory prose into machine-oriented instructions.

## 3. Voice and Tone

The voice stays consistent across the site. The level of detail changes with the task.

| Aim for | Avoid |
|---|---|
| Direct | Abrupt or commanding |
| Technical | Jargon-heavy |
| Confident | Promotional or absolute |
| Approachable | Chatty or overly familiar |
| Concise | Compressed or cryptic |
| Specific | Needlessly exhaustive |
| Honest about limits | Defensive |
| Natural | Formulaic |

Contractions such as "doesn't" and "you'll" are fine in explanatory prose. Use "you" when giving instructions. Use "the project," "the team," or a specific role when responsibility matters.

Avoid personalizing tools unnecessarily. A tool reads, computes, checks, and reports. Don't say it "understands the business" unless that claim is both intentional and supported.

## 4. Lead with What the Reader Needs

Start with the answer, problem, or action, not a description of the page.

**Avoid:**

> This page explains the parts, layers, tools, and conventions that make up LiteSpec.

**Prefer:**

> LiteSpec connects product requirements to the code and tests that implement them.

A brief scope statement is useful when it helps readers decide whether they are in the right place. A contents summary is not a substitute for an opening explanation.

Do not automatically add an "Intent" section to every page. A page opens with a lead paragraph under its title, and that lead focuses on purpose rather than listing the page's headings.

## 5. Use Concrete Subjects and Verbs

Name the thing doing the work and say what it does.

**Avoid:**

> LiteSpec enables durable alignment between intent and implementation.

**Prefer:**

> LiteSpec records requirements in git and checks how they connect to code and tests.

Prefer verbs over abstract noun phrases:

| Instead of | Use |
|---|---|
| Perform validation of | Validate |
| Provide visibility into | Show |
| Facilitate the creation of | Help create, or create |
| Conduct a review of | Review |
| Enable the identification of | Identify, or help identify |

Choose the verb that accurately describes the capability. Don't replace "helps identify" with "identifies" if that strengthens the claim.

Use active voice by default. Passive voice is appropriate when the actor is unknown, unimportant, or less relevant than the result.

## 6. Use Plain Language Without Losing Precision

Prefer familiar words, but keep necessary technical terms.

A precise term is better than a vague simplification. Explain it rather than replacing it with an approximation.

For LiteSpec terminology:

- Use the canonical term consistently.
- Preserve defined capitalization where it matters.
- Distinguish records, layers, tools, outputs, and processes.
- Don't introduce new terms merely to vary the prose.
- Don't alternate between synonyms if readers could mistake them for different concepts.

For example, if **provision** is the defined name for a rule in the standard, introduce that relationship once:

> Each rule in the standard is called a provision.

Then use the term consistently where the distinction matters.

Avoid strained wording such as "the homes of records" when "where records live" or "record locations" is clearer.

## 7. Make Claims at the Right Strength

Distinguish between:

- What the standard **requires**.
- What a record **declares**.
- What a tool **checks**.
- What a test **demonstrates**.
- What the project **aims to achieve**.

These are not interchangeable.

**Avoid:**

> LiteSpec ensures the implementation matches the product's intent.

**Prefer:**

> LiteSpec checks the declared connections between requirements, code, and tests and reports gaps.

Only use the second claim if it accurately describes the tool.

Be especially careful with claims about:

- Complete or correct implementations.
- Security.
- Architectural consistency.
- Passing tests.
- Automated verification.
- Agent behavior.
- Business outcomes.

A reference from code to a record does not, by itself, prove that the code implements the record correctly. Passing tests provide evidence for the cases they cover; they do not prove every possible behavior.

Avoid unsupported guarantees such as:

> Agents make the same choices as a careful senior developer.

Explain the mechanism instead:

> Shared rules reduce the architectural choices each agent has to make.

## 8. Preserve Normative Meaning

When editing standards and reference material, do not accidentally change a rule's strength.

- Preserve required normative keywords and their defined meanings.
- Don't change "must" to "should" to sound friendlier.
- Don't turn a recommendation into a requirement.
- Keep conditions, exceptions, scope, and prerequisites.
- Distinguish a rule from the explanation or example that follows it.

Where useful, separate them:

**Rule:** State the requirement precisely.

**Explanation:** Explain why it exists or how to apply it.

**Example:** Show a valid instance without implying that the example is the only permitted form.

Natural prose must not come at the cost of an ambiguous standard.

## 9. Write Readable Sentences and Paragraphs

Give each sentence a clear main point. Split sentences that contain several independent explanations.

Keep one main idea per paragraph. Two to four sentences is a useful default, not a limit.

Vary sentence length. Avoid both dense compound sentences and a relentless sequence of short statements.

**Too compressed:**

> Records declare. Code implements. Tests prove. The model connects.

**Clearer:**

> Records describe the expected behavior. Code implements it, and tests check the declared scenarios. The model connects these records to their implementation and test evidence.

Do not remove articles, transitions, or qualifications just to reduce word count.

## 10. Organize Around the Reader's Task

Choose a structure that fits the page instead of applying the same template everywhere. Where a page style fixes the sections (a standards page's Standards and Conventions, a command page's Synopsis and Options), keep those sections and shape the writing inside them to the reader's task.

### Overview Pages

Explain:

- The problem.
- The approach.
- The main parts.
- The limits.
- Where to go next.

### Guides

Explain:

- What the reader will accomplish.
- What they need before starting.
- What to do, in order.
- What they should see.
- How to address likely failures.

### Concept Pages

Explain:

- What the concept means.
- Why it exists.
- How it works.
- A concrete example.
- Important boundaries or exceptions.

### Reference Pages

Prioritize:

- Exact definitions.
- Fields and accepted values.
- Required and optional behavior.
- Constraints.
- Exceptions.
- Minimal examples.

Reference pages can be dense when readers need completeness. Do not force them into a conversational narrative.

### Tool and Error Messages

State:

1. What happened.
2. Where it happened.
3. How to fix it, if known.

Avoid blame, jokes, and vague messages such as "Something went wrong."

## 11. Use Formatting to Clarify Structure

### Headings

Use headings that identify a subject, task, or question.

Prefer:

- Where Records Live
- Check the Model
- How Access Is Declared
- What LiteSpec Doesn't Cover

Avoid:

- Unlocking Value
- A Better Way Forward
- Powerful Capabilities

Follow the site's heading capitalization convention consistently: Title Case on every heading, as the [authoring standard](docs/core/authoring.md) states.

### Lists

Use bullets for related items. Use numbered lists when order matters.

Keep items parallel: all actions, all questions, all components, or all outcomes.

Do not turn every paragraph into bullets. Do not force explanations into groups of three.

### Tables

Use tables for comparisons or repeated attributes.

Avoid tables when:

- Most cells contain paragraphs.
- A simple list would be easier to scan.
- The columns don't add a useful distinction.

### Emphasis

Use bold sparingly for labels and important distinctions.

Use code formatting for commands, paths, identifiers, fields, and literal values.

Do not bold every key phrase or use formatting as a substitute for clear structure.

## 12. Use Examples That Explain Something

Place examples near the idea they clarify.

Prefer concrete examples from the documented product over invented scenarios when suitable examples already exist.

For LiteSpec, Claimwick examples can show how requirements, records, handlers, and tests connect.

Every example should have a purpose:

- Explain a relationship.
- Show valid syntax.
- Demonstrate a command.
- Clarify a boundary.
- Show an expected result.

Preserve exact commands, paths, IDs, field names, and code syntax.

Do not silently alter examples to make them easier to explain. If an example appears inconsistent, flag it for review.

Label incomplete snippets and placeholders clearly. Keep commands copyable; don't put explanatory prose inside a command block.

## 13. Keep Links Useful

Use descriptive link text that tells readers what they will find.

**Avoid:**

> Click here to learn more.

**Prefer:**

> [Completion Stages](docs/core/model.md#completion-stages) defines the requirements for each stage.

Put links where the related question arises. Don't repeat the same link in every nearby paragraph.

Keep enough explanation on the current page for it to make sense. A link should provide depth, not replace a necessary definition.

Preserve existing URLs unless changing them is part of the task.

## 14. Avoid Formulaic and Promotional Writing

Do not try to sound human by adding casual filler. Sound natural by being specific and clear.

Avoid habitual phrases such as:

- "Let's dive in."
- "Here's the thing."
- "In today's rapidly evolving landscape."
- "It's that simple."
- "Unlock the power of..."
- "Seamlessly..."
- "A robust and comprehensive solution..."

Also avoid repeated rhetorical patterns:

- "It's not just X; it's Y."
- "Not X. Not Y. Just Z."
- Repeated three-part slogans.
- A summary sentence after every section.
- An opening paragraph that previews every heading.
- A conclusion that repeats the introduction without adding anything.

These structures are not forbidden in isolation. The problem is using them automatically instead of explaining the subject.

Do not replace promotional prose with equally artificial minimalism.

## 15. Handle Repetition and Detail Deliberately

Explain each concept fully where it belongs. Elsewhere, give a brief reminder and link to the detailed explanation.

Keep repetition when it helps readers use a section independently or prevents a consequential mistake.

Remove repetition that merely restates the same benefit.

Likewise, keep details that affect:

- What the reader does.
- What the system means.
- Whether an example is valid.
- How a rule applies.
- What a result does or does not establish.

Move secondary detail to a reference page when appropriate. Do not remove it merely because it is technical.

## 16. Editing and Rewriting Existing Content

A rewrite should make the existing meaning easier to understand. It should not introduce new product claims, change requirements, or add a persuasive narrative.

Treat the source as material to clarify: not as a sentence structure to preserve and not as permission to invent missing explanations.

### Set the Scope of the Edit

Distinguish between three kinds of work:

- **Copyedit:** Improve grammar, punctuation, word choice, and sentence flow. Preserve the organization.
- **Rewrite:** Reorganize sentences, paragraphs, and sections to improve understanding. Preserve technical meaning and required document structure.
- **Technical revision:** Change requirements, architecture, capabilities, examples, or definitions. Do this only when explicitly requested or supported by an authoritative source.

When asked to make a page clearer or more readable, treat the task as a **rewrite**, not a technical revision.

### Identify What Must Not Change

Before rewriting, identify the technical details that must remain intact:

- Requirements and their strength.
- Permissions, prohibitions, conditions, and exceptions.
- Scope and responsibility.
- Dependency direction.
- Placement and ownership rules.
- Defined terminology.
- Commands, paths, identifiers, field names, and finding codes.
- The meaning of examples.
- Required headings, metadata, and provision fields.

Preserve these details even when moving or rephrasing them.

Small wording changes can change a rule:

| Original | Unsafe rewrite | Why |
|---|---|---|
| A node **may contain** layer folders. | A node **contains** layer folders. | Permission becomes an apparent requirement or fact. |
| Endpoints **without a handler** live in the host. | Endpoints **outside a use case** live in the host. | The placement condition changes. |
| Types used by **two or more modules**. | Types used by **every module**. | The scope changes. |
| Domain code does not depend on infrastructure. | Infrastructure changes never affect domain behavior. | A dependency restriction becomes an outcome guarantee. |

Check meaning throughout the page, not only in formal requirement statements.

### Improve Structure Before Polishing Sentences

Do not limit a rewrite to adding commas and paragraph breaks.

First identify the separate ideas in a dense passage. For example:

- What a project contains.
- Which projects it references.
- How namespaces are formed.
- Where endpoints belong.
- Which exceptions apply.

Then organize those ideas into readable paragraphs, lists, or subsections.

Use prose for connected explanations. Use lists for distinct responsibilities or rules. Use tables for comparisons and repeated attributes.

Do not preserve a long inventory in the opening merely because it appeared there originally. Move it to the relevant section, or remove it if the headings already provide the same information.

### Clarify Before Expanding

First improve the supplied explanation using the information already available.

Add explanation only when:

- It resolves a specific ambiguity or helps the reader perform the task.
- It is supported by the source or an authoritative reference.
- It belongs on that page.

Do not add:

- Imagined failure scenarios.
- Generic arguments for standardization.
- New dependency allowances.
- Unverified reasons for architectural decisions.
- Claims about what developers or agents will always do.
- Guarantees about outcomes.

**Source:**

> Domain code does not depend on outer layers.

**Acceptable rewrite:**

> Keep dependencies on outer layers out of domain code.

**Unsupported expansion:**

> Changing the database or web framework never requires changes to business behavior.

Longer is acceptable when it clarifies existing meaning. Added explanation is not automatically added value.

### Match the Rewrite to the Page Type

Do not apply landing-page framing to standards and reference pages.

For a **standard or reference page**:

- State the subject and scope directly.
- Prioritize definitions, constraints, conditions, and exceptions.
- Keep rationales close to the rules they explain.
- Preserve normative wording and required provision fields.
- Avoid problem stories unless they explain a non-obvious rule.

For a **guide**:

- Organize around the task.
- Put prerequisites before instructions.
- State expected results and relevant failure cases.

For an **overview or landing page**:

- Explain the problem and approach.
- Describe benefits through supported mechanisms.
- Keep scope and limits visible.

Do not automatically add sentences such as:

> Without these rules, every team invents its own structure.

> You need this page whenever you write or review code.

Use that space for information the reader needs.

### Replace Awkward Language, Not Just Its Punctuation

A grammatically correct sentence can still be difficult to understand.

Replace unusual or figurative phrasing with ordinary technical language, unless the phrase has a defined meaning.

| Awkward phrasing | Clearer alternative |
|---|---|
| Code that names several modules | Code that references several modules |
| A module type reads... | A module namespace is... |
| A closed set refuses other folders | Only the listed folders are allowed |
| What another module may name | What another module may reference |
| This page holds the .NET bindings | This page defines how the rules apply to .NET |

Choose replacements based on context. For example, "references," "uses," and "depends on" are not always interchangeable.

Do not retain obscure wording merely because it sounds distinctive.

### Preserve Required Structure

Keep required:

- Section headings.
- Metadata.
- Provision IDs.
- Strength and applicability fields.
- Statements, rationales, examples, and check descriptions.
- Links and anchors.

Do not remove an `Intent` section because overview pages usually work better without one. A required documentation structure takes precedence over general style preferences.

Within that structure, improve the content. An `Intent` section should state purpose and scope clearly, not provide a long preview of every heading.

LiteSpec's own page contract no longer requires an `Intent` section: a page opens with a lead paragraph under its title, as `litespec/core/authoring#open-each-page-with-a-lead` states. The lead follows the same rule: purpose and scope, not a preview of every heading.

### Check for Contradictions

Compare prose, normative statements, tables, examples, and check descriptions.

Look for disagreements about:

- Allowed folders or values.
- Required versus optional elements.
- Ownership and placement.
- Dependency direction.
- Exceptions.
- What tools actually check.

For example:

> Subfolders are unrestricted inside layer folders.

may conflict with a table stating:

> Use-case `Application/` and `WebApi/` folders: files only.

Do not silently choose an interpretation or invent an exception.

Resolve the conflict from an authoritative source when available. Otherwise, identify it in a separate editorial note. Do not present the page as technically resolved.

### Preserve Meaning When Cutting

Remove repetition, not requirements.

Before deleting a sentence, ask whether it contains a unique:

- Definition.
- Condition.
- Exception.
- Responsibility.
- Scope boundary.
- Explanation needed to apply a rule.

If it does, retain that information or move it to a better location.

A definition should not disappear merely because it interrupts the opening paragraph. Move it to the concept section if that is where it belongs.

### Keep Rationales Factual and Local

A rationale should explain why a particular rule exists.

Prefer concrete consequences:

> Matching namespaces to folders makes types easier to locate.

Avoid exaggerated counterfactuals:

> Without this alignment, every navigation starts with a search.

Do not turn each rationale into a general argument for LiteSpec. If the existing rationale already explains the rule, clarify it without adding another justification.

### Review the Meaning After Rewriting

Compare the finished rewrite with the source.

Check:

- Did any permission become a requirement?
- Did any condition become broader or narrower?
- Did any definition or exception disappear?
- Did a design constraint become an outcome guarantee?
- Did the rewrite imply a new capability?
- Did it change what a tool checks or what a test establishes?
- Did any example stop matching the surrounding rule?
- Did a required section or field disappear?

A smoother sentence is not an improvement if it changes the technical meaning.

### Editing Workflow

Use this sequence:

1. **Identify the page type and reader's task.**
2. **Identify required structure and technical details that must remain unchanged.**
3. **Check for ambiguity and contradictions.**
4. **Reorganize the explanation around distinct ideas.**
5. **Rewrite awkward or abstract language.**
6. **Remove filler, repetition, and unnecessary framing.**
7. **Compare the rewrite with the source for semantic changes.**
8. **Check examples, commands, identifiers, and links.**
9. **Read for natural flow without making the prose chatty or choppy.**
10. **Return the draft and list unresolved issues separately.**

### Output Requirements

Unless asked otherwise:

- Return publication-ready Markdown.
- Keep editorial notes outside the page content.
- Briefly identify unresolved technical questions.
- Do not silently repair technical inconsistencies.
- Do not claim validation that was not performed.
- Do not add an editing commentary when the user only requested the rewritten page.

**Success criterion:** The reader should understand the same technical content with less effort, without receiving new assumptions disguised as clarification.

## 17. Output Expectations

Unless the task asks for something else:

- Return the complete, publication-ready Markdown.
- Preserve working links and valid code examples.
- Do not add a long explanation of your editing choices.
- Put unresolved factual questions in a short note after the draft.
- Clearly separate editorial notes from content intended for publication.
- Do not claim to have validated behavior, commands, or links unless you actually did.

## 18. Final Review Checklist

Before returning the page, check:

- Does the opening give the reader useful information immediately?
- Is the structure appropriate for this kind of page?
- Are unfamiliar LiteSpec terms introduced when needed?
- Are technical terms used consistently?
- Does each paragraph have a clear main point?
- Are requirements distinct from recommendations and aspirations?
- Are automated checks described within their actual limits?
- Are commands, paths, IDs, examples, and links preserved accurately?
- Have important conditions and exceptions survived the edit?
- Are examples concrete and relevant?
- Are lists and tables genuinely useful?
- Have promotional language, filler, and repetitive summaries been removed?
- Does the page sound natural without becoming chatty or choppy?
- Can the reader understand or act without guessing?

**Final test:** Would a knowledgeable colleague write this to help someone understand the system, or would a company write it to sound impressive? Choose the former.
