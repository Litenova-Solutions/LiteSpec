# {{task}}: check slice {{id}} against its specification

A worker wrote slice `{{id}}` in wave {{wave}}. Another step checks that it compiles. Your question is different: does the code do everything the specification below says, and nothing it refuses?

Read only. Do not edit any file, and run no command other than reading and searching.

## How to review

1. Read the specification at the end of this brief, line by line. List every rule, refusal, field, state, event, permission and screen it names.
2. For each one, find where the changed files (listed below) implement it. Open the files; do not trust names.
3. Read the slice's example, {{example}}, and compare the shape: layers, naming, tags, audit declaration, events.
4. A finding is something the specification requires that the code lacks or contradicts, or a shape that breaks the example's pattern. Style preferences are not findings.

## Answer

Write `{{run}}/done/{{task}}.md` with exactly this shape, one finding per line, each naming the file:

```markdown
## Findings
- <path>[:line]: the specification says <what, quoted briefly>; the code <what it does instead, or "has nothing for it">.
```

If everything holds, write one line under the heading: `- none`.
