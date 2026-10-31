namespace LiteSpec.Model;

/// <summary>
///     What the record read found: records, bodies, provisions and problems.
/// </summary>
/// <param name="Records">Every written record.</param>
/// <param name="Bodies">Every page body under the record roots.</param>
/// <param name="Provisions">The shared provision ids, from the release the tool carries.</param>
/// <param name="Problems">Files the reader could not read.</param>
sealed record RecordRead(
    IReadOnlyList<Record> Records,
    IReadOnlyList<PageBody> Bodies,
    IReadOnlySet<string> Provisions,
    IReadOnlyList<Finding> Problems);
