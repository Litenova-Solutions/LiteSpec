using LiteSpec.Authoring;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Authoring;

/// <summary>
///     The minimal LiteSpec tree every standards case mutates, so each case isolates one rule.
///     Each case starts from <see cref="Base"/> and changes one file.
/// </summary>
static class StandardsFixture
{
    /// <summary>The topic page every case mutates, with maintainer evidence.</summary>
    public static readonly string TopicPage = Normalize("""
        # Topic Standard

        **Stack:** any
        **Layer:** records

        ## Intent

        This page defines one repository topic.

        ## Standards

        ### Keep the Topic Bounded

        **ID:** `litespec/core/topic#keep-the-topic-bounded`

        **Strength:** rule

        **Statement:** Consumers MUST keep the topic inside its declared boundary.

        **Rationale:** The boundary gives reviewers one testable result.

        **Checked by:** `litespec-maintain validate` confirms the topic stays inside its declared boundary.

        ## Conventions

        ### Use the Default Location

        **ID:** `litespec/core/topic#use-the-default-location`

        **Strength:** default

        **Default:** Store the topic in its owning documentation area.

        **Replacement:** A consumer can name another location through an explicit local convention.

        **Reviewed for:** Does the topic live in its owning documentation area?
        """) + "\n";

    /// <summary>The pattern page: one pattern with two drawn options.</summary>
    public static readonly string PatternPage = Normalize("""
        # Sample Patterns

        **Stack:** any
        **Layer:** frontend-ui

        ## Intent

        This page offers drawn options for one recurring question.

        ## Patterns

        ### Record Header

        **ID:** `litespec/frontend/patterns/sample#record-header`

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
            heading "SO-1042"
            action primary "Mark as shipped"
        ```

        **Rationale:** The next step is plain.

        **Cost:** Other commands wait in a menu.

        #### `two-equal`: Two Equal Actions

        ```wireframe
        page
          header
            heading "SO-1042"
            action "Mark as shipped"
            action "Refund"
        ```

        **Rationale:** Both commands are one click away.

        **Cost:** The reader compares labels before acting.
        """) + "\n";

    /// <summary>The extension page, with maintainer evidence.</summary>
    public static readonly string ExtensionPage = Normalize("""
        # Sample Extension

        **Stack:** any
        **Layer:** operations

        ## Intent

        This extension adds one optional capability.

        ## Activation

        Activation scope: `local`.

        Applicable specification kinds: `use-case`.

        Enable this extension when the sample capability is required.

        ## Baseline Relationship

        This extension replaces no baseline provision.

        ## Standards

        ### Record Extension Activation

        **ID:** `litespec/ext/sample#record-extension-activation`

        **Strength:** rule

        **Statement:** Consumers MUST record activation on each applicable specification.

        **Checked by:** `litespec-maintain validate` confirms each applicable specification records its activation.

        ## Conventions

        None.

        ## Dependencies

        None.
        """) + "\n";

    /// <summary>The stack page, with maintainer evidence.</summary>
    public static readonly string StackPage = Normalize("""
        # Sample Stack

        **Stack:** dotnet
        **Layer:** domain

        ## Intent

        This stack selects one standards composition.

        ## Standards

        ### Apply the Complete Stack

        **ID:** `litespec/backend/sample/stack#apply-the-complete-stack`

        **Strength:** rule

        **Statement:** Consumers MUST apply every document in the selected stack composition.

        **Checked by:** `litespec-maintain validate` confirms every document in the selected stack composition.

        ## Conventions

        None.
        """) + "\n";

    /// <summary>The scope vocabulary page, with no provisions of its own.</summary>
    public static readonly string ScopesPage = Normalize("""
        # Provision Scopes

        ## Intent

        This page states the shared situation vocabulary each provision carries.

        ## Concepts

        ### The Four Fields

        Each provision carries four scope fields.

        | Field | Answers | Values |
        |:---|:---|:---|
        | `stack` | Which technology | `any`, `dotnet`, `react` |
        | `layer` | Which part of the system | `records`, `domain`, `application`, `operations`, `frontend-ui`, `tests` |
        | `artifact` | Which kind of thing is made | `aggregate`, `test`, `record:<type>` |
        | `strength` | How binding it is | `rule`, `default`, `advice` |

        ### Stack Values

        | Value | Meaning |
        |:---|:---|
        | `any` | No technology; the provision applies to every stack. |
        | `dotnet` | The .NET backend stack. |
        | `react` | The React web stack. |

        ### Layer Values

        | Value | Meaning |
        |:---|:---|
        | `records` | Specification records and standards pages. |
        | `domain` | Business state, rules, and language. |
        | `application` | Use-case coordination and the pipeline. |
        | `operations` | Releases, deployment, and support. |
        | `frontend-ui` | Components, routes, and rendering. |
        | `tests` | The suites that prove behavior. |

        ### Artifact Values

        | Value | Meaning |
        |:---|:---|
        | `aggregate` | The aggregate root with the entities it guards. |
        | `test` | One test. |
        | `record:<type>` | One record of that type, such as `record:use-case`. |

        ### Strength Values

        | Value | Meaning |
        |:---|:---|
        | `rule` | Follow it; deviate only through a recorded exception. |
        | `default` | Follow it unless the project chooses otherwise, and record the choice. |
        | `advice` | Experience; apply it with judgement and record nothing. |

        ## Standards

        None.

        ## Conventions

        None.
        """) + "\n";

