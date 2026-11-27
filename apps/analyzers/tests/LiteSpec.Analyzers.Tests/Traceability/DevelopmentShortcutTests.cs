using LiteSpec.Analyzers.Rules.Traceability;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Traceability;

/// <summary>Tests for LS5014, the development shortcut compiled into a build without DEBUG.</summary>
public sealed class DevelopmentShortcutTests
{
    /// <summary>The Infrastructure file of the shortcut project, which every test below compiles.</summary>
    private const string ShortcutPath = "Claimwick.DevelopmentSignIn/Infrastructure/DevelopmentSignIn.cs";

    /// <summary>The assembly the shortcut project compiles under.</summary>
    private const string ShortcutAssembly = "Claimwick.DevelopmentSignIn";

    /// <summary>LS5014 reports a tagged class compiled into a build without DEBUG.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/quality/development#keep-development-shortcuts-out-of-release-builds</proves>
    [Fact(DisplayName = "LS5014 reports a tagged class compiled into a build without DEBUG")]
    public Task ReportsATaggedClassWithoutDebug() =>
        Verify.AnalyzerAsync<DevelopmentShortcutAnalyzer>(
            [(ShortcutPath, """
                namespace Claimwick.DevelopmentSignIn;

                /// <development-shortcut>Signs in as a cast person without the identity provider.</development-shortcut>
                internal sealed class {|LS5014:DevelopmentSignIn|}
                {
                }
                """)],
            assemblyName: ShortcutAssembly);

    /// <summary>LS5014 reports a tagged struct, record and interface compiled into a build without DEBUG.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/quality/development#keep-development-shortcuts-out-of-release-builds</proves>
    [Fact(DisplayName = "LS5014 reports a tagged struct, record and interface without DEBUG")]
    public Task ReportsEveryDeclarationKindWithoutDebug() =>
        Verify.AnalyzerAsync<DevelopmentShortcutAnalyzer>(
            [(ShortcutPath, """
                namespace Claimwick.DevelopmentSignIn;

                /// <development-shortcut>Holds a fixed sign-in clock.</development-shortcut>
                internal struct {|LS5014:FixedClock|}
                {
                }

                /// <development-shortcut>Carries a fake sign-in result.</development-shortcut>
                internal sealed record {|LS5014:FakeSignInResult|}(string PersonId);

                /// <development-shortcut>Stands in for the identity provider.</development-shortcut>
                internal interface {|LS5014:IFakeIdentityProvider|}
                {
                }
                """)],
            assemblyName: ShortcutAssembly);

    /// <summary>LS5014 reports a tagged type even when the project generates no documentation file.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/quality/development#keep-development-shortcuts-out-of-release-builds</proves>
    [Fact(DisplayName = "LS5014 reports a tagged type without a documentation file")]
    public Task ReportsATaggedTypeWithoutADocumentationFile() =>
        Verify.AnalyzerAsync<DevelopmentShortcutAnalyzer>(
            [(ShortcutPath, """
                namespace Claimwick.DevelopmentSignIn;

                /// <development-shortcut>Signs in as a cast person without the identity provider.</development-shortcut>
                internal sealed class {|LS5014:DevelopmentSignIn|}
                {
                }
                """)],
            assemblyName: ShortcutAssembly,
            documentationFile: false);

    /// <summary>LS5014 accepts a tagged type compiled into a build that defines DEBUG.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/quality/development#keep-development-shortcuts-out-of-release-builds</proves>
    [Fact(DisplayName = "LS5014 accepts a tagged type compiled into a build with DEBUG")]
    public Task AcceptsATaggedTypeWithDebug() =>
        Verify.AnalyzerAsync<DevelopmentShortcutAnalyzer>(
            [(ShortcutPath, """
                namespace Claimwick.DevelopmentSignIn;

                /// <development-shortcut>Signs in as a cast person without the identity provider.</development-shortcut>
                internal sealed class DevelopmentSignIn
                {
                }
                """)],
            assemblyName: ShortcutAssembly,
            preprocessorSymbols: ["DEBUG"]);

    /// <summary>LS5014 accepts a type without the tag, with or without DEBUG.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/quality/development#keep-development-shortcuts-out-of-release-builds</proves>
    [Fact(DisplayName = "LS5014 accepts a type without the development-shortcut tag")]
    public Task AcceptsATypeWithoutTheTag() =>
        Verify.AnalyzerAsync<DevelopmentShortcutAnalyzer>(
            [(ShortcutPath, """
                namespace Claimwick.DevelopmentSignIn;

                /// <summary>Signs in a person through the identity provider.</summary>
                internal sealed class SignInService
                {
                }
                """)],
            assemblyName: ShortcutAssembly);

    /// <summary>LS5014 reports a tag with an empty reason in a build that defines DEBUG.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/quality/development#keep-development-shortcuts-out-of-release-builds</proves>
    [Fact(DisplayName = "LS5014 reports an empty reason in a build with DEBUG")]
    public Task ReportsAnEmptyReasonWithDebug() =>
        Verify.AnalyzerAsync<DevelopmentShortcutAnalyzer>(
            [(ShortcutPath, """
                namespace Claimwick.DevelopmentSignIn;

                /// <development-shortcut>   </development-shortcut>
                internal sealed class {|LS5014:DevelopmentSignIn|}
                {
                }
                """)],
            assemblyName: ShortcutAssembly,
            preprocessorSymbols: ["DEBUG"]);

    /// <summary>LS5014 reports one diagnostic for a type that lacks both DEBUG and a reason.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/quality/development#keep-development-shortcuts-out-of-release-builds</proves>
    [Fact(DisplayName = "LS5014 reports one diagnostic when DEBUG and the reason both fail")]
    public Task ReportsOneDiagnosticWhenDebugAndReasonFail() =>
        Verify.AnalyzerAsync<DevelopmentShortcutAnalyzer>(
            [(ShortcutPath, """
                namespace Claimwick.DevelopmentSignIn;

                /// <development-shortcut></development-shortcut>
                internal sealed class {|LS5014:DevelopmentSignIn|}
                {
                }
                """)],
            assemblyName: ShortcutAssembly);

    /// <summary>LS5014 reads a reason wrapped over several documentation lines.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/quality/development#keep-development-shortcuts-out-of-release-builds</proves>
    [Fact(DisplayName = "LS5014 reads a reason wrapped over several lines")]
    public Task AcceptsAReasonWrappedOverSeveralLines() =>
        Verify.AnalyzerAsync<DevelopmentShortcutAnalyzer>(
            [(ShortcutPath, """
                namespace Claimwick.DevelopmentSignIn;

                /// <development-shortcut>
                /// Signs in as a cast person without the identity provider,
                /// for local runs only.
                /// </development-shortcut>
                internal sealed class DevelopmentSignIn
                {
                }
                """)],
            assemblyName: ShortcutAssembly,
            preprocessorSymbols: ["DEBUG"]);
}
