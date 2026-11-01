using System.Text.RegularExpressions;

namespace LiteSpec.Authoring;

/// <summary>
///     A glossary compiled once so a scan over many files does not rebuild every pattern per file.
/// </summary>
public sealed class CompiledLanguage
{
    /// <summary>
    ///     A mannered term with its literal replacement and its matcher.
    /// </summary>
    /// <param name="Term">The metaphor or stock phrase.</param>
    /// <param name="Instead">The literal replacement named in the diagnostic.</param>
    /// <param name="Pattern">The whole-word matcher.</param>
    /// <param name="FileFilter">The term, or its first word when it holds a space: a file without it holds no match.</param>
    /// <param name="FileFilterIsAscii">Whether the file filter holds ASCII only, so an ordinal search prefilters it.</param>
    /// <param name="TermIsAscii">Whether the term holds ASCII only, so an ordinal search prefilters it.</param>
    internal sealed record ManneredTerm(string Term, string Instead, Regex Pattern, string FileFilter, bool FileFilterIsAscii, bool TermIsAscii);

    /// <summary>
    ///     A rejected synonym with its scope, reason, compound exemptions and matcher.
    /// </summary>
    /// <param name="Term">The one word the project uses for the concept.</param>
    /// <param name="Synonym">The rejected word.</param>
    /// <param name="Scope">The path scope, or null for every page.</param>
    /// <param name="Reason">Why the synonym is rejected, or null for no suffix.</param>
    /// <param name="Except">The compounds in which the word names a different concept.</param>
    /// <param name="Pattern">The whole-word matcher.</param>
    /// <param name="FileFilter">The synonym, or its first word when it holds a space: a file without it holds no match.</param>
    /// <param name="FileFilterIsAscii">Whether the file filter holds ASCII only, so an ordinal search prefilters it.</param>
    /// <param name="TermIsAscii">Whether the synonym holds ASCII only, so an ordinal search prefilters it.</param>
    internal sealed record RejectedSynonym(string Term, string Synonym, Regex? Scope, string? Reason, IReadOnlyList<Regex> Except, Regex Pattern, string FileFilter, bool FileFilterIsAscii, bool TermIsAscii);

    /// <summary>
    ///     Gets the mannered terms in record order.
    /// </summary>
    internal IReadOnlyList<ManneredTerm> ManneredTerms { get; }

    /// <summary>
    ///     Gets the rejected synonyms in record order.
    /// </summary>
    internal IReadOnlyList<RejectedSynonym> RejectedTerms { get; }

    /// <summary>
    ///     Gets a value indicating whether whether the gates below are sound for this glossary: every term carries a
    ///     word a gate can hold. Otherwise the scans run without them.
    /// </summary>
    internal bool CanGate { get; }

    /// <summary>
    ///     Gets matches raw text holding any single-word term or any multi-word term's
    ///     first word, or null when the glossary holds no word.
    /// </summary>
    internal Regex? FileGate { get; }

    /// <summary>
    ///     Gets matches text holding any whole term, or null when the glossary holds no word.
    /// </summary>
    internal Regex? LineGate { get; }

    /// <summary>
    ///     Gets matches text holding any multi-word term's first word, or null when no
    ///     term holds a space.
    /// </summary>
    internal Regex? FirstWordGate { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompiledLanguage"/> class.
    ///     Compiles the record from its entries and its gates.
    /// </summary>
    /// <param name="mannered">The mannered terms in record order.</param>
    /// <param name="rejected">The rejected synonyms in record order.</param>
    /// <param name="canGate">Whether the gates are sound for this glossary.</param>
    /// <param name="fileGate">The gate over raw file text, or null when the glossary holds no word.</param>
    /// <param name="lineGate">The gate over whole terms, or null when the glossary holds no word.</param>
    /// <param name="firstWordGate">The gate over multi-word terms' first words, or null when no term holds a space.</param>
    internal CompiledLanguage(
        IReadOnlyList<ManneredTerm> mannered,
        IReadOnlyList<RejectedSynonym> rejected,
        bool canGate,
        Regex? fileGate,
        Regex? lineGate,
        Regex? firstWordGate)
    {
        ManneredTerms = mannered;
        RejectedTerms = rejected;
        CanGate = canGate;
        FileGate = fileGate;
        LineGate = lineGate;
        FirstWordGate = firstWordGate;
    }
}