    /// <summary>The how-to page.</summary>
    public static readonly string GuidePage = Normalize("""
        # Sample Guide

        ## Purpose

        Create one verified standards artifact.

        ## Prerequisites

        - Read the applicable canonical provision.

        ## Procedure

        1. Create the artifact in its owning directory.
        2. Run the applicable repository validator.

        ## Check

        - Confirm that the validator exits with code zero.
        """) + "\n";

    /// <summary>The tutorial page.</summary>
    public static readonly string TutorialPage = Normalize("""
        # Sample Tutorial

        ## Purpose

        Produce one working standards artifact.

        ## Prerequisites

        - Install the repository validator.

        ## Lesson

        1. Create the artifact in its owning directory.
        2. Run the repository validator.

        ## What You Built

        - Confirm that one validated artifact sits in its owning directory.
        """) + "\n";

    /// <summary>The reference page.</summary>
    public static readonly string ReferencePage = Normalize("""
        # Sample Reference

        ## Intent

        State the exact values that this repository publishes.

        ## Reference

        | Name | Value |
        |:---|:---|
        | Sample | One published value. |

        ## Notes

        The values above come from the repository manifest.
        """) + "\n";

    /// <summary>The research record for <c>docs/intent/research/sample-study.yaml</c>, as JSON, with maintainer evidence.</summary>
    public static readonly string ResearchJson = Normalize("""
        {
          "type": "research",
          "id": "research/sample-study",
          "title": "Sample Study",
          "conclusion": "The sample shows one finding.",
          "sources": [
            {
              "url": "https://example.com/sample",
              "readOn": "2026-10-30"
            }
          ]
        }
        """) + "\n";

    /// <summary>The decision record for <c>docs/intent/decisions/0001-sample-choice.yaml</c>, as JSON, with maintainer evidence.</summary>
    public static readonly string DecisionJson = Normalize("""
        {
          "type": "decision",
          "id": "decision/0001-sample-choice",
          "title": "Sample Choice",
          "options": [
            {
              "name": "Keep the sample",
              "pros": "The sample is small.",
              "cons": "The sample is narrow."
            }
          ],
          "choice": "Keep the sample",
          "reopenedWhen": "A larger sample disagrees.",
          "basedOn": ["research/sample-study"],
          "consequences": "The sample stays beside `litespec/core/topic#keep-the-topic-bounded`."
        }
        """) + "\n";

    /// <summary>The research explanation for <c>docs/intent/research/sample-study.md</c>.</summary>
    public static readonly string ResearchExplanation = Normalize("""
        # Sample Study

        ## Answer

        The sample shows one finding with high confidence.

        ## Notes on Sources

        One web source, read in October 2026.
        """) + "\n";

    /// <summary>The decision explanation for <c>docs/intent/decisions/0001-sample-choice.md</c>.</summary>
    public static readonly string DecisionExplanation = Normalize("""
        # Sample Choice

        ## Context

        The sample forces one choice.

        ## Decision

        Keep the sample.
        """) + "\n";

    /// <summary>The command page. The validator checks its sections, not its sample command.</summary>
    public static readonly string CommandPage = Normalize("""
        # Sample Command

        ## Name

        `node tools/sample.mjs` runs one sample check.

        ## Synopsis

        ```bash
        node tools/sample.mjs [root]
        ```

        ## Description

        Run this command after changing the sample material.

        ## Arguments

        | Argument | Required | Effect |
        |:---|:---|:---|
        | `root` | no | Selects the repository to read. |

        ## Options

        None.

        ## Exit Codes

        | Code | Meaning |
        |:---|:---|
        | `0` | The check passed. |
        | `1` | The check reported a defect. |

        ## Examples

        ```bash
        node tools/sample.mjs
        ```

        ## Underneath

        None.
        """) + "\n";

    /// <summary>
    ///     The routing block a documentation root index names before it links to one.
    /// </summary>
    public static readonly string IndexRouting =
        "## Which Page Do You Want\n\n" +
        "A tutorial takes you through a first working result, one step at a time.\n" +
        "A how-to solves one stated problem for a reader who already has it running.\n" +
        "A reference states exact values, options, and defaults.\n" +
        "A command page documents one command and what it runs underneath.\n";

