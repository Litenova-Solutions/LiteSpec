namespace LiteSpec.Tests.Authoring;

/// <summary>Self-mark cases: MARK_MISSING and MARK_UNRESOLVED over fixture projects.</summary>
public sealed partial class StandardsValidatorTests
{
    static readonly string UnmarkedClass = """
        namespace X;

        public sealed class Widget
        {
            public void Run()
            {
            }
        }
        """ + "\n";

    static readonly string MarkedClass = """
        namespace X;

        /// <follows>litespec/core/topic#keep-the-topic-bounded</follows>
        public sealed class Widget
        {
            public void Run()
            {
            }
        }
        """ + "\n";

    static readonly string PropertyBag = """
        namespace X;

        public sealed class Settings
        {
            public string Name { get; set; } = "";
        }
        """ + "\n";

    static readonly string NestedClass = """
        namespace X;

        public sealed class Outer
        {
            public sealed class Inner
            {
                public void Run()
                {
                }
            }
        }
        """ + "\n";

    static readonly string UnknownProof = """
        namespace X.Tests;

        public sealed class WidgetTests
        {
            /// <proves>litespec/core/topic#no-such-provision</proves>
            [Fact]
            public void WidgetRuns()
            {
            }
        }
        """ + "\n";

    static readonly string KnownProof = """
        namespace X.Tests;

        public sealed class WidgetTests
        {
            /// <proves>litespec/core/topic#keep-the-topic-bounded</proves>
            [Fact]
            public void WidgetRuns()
            {
            }
        }
        """ + "\n";

    static readonly string PartialFirst = """
        namespace X;

        public sealed partial class Widget
        {
            public void Run()
            {
            }
        }
        """ + "\n";

    static readonly string PartialSecond = """
        namespace X;

        /// <follows>litespec/core/topic#keep-the-topic-bounded</follows>
        public sealed partial class Widget
        {
            public void Stop()
            {
            }
        }
        """ + "\n";

    /// <proves>litespec/core/maintaining#mark-the-tools-with-the-provisions-they-follow</proves>
    [Fact(DisplayName = "unmarked class with a method reports MARK_MISSING")]
    public void UnmarkedClassWithMethodReportsMarkMissing() => Check(
        root => StandardsFixture.WriteSource(root, "apps/x/src/X", "Widget.cs", UnmarkedClass),
        ["MARK_MISSING"]);

    /// <proves>litespec/core/maintaining#mark-the-tools-with-the-provisions-they-follow</proves>
    [Fact(DisplayName = "marked class with a method reports nothing")]
    public void MarkedClassWithMethodReportsNothing() => Check(
        root => StandardsFixture.WriteSource(root, "apps/x/src/X", "Widget.cs", MarkedClass),
        []);

    /// <proves>litespec/core/maintaining#mark-the-tools-with-the-provisions-they-follow</proves>
    [Fact(DisplayName = "class with only properties reports nothing")]
    public void ClassWithOnlyPropertiesReportsNothing() => Check(
        root => StandardsFixture.WriteSource(root, "apps/x/src/X", "Settings.cs", PropertyBag),
        []);

    /// <proves>litespec/core/maintaining#mark-the-tools-with-the-provisions-they-follow</proves>
    [Fact(DisplayName = "nested class reports nothing")]
    public void NestedClassReportsNothing() => Check(
        root => StandardsFixture.WriteSource(root, "apps/x/src/X", "Outer.cs", NestedClass),
        []);

    /// <proves>litespec/core/maintaining#mark-the-tools-with-the-provisions-they-follow</proves>
    [Fact(DisplayName = "class under a Fixtures folder reports nothing")]
    public void FixtureFolderClassReportsNothing() => Check(
        root => StandardsFixture.WriteSource(root, "apps/x/src/X", "Fixtures/Widget.cs", UnmarkedClass),
        []);

    /// <proves>litespec/core/maintaining#mark-the-tools-with-the-provisions-they-follow</proves>
    [Fact(DisplayName = "unknown proves mark reports MARK_UNRESOLVED")]
    public void UnknownProvesMarkReportsMarkUnresolved() => Check(
        root => StandardsFixture.WriteSource(root, "apps/x/tests/X.Tests", "WidgetTests.cs", UnknownProof),
        ["MARK_UNRESOLVED"]);

    /// <proves>litespec/core/maintaining#mark-the-tools-with-the-provisions-they-follow</proves>
    [Fact(DisplayName = "known proves mark reports nothing")]
    public void KnownProvesMarkReportsNothing() => Check(
        root => StandardsFixture.WriteSource(root, "apps/x/tests/X.Tests", "WidgetTests.cs", KnownProof),
        []);

    /// <proves>litespec/core/maintaining#mark-the-tools-with-the-provisions-they-follow</proves>
    [Fact(DisplayName = "partial class marked on one part reports nothing")]
    public void PartialClassMarkedOnOnePartReportsNothing() => Check(
        root =>
        {
            StandardsFixture.WriteSource(root, "apps/x/src/X", "First.cs", PartialFirst);
            StandardsFixture.WriteSource(root, "apps/x/src/X", "Second.cs", PartialSecond);
        },
        []);
}
