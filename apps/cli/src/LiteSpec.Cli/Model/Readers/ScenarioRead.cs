namespace LiteSpec.Model.Readers;

/// <summary>What the scenario read found: the test read and the files it could not read.</summary>
/// <param name="Tests">The scenarios, elements and step facts the test roots declare.</param>
/// <param name="Problems">One <c>reader-missing</c> finding per file the reader could not read or parse.</param>
sealed record ScenarioRead(TestRead Tests, IReadOnlyList<Finding> Problems);
