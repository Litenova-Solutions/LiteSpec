using System.Text.Json.Nodes;
using static LiteSpec.Schemas.JsonSchemaSupport;

namespace LiteSpec.Schemas;

/// <summary>
///     Schema composition: <c>allOf</c>, <c>anyOf</c>, <c>oneOf</c>, <c>if</c>,
///     <c>then</c>, <c>else</c>, and <c>not</c>.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal static class SchemaComposition
{
    /// <summary>Validates a value through every composition keyword of a schema.</summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="schema">The schema object.</param>
    /// <param name="root">The schema root.</param>
    /// <param name="location">The value location.</param>
    /// <param name="errors">The errors to append to.</param>
    internal static void ValidateComposition(JsonNode? value, JsonObject schema, JsonNode root, string location, List<string> errors)
    {
        if (schema.TryGetPropertyValue("allOf", out var allOf) && allOf is JsonArray branches)
        {
            foreach (var branch in branches)
            {
                JsonSchema.Validate(value, branch, root, location, errors);
            }
        }

        // Exactly one branch accepts the value. The count alone tells an author that
        // the value fits no shape without telling them why any shape refused it, so
        // the message carries the first reason from each branch that rejected.
        if (schema.TryGetPropertyValue("oneOf", out var oneOfNode) && oneOfNode is JsonArray oneOf)
        {
            var dispatch = DispatchIndex(value, oneOf, root);

            if (dispatch >= 0)
            {
                var fastErrors = new List<string>();
                JsonSchema.Validate(value, oneOf[dispatch], root, location, fastErrors);

                if (fastErrors.Count == 0)
                {
                    // Every other branch pins a different type const, so each one
                    // rejects a value whose type matches this branch. The value
                    // matches exactly one shape and reports nothing.
                }
                else
                {
                    // The matching shape failed, so the value matches none. The
                    // loop below reproduces the exact message, reusing the
                    // dispatched branch's errors instead of validating it twice.
                    var rejections = new List<string>();
                    var matched = 0;

                    for (var index = 0; index < oneOf.Count; index++)
                    {
                        List<string> branchErrors;

                        if (index == dispatch)
                        {
                            branchErrors = fastErrors;
                        }
                        else
                        {
                            branchErrors = new List<string>();
                            JsonSchema.Validate(value, oneOf[index], root, location, branchErrors);
                        }

                        if (branchErrors.Count == 0)
                        {
                            matched++;
                        }
                        else
                        {
                            rejections.Add($"shape {index + 1}: {StripLocationPrefix(branchErrors[0], location)}");
                        }
                    }

                    if (matched != 1)
                    {
                        var because = matched == 0 && rejections.Count > 0 ? $" ({string.Join("; ", rejections)})" : "";
                        errors.Add($"{location}: value matches {matched} of {oneOf.Count} allowed shapes, expected exactly 1{because}");
                    }
                }
            }
            else
            {
                var rejections = new List<string>();
                var matched = 0;

                for (var index = 0; index < oneOf.Count; index++)
                {
                    var branchErrors = new List<string>();
                    JsonSchema.Validate(value, oneOf[index], root, location, branchErrors);

                    if (branchErrors.Count == 0)
                    {
                        matched++;
                    }
                    else
                    {
                        rejections.Add($"shape {index + 1}: {StripLocationPrefix(branchErrors[0], location)}");
                    }
                }

                if (matched != 1)
                {
                    var because = matched == 0 && rejections.Count > 0 ? $" ({string.Join("; ", rejections)})" : "";
                    errors.Add($"{location}: value matches {matched} of {oneOf.Count} allowed shapes, expected exactly 1{because}");
                }
            }
        }

        if (schema.TryGetPropertyValue("anyOf", out var anyOfNode) && anyOfNode is JsonArray anyOf)
        {
            var rejections = new List<string>();
            var accepted = false;

            for (var index = 0; index < anyOf.Count && !accepted; index++)
            {
                var branchErrors = new List<string>();
                JsonSchema.Validate(value, anyOf[index], root, location, branchErrors);

                if (branchErrors.Count == 0)
                {
                    accepted = true;
                }
                else
                {
                    rejections.Add($"shape {index + 1}: {StripLocationPrefix(branchErrors[0], location)}");
                }
            }

            if (!accepted)
            {
                errors.Add($"{location}: value matches none of the {anyOf.Count} allowed shapes ({string.Join("; ", rejections)})");
            }
        }

        if (schema.TryGetPropertyValue("if", out var condition) && IsTruthy(condition))
        {
            var conditionErrors = new List<string>();
            JsonSchema.Validate(value, condition, root, location, conditionErrors);
            schema.TryGetPropertyValue("then", out var thenBranch);
            schema.TryGetPropertyValue("else", out var elseBranch);
            JsonSchema.Validate(value, conditionErrors.Count == 0 ? thenBranch : elseBranch, root, location, errors);
        }

        if (schema.TryGetPropertyValue("not", out var forbidden) && IsTruthy(forbidden))
        {
            var notErrors = new List<string>();
            JsonSchema.Validate(value, forbidden, root, location, notErrors);

            if (notErrors.Count == 0)
            {
                errors.Add($"{location}: value matches prohibited schema");
            }
        }
    }

    /// <summary>
    ///     The one branch of a <c>oneOf</c> whose pinned type const equals the value's
    ///     type, or -1 when the shape does not dispatch. A dispatch needs an object
    ///     value with a string type, every branch pinning a type const through its
    ///     properties, and exactly one branch pinning this value's type. Anything
    ///     else falls back to validating every branch, so every message reads as before.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="oneOf">The oneOf branches.</param>
    /// <param name="root">The schema root.</param>
    /// <returns>The dispatch branch index, or -1.</returns>
    private static int DispatchIndex(JsonNode? value, JsonArray oneOf, JsonNode root)
    {
        if (value is not JsonObject fields)
        {
            return -1;
        }

        if (!fields.TryGetPropertyValue("type", out var typeNode))
        {
            return -1;
        }

        if (typeNode is not JsonValue typeValue)
        {
            return -1;
        }

        if (!typeValue.TryGetValue<string>(out var typeName) || typeName is null)
        {
            return -1;
        }

        var match = -1;

        for (var index = 0; index < oneOf.Count; index++)
        {
            var pinned = BranchTypeConst(oneOf[index], root);

            if (pinned is null)
            {
                return -1;
            }

            if (pinned == typeName)
            {
                if (match >= 0)
                {
                    return -1;
                }

                match = index;
            }
        }

        return match;
    }

    /// <summary>
    ///     The type const a <c>oneOf</c> branch pins, following local references to
    ///     the shape that carries <c>properties.type.const</c>, or null when the
    ///     branch pins none. A branch that cannot be read this way disables the
    ///     dispatch for its whole <c>oneOf</c>.
    /// </summary>
    /// <param name="branch">The oneOf branch.</param>
    /// <param name="root">The schema root.</param>
    /// <returns>The pinned type const, or null.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "BranchTypeConst reads an optional dispatch hint and returns null on any unreadable reference, so the caller falls back to validating every branch.")]
    private static string? BranchTypeConst(JsonNode? branch, JsonNode root)
    {
        var current = branch;

        for (var depth = 0; depth < 10; depth++)
        {
            if (current is not JsonObject currentObject)
            {
                return null;
            }

            if (currentObject.TryGetPropertyValue("$ref", out var refNode)
                && refNode is JsonValue refValue
                && refValue.TryGetValue<string>(out var reference)
                && !string.IsNullOrEmpty(reference))
            {
                JsonNode? target;

                try
                {
                    target = SchemaReferences.CachedRef(root, reference);
                }
                catch
                {
                    return null;
                }

                if (target is null)
                {
                    return null;
                }

                current = target;
                continue;
            }

            break;
        }

        if (current is not JsonObject effective)
        {
            return null;
        }

        if (!effective.TryGetPropertyValue("properties", out var propertiesNode))
        {
            return null;
        }

        if (propertiesNode is not JsonObject properties)
        {
            return null;
        }

        if (!properties.TryGetPropertyValue("type", out var typeSchema))
        {
            return null;
        }

        if (typeSchema is not JsonObject typeObject)
        {
            return null;
        }

        if (!typeObject.TryGetPropertyValue("const", out var constNode))
        {
            return null;
        }

        return constNode is JsonValue constValue && constValue.TryGetValue<string>(out var pinned) ? pinned : null;
    }
}
