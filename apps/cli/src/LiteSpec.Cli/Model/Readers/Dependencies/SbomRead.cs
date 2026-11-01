namespace LiteSpec.Model.Readers.Dependencies;

/// <summary>What the bill of materials read found: direct packages and the configured paths it could not read.</summary>
/// <param name="Packages">Every direct package of every readable bill of materials.</param>
/// <param name="Problems">One <c>reader-missing</c> finding per configured path that could not be read or parsed, is not JSON, is neither SPDX nor CycloneDX, or yields no packages.</param>
sealed record SbomRead(IReadOnlyList<DirectPackage> Packages, IReadOnlyList<Finding> Problems);
