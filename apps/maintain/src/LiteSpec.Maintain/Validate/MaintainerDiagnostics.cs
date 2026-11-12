namespace LiteSpec.Maintain.Validate;

/// <summary>
///     The stable diagnostic codes the maintainer emits, held once.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal static class MaintainerDiagnostics
{
    /// <summary>Gets every diagnostic code in declaration order.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Codes.AnchorBroken,
        Codes.CheckedByUnknown,
        Codes.ConventionDefaultSentence,
        Codes.ConventionMissingDefault,
        Codes.ConventionMissingReplacement,
        Codes.ConventionReplacementDeviation,
        Codes.ConventionReplacementSentence,
        Codes.ExtensionActivation,
        Codes.ExtensionBaseline,
        Codes.ExtensionDependencies,
        Codes.ExtensionKind,
        Codes.ExtensionReplacesEnforced,
        Codes.ExtensionReplacesUnknown,
        Codes.ExtensionLocation,
        Codes.ExtensionScope,
        Codes.ExtensionUnlisted,
        Codes.FieldNameShape,
        Codes.LayerUnreachable,
        Codes.ShapePage,
        Codes.StackFolder,
        Codes.StackReference,
        Codes.GlossaryDefinition,
        Codes.GlossaryOrder,
        Codes.HeadingAction,
        Codes.HeadingEmptyBody,
        Codes.IdDuplicate,
        Codes.IdInHeading,
        Codes.IdLineMissing,
        Codes.IdLineMismatch,
        Codes.IdPageFilename,
        Codes.IdUnknownReference,
        Codes.IndexContainsProcedure,
        Codes.IndexIntent,
        Codes.LinkBroken,
        Codes.ManifestAnalyzerPin,
        Codes.ManifestAnalyzerRule,
        Codes.ManifestAnchor,
        Codes.ManifestDuplicatePath,
        Codes.ManifestPath,
        Codes.ManifestYaml,
        Codes.MarkMissing,
        Codes.MarkUnresolved,
        Codes.PageEmptySection,
        Codes.PageEmptySectionText,
        Codes.PageHeadingCase,
        Codes.PageMissingSection,
        Codes.PageSectionOrder,
        Codes.PageTitleCase,
        Codes.PageTitleCount,
        Codes.PageUnknownSection,
        Codes.PatternInvalid,
        Codes.ProseNonAscii,
        Codes.ProseNormativeLocation,
        Codes.ProvisionsStale,
        Codes.ProvisionRestatesHeading,
        Codes.RecordDecisionUncited,
        Codes.RecordExplanationMissing,
        Codes.RecordExplanationOrphaned,
        Codes.RecordIdMismatch,
        Codes.RecordSchema,
        Codes.RecordSource,
        Codes.RecordUnknownReference,
        Codes.ReferenceExampleDeclaration,
        Codes.SchemaId,
        Codes.SchemaInvalid,
        Codes.SchemaUnsupportedKeyword,
        Codes.SchemaYaml,
        Codes.StandardDeviationNormative,
        Codes.StandardDeviationSentence,
        Codes.StandardExampleLabel,
        Codes.StandardInformativeNormative,
        Codes.StandardLabelDuplicate,
        Codes.StandardLabelInvalid,
        Codes.StandardLabelOrder,
        Codes.StandardMissingDeviation,
        Codes.StandardMissingRequirement,
        Codes.StandardModalCount,
        Codes.StandardModalVocabulary,
        Codes.StandardRequirementCount,
        Codes.StandardSentenceCount,
        Codes.StandardUnexpectedDeviation,
        Codes.StandardUnlabeledContent,
        Codes.VerifyCommand,
        Codes.VerifyDuplicateEvidence,
        Codes.VerifyDuplicateId,
        Codes.VerifyGenericEvidence,
        Codes.VerifyHeader,
        Codes.VerifyMissingId,
        Codes.VerifyOrder,
        Codes.VerifyTemplatedEvidence,
        Codes.ValueStyle,
        Codes.TemplateIndexPath,
        Codes.TemplateSchema,
        Codes.IndexMissingRouting,
        Codes.LayerUnlisted,
        Codes.GeneratedStale,
        Codes.ScopeArtifact,
        Codes.ScopeBlock,
        Codes.ScopeNarrow,
        Codes.ScopeValue,
        Codes.StrengthLine,
        Codes.CheckedByFindingMissing,
        Codes.CommandOptionUnknown,
        Codes.ExampleRecordUnknown,
        Codes.FindingCodeUnknown,
        Codes.IndexPageMissing,
        Codes.PatternOptionCount,
        Codes.TemplateCitedUnknown,
        Codes.TemplateCitationUnknown,
        Codes.StandardStrengthModal,
    ];

    /// <summary>The diagnostic code constants.</summary>
    internal static class Codes
    {
        /// <summary>A broken anchor.</summary>
        public const string AnchorBroken = "ANCHOR_BROKEN";

        /// <summary>An unknown Checked by reference.</summary>
        public const string CheckedByUnknown = "CHECKED_BY_UNKNOWN";

        /// <summary>A convention Default with more than one sentence.</summary>
        public const string ConventionDefaultSentence = "CONVENTION_DEFAULT_SENTENCE";

        /// <summary>A convention missing a Default.</summary>
        public const string ConventionMissingDefault = "CONVENTION_MISSING_DEFAULT";

        /// <summary>A convention missing a Replacement.</summary>
        public const string ConventionMissingReplacement = "CONVENTION_MISSING_REPLACEMENT";

        /// <summary>A convention Replacement that names a deviation.</summary>
        public const string ConventionReplacementDeviation = "CONVENTION_REPLACEMENT_DEVIATION";

        /// <summary>A convention Replacement with more than one sentence.</summary>
        public const string ConventionReplacementSentence = "CONVENTION_REPLACEMENT_SENTENCE";

        /// <summary>An extension with no activation criterion.</summary>
        public const string ExtensionActivation = "EXTENSION_ACTIVATION";

        /// <summary>An extension with an empty Baseline Relationship.</summary>
        public const string ExtensionBaseline = "EXTENSION_BASELINE";

        /// <summary>An extension with an empty Dependencies section.</summary>
        public const string ExtensionDependencies = "EXTENSION_DEPENDENCIES";

        /// <summary>An extension whose kinds do not match the manifest.</summary>
        public const string ExtensionKind = "EXTENSION_KIND";

        /// <summary>An extension that still states a replaced provision.</summary>
        public const string ExtensionReplacesEnforced = "EXTENSION_REPLACES_ENFORCED";

        /// <summary>An extension that replaces an unknown provision.</summary>
        public const string ExtensionReplacesUnknown = "EXTENSION_REPLACES_UNKNOWN";

        /// <summary>An extension path outside its scope folder.</summary>
        public const string ExtensionLocation = "EXTENSION_LOCATION";

        /// <summary>An extension that does not state its activation scope.</summary>
        public const string ExtensionScope = "EXTENSION_SCOPE";

        /// <summary>An extension page not listed in the manifest.</summary>
        public const string ExtensionUnlisted = "EXTENSION_UNLISTED";

        /// <summary>A field name whose shape does not match its word class.</summary>
        public const string FieldNameShape = "FIELD_NAME_SHAPE";

        /// <summary>A layer page reached by no stack selection.</summary>
        public const string LayerUnreachable = "LAYER_UNREACHABLE";

        /// <summary>A shape backend page outside docs/backend.</summary>
        public const string ShapePage = "SHAPE_PAGE";

        /// <summary>A stack folder that holds no stack.md.</summary>
        public const string StackFolder = "STACK_FOLDER";

        /// <summary>A stack reference to an unknown or invalid stack.</summary>
        public const string StackReference = "STACK_REFERENCE";

        /// <summary>A glossary definition problem.</summary>
        public const string GlossaryDefinition = "GLOSSARY_DEFINITION";

        /// <summary>A glossary order problem.</summary>
        public const string GlossaryOrder = "GLOSSARY_ORDER";

        /// <summary>A heading that does not start with an action.</summary>
        public const string HeadingAction = "HEADING_ACTION";

        /// <summary>A heading with an empty body.</summary>
        public const string HeadingEmptyBody = "HEADING_EMPTY_BODY";

        /// <summary>A duplicate provision identifier.</summary>
        public const string IdDuplicate = "ID_DUPLICATE";

        /// <summary>An identifier written in a heading.</summary>
        public const string IdInHeading = "ID_IN_HEADING";

        /// <summary>A provision with no ID line.</summary>
        public const string IdLineMissing = "ID_LINE_MISSING";

        /// <summary>A provision ID line that does not match.</summary>
        public const string IdLineMismatch = "ID_LINE_MISMATCH";

        /// <summary>A page filename that collides with a virtual template path.</summary>
        public const string IdPageFilename = "ID_PAGE_FILENAME";

        /// <summary>A citation of an unknown provision identifier.</summary>
        public const string IdUnknownReference = "ID_UNKNOWN_REFERENCE";

        /// <summary>An index that contains a procedure.</summary>
        public const string IndexContainsProcedure = "INDEX_CONTAINS_PROCEDURE";

        /// <summary>An index intent problem.</summary>
        public const string IndexIntent = "INDEX_INTENT";

        /// <summary>A broken link.</summary>
        public const string LinkBroken = "LINK_BROKEN";

        /// <summary>An analyzer pin that differs from the project version.</summary>
        public const string ManifestAnalyzerPin = "MANIFEST_ANALYZER_PIN";

        /// <summary>An analyzer rule cited but not shipped by the pinned release.</summary>
        public const string ManifestAnalyzerRule = "MANIFEST_ANALYZER_RULE";

        /// <summary>A manifest anchor that does not exist.</summary>
        public const string ManifestAnchor = "MANIFEST_ANCHOR";

        /// <summary>A duplicate path in the manifest.</summary>
        public const string ManifestDuplicatePath = "MANIFEST_DUPLICATE_PATH";

        /// <summary>A manifest that cannot be parsed as YAML.</summary>
        public const string ManifestYaml = "MANIFEST_YAML";

        /// <summary>A manifest path that does not exist.</summary>
        public const string ManifestPath = "MANIFEST_PATH";

        /// <summary>A missing mark.</summary>
        public const string MarkMissing = "MARK_MISSING";

        /// <summary>A mark that does not resolve.</summary>
        public const string MarkUnresolved = "MARK_UNRESOLVED";

        /// <summary>An empty page section.</summary>
        public const string PageEmptySection = "PAGE_EMPTY_SECTION";

        /// <summary>A page section with empty text.</summary>
        public const string PageEmptySectionText = "PAGE_EMPTY_SECTION_TEXT";

        /// <summary>A page heading not in Title Case.</summary>
        public const string PageHeadingCase = "PAGE_HEADING_CASE";

        /// <summary>A page missing a required section.</summary>
        public const string PageMissingSection = "PAGE_MISSING_SECTION";

        /// <summary>A page section out of order.</summary>
        public const string PageSectionOrder = "PAGE_SECTION_ORDER";

        /// <summary>A page title not in Title Case.</summary>
        public const string PageTitleCase = "PAGE_TITLE_CASE";

        /// <summary>A page with the wrong number of titles.</summary>
        public const string PageTitleCount = "PAGE_TITLE_COUNT";

        /// <summary>A page with an unknown section.</summary>
        public const string PageUnknownSection = "PAGE_UNKNOWN_SECTION";

        /// <summary>An invalid pattern page.</summary>
        public const string PatternInvalid = "PATTERN_INVALID";

        /// <summary>A non-ASCII character in prose.</summary>
        public const string ProseNonAscii = "PROSE_NON_ASCII";

        /// <summary>Normative vocabulary outside a provision assertion.</summary>
        public const string ProseNormativeLocation = "PROSE_NORMATIVE_LOCATION";

        /// <summary>A generated provision artifact that differs from the active standards.</summary>
        public const string ProvisionsStale = "PROVISIONS_STALE";

        /// <summary>A provision assertion that only restates its heading.</summary>
        public const string ProvisionRestatesHeading = "PROVISION_RESTATES_HEADING";

        /// <summary>A decision record that cites no provision.</summary>
        public const string RecordDecisionUncited = "RECORD_DECISION_UNCITED";

        /// <summary>A record explanation file that is missing.</summary>
        public const string RecordExplanationMissing = "RECORD_EXPLANATION_MISSING";

        /// <summary>An orphaned record explanation file.</summary>
        public const string RecordExplanationOrphaned = "RECORD_EXPLANATION_ORPHANED";

        /// <summary>A record whose id does not match its file.</summary>
        public const string RecordIdMismatch = "RECORD_ID_MISMATCH";

        /// <summary>A record that does not satisfy its schema.</summary>
        public const string RecordSchema = "RECORD_SCHEMA";

        /// <summary>A record source problem.</summary>
        public const string RecordSource = "RECORD_SOURCE";

        /// <summary>A record that references an unknown id.</summary>
        public const string RecordUnknownReference = "RECORD_UNKNOWN_REFERENCE";

        /// <summary>A reference example declaration problem.</summary>
        public const string ReferenceExampleDeclaration = "REFERENCE_EXAMPLE_DECLARATION";

        /// <summary>A schema $id that does not match its published location.</summary>
        public const string SchemaId = "SCHEMA_ID";

        /// <summary>A value that does not satisfy its schema.</summary>
        public const string SchemaInvalid = "SCHEMA_INVALID";

        /// <summary>A schema that cannot be parsed as YAML.</summary>
        public const string SchemaYaml = "SCHEMA_YAML";

        /// <summary>A JSON Schema keyword the evaluator does not implement.</summary>
        public const string SchemaUnsupportedKeyword = "SCHEMA_UNSUPPORTED_KEYWORD";

        /// <summary>A Deviation that contains normative vocabulary.</summary>
        public const string StandardDeviationNormative = "STANDARD_DEVIATION_NORMATIVE";

        /// <summary>A Deviation with more than one sentence.</summary>
        public const string StandardDeviationSentence = "STANDARD_DEVIATION_SENTENCE";

        /// <summary>A fenced example not under an Example label.</summary>
        public const string StandardExampleLabel = "STANDARD_EXAMPLE_LABEL";

        /// <summary>Informative content that contains normative vocabulary.</summary>
        public const string StandardInformativeNormative = "STANDARD_INFORMATIVE_NORMATIVE";

        /// <summary>A duplicate provision label.</summary>
        public const string StandardLabelDuplicate = "STANDARD_LABEL_DUPLICATE";

        /// <summary>An invalid provision label.</summary>
        public const string StandardLabelInvalid = "STANDARD_LABEL_INVALID";

        /// <summary>A provision label out of order.</summary>
        public const string StandardLabelOrder = "STANDARD_LABEL_ORDER";

        /// <summary>A recommendation with no Deviation statement.</summary>
        public const string StandardMissingDeviation = "STANDARD_MISSING_DEVIATION";

        /// <summary>A Standard with no Statement.</summary>
        public const string StandardMissingRequirement = "STANDARD_MISSING_REQUIREMENT";

        /// <summary>A Statement with the wrong number of normative modals.</summary>
        public const string StandardModalCount = "STANDARD_MODAL_COUNT";

        /// <summary>A Statement that uses an unsupported normative term.</summary>
        public const string StandardModalVocabulary = "STANDARD_MODAL_VOCABULARY";

        /// <summary>A Standard with more than one Statement.</summary>
        public const string StandardRequirementCount = "STANDARD_REQUIREMENT_COUNT";

        /// <summary>A Statement with more than one sentence.</summary>
        public const string StandardSentenceCount = "STANDARD_SENTENCE_COUNT";

        /// <summary>A non-recommendation with a Deviation statement.</summary>
        public const string StandardUnexpectedDeviation = "STANDARD_UNEXPECTED_DEVIATION";

        /// <summary>Content outside a Rationale or Example label.</summary>
        public const string StandardUnlabeledContent = "STANDARD_UNLABELED_CONTENT";

        /// <summary>A checkable verification line that names no command.</summary>
        public const string VerifyCommand = "VERIFY_COMMAND";

        /// <summary>Verification evidence repeated by another provision on the page.</summary>
        public const string VerifyDuplicateEvidence = "VERIFY_DUPLICATE_EVIDENCE";

        /// <summary>A provision with more than one verification line.</summary>
        public const string VerifyDuplicateId = "VERIFY_DUPLICATE_ID";

        /// <summary>Verification that uses generic evidence.</summary>
        public const string VerifyGenericEvidence = "VERIFY_GENERIC_EVIDENCE";

        /// <summary>A leftover Verification table.</summary>
        public const string VerifyHeader = "VERIFY_HEADER";

        /// <summary>A provision with no verification line.</summary>
        public const string VerifyMissingId = "VERIFY_MISSING_ID";

        /// <summary>A verification line not last among its fields.</summary>
        public const string VerifyOrder = "VERIFY_ORDER";

        /// <summary>Verification that repeats its provision heading.</summary>
        public const string VerifyTemplatedEvidence = "VERIFY_TEMPLATED_EVIDENCE";

        /// <summary>A value style problem.</summary>
        public const string ValueStyle = "VALUE_STYLE";

        /// <summary>A template index path that does not resolve.</summary>
        public const string TemplateIndexPath = "TEMPLATE_INDEX_PATH";

        /// <summary>A template that does not satisfy its schema.</summary>
        public const string TemplateSchema = "TEMPLATE_SCHEMA";

        /// <summary>An index missing a routing block.</summary>
        public const string IndexMissingRouting = "INDEX_MISSING_ROUTING";

        /// <summary>A page in none of the known docs areas.</summary>
        public const string LayerUnlisted = "LAYER_UNLISTED";

        /// <summary>A generated count that differs from the active standards.</summary>
        public const string GeneratedStale = "GENERATED_STALE";

        /// <summary>A provision on a mixed-artifact page that names no artifact.</summary>
        public const string ScopeArtifact = "SCOPE_ARTIFACT";

        /// <summary>An out-of-order scope block.</summary>
        public const string ScopeBlock = "SCOPE_BLOCK";

        /// <summary>An Applies to line that does not narrow its page scope.</summary>
        public const string ScopeNarrow = "SCOPE_NARROW";

        /// <summary>An Applies to line with an unknown or invalid value.</summary>
        public const string ScopeValue = "SCOPE_VALUE";

        /// <summary>A missing or invalid Strength line.</summary>
        public const string StrengthLine = "STRENGTH_LINE";

        /// <summary>A Checked by line for the model check that names no finding code.</summary>
        public const string CheckedByFindingMissing = "CHECKED_BY_FINDING_MISSING";

        /// <summary>A command page that shows an option its command does not parse, or names a LiteSpec command the source does not declare.</summary>
        public const string CommandOptionUnknown = "COMMAND_OPTION_UNKNOWN";

        /// <summary>A docs page that cites an example record ID the example lacks.</summary>
        public const string ExampleRecordUnknown = "EXAMPLE_RECORD_UNKNOWN";

        /// <summary>A docs page that cites a finding code the finding code table lacks.</summary>
        public const string FindingCodeUnknown = "FINDING_CODE_UNKNOWN";

        /// <summary>A docs page that the index page does not link, directly or through a section index.</summary>
        public const string IndexPageMissing = "INDEX_PAGE_MISSING";

        /// <summary>A pattern that offers more than three options.</summary>
        public const string PatternOptionCount = "PATTERN_OPTION_COUNT";

        /// <summary>A docs page that cites a template path the templates folder lacks.</summary>
        public const string TemplateCitedUnknown = "TEMPLATE_CITED_UNKNOWN";

        /// <summary>A template that cites a provision the index lacks.</summary>
        public const string TemplateCitationUnknown = "TEMPLATE_CITATION_UNKNOWN";

        /// <summary>A provision whose Strength line disagrees with its Statement or Default.</summary>
        public const string StandardStrengthModal = "STANDARD_STRENGTH_MODAL";
    }
}
