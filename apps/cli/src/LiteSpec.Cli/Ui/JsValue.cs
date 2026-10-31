using System.Text.Json.Nodes;

namespace LiteSpec.Ui;

/// <summary>
///     A JSON property read with its presence: whether the key exists,
///     and the value when it does. A missing key and an explicit JSON null both
///     hold a null value; only <see cref="Found"/> tells them apart.
/// </summary>
/// <param name="Found">Whether the key exists.</param>
/// <param name="Node">The value the key holds, or null.</param>
readonly record struct JsValue(bool Found, JsonNode? Node);