    /// <summary>Writes a file inside the temporary repository, creating its folder.</summary>
    /// <param name="root">The temporary repository root folder.</param>
    /// <param name="relative">The file path under the root, with forward-slash segments.</param>
    /// <param name="contents">The file contents to write.</param>
    public static void Write(string root, string relative, string contents)
    {
        var file = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, contents.Replace("\r\n", "\n"));
    }

    /// <summary>Deletes a file inside the temporary repository when it exists.</summary>
    /// <param name="root">The temporary repository root folder.</param>
    /// <param name="relative">The file path under the root, with forward-slash segments.</param>
    public static void Delete(string root, string relative)
    {
        var file = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(file))
        {
            File.Delete(file);
        }
    }

    /// <summary>Converts a JSON record to its canonical YAML record file text.</summary>
    /// <param name="json">The record as JSON.</param>
    /// <returns>The record file text.</returns>
    public static string Yaml(string json) =>
        LiteSpec.Model.YamlWriter.Write(LiteSpec.Model.JsonText.Parse(json));

    /// <summary>Writes the sample research record file with its explanation file.</summary>
    /// <param name="root">The temporary repository root folder.</param>
    /// <param name="json">The record as JSON, or null for the sample record.</param>
    /// <param name="explanation">The explanation text, or null for the sample explanation.</param>
    public static void WriteResearch(string root, string? json = null, string? explanation = null)
    {
        Write(root, "docs/intent/research/sample-study.yaml", Yaml(json ?? ResearchJson));
        Write(root, "docs/intent/research/sample-study.md", explanation ?? ResearchExplanation);
    }

    /// <summary>Writes the sample decision record file with its explanation file.</summary>
    /// <param name="root">The temporary repository root folder.</param>
    /// <param name="stem">The record path without its extension.</param>
    /// <param name="json">The record as JSON, or null for the sample record.</param>
    /// <param name="explanation">The explanation text, or null for the sample explanation.</param>
    public static void WriteDecision(string root, string stem, string? json = null, string? explanation = null)
    {
        Write(root, stem + ".yaml", Yaml(json ?? DecisionJson));
        Write(root, stem + ".md", explanation ?? DecisionExplanation);
    }

    /// <summary>The minimal project file a mark case writes beside its sources.</summary>
    public static readonly string ProjectFile = "<Project Sdk=\"Microsoft.NET.Sdk\" />\n";

    /// <summary>Writes a C# file into a .NET project, creating the project file beside it.</summary>
    /// <param name="root">The temporary repository root folder.</param>
    /// <param name="project">The project folder path under the root.</param>
    /// <param name="file">The source file name inside the project folder.</param>
    /// <param name="contents">The source file contents to write.</param>
    public static void WriteSource(string root, string project, string file, string contents)
    {
        var name = project.Split('/')[^1];
        Write(root, $"{project}/{name}.csproj", ProjectFile);
        Write(root, $"{project}/{file}", contents);
    }

    /// <summary>Regenerates the provision index and its data file from the current standards.</summary>
    /// <param name="root">The temporary repository root folder.</param>
    public static void RebuildIndex(string root)
    {
        Write(root, Provisions.IndexPath, Provisions.BuildIndex(root) + "\n");
        Write(root, Provisions.JsonPath, Provisions.BuildJson(root) + "\n");
    }

    /// <summary>
    ///     Writes the minimal repository every case mutates. Checkable evidence
    ///     names <c>litespec-maintain validate</c>, which needs no stub file, so this tree
    ///     carries no <c>tools/</c> stub.
    /// </summary>
    /// <param name="root">The temporary repository root folder.</param>
    public static void Base(string root)
    {
        Write(root, "docs/core/topic.md", TopicPage);
        Write(root, "docs/core/scopes.md", ScopesPage);
        Write(root, "docs/ext/sample.md", ExtensionPage);
        Write(root, "docs/backend/sample/stack.md", StackPage);
        Write(root, "docs/frontend/patterns/sample.md", PatternPage);
        Write(root, "docs/guide/sample.md", GuidePage);
        Write(root, "docs/README.md", "# Documentation\n\n## Intent\n\nUse this page to find repository documentation.\n\n" + IndexRouting);
        Write(root, "docs/tutorial/sample.md", TutorialPage);
        Write(root, "docs/reference/sample.md", ReferencePage);
        Write(root, "docs/tools/sample.md", CommandPage);
        Write(root, "docs/reference/glossary.md", "# Glossary\n\n## Sample\n\nOne sample term.\n");
        RebuildIndex(root);
    }

    /// <summary>Replaces the first occurrence of one substring with another.</summary>
    /// <param name="text">The text to change.</param>
    /// <param name="oldValue">The substring to replace.</param>
    /// <param name="newValue">The replacement text.</param>
    /// <returns>The text with the first occurrence replaced, or the text itself when absent.</returns>
    public static string ReplaceFirst(string text, string oldValue, string newValue)
    {
        var index = text.IndexOf(oldValue, StringComparison.Ordinal);
        return index < 0 ? text : text[..index] + newValue + text[(index + oldValue.Length)..];
    }

    /// <summary>The shipped project schema, read from the real repository.</summary>
    /// <param name="root">The repository root to read from, or null for the test repository.</param>
    /// <returns>The project schema text.</returns>
    public static string ShippedProjectSchema(string? root = null) =>
        File.ReadAllText(Path.Combine(root ?? Cli.Repository, "schemas", "litespec-project.schema.yaml"));

    static string Normalize(string value) => value.Replace("\r\n", "\n");
}
