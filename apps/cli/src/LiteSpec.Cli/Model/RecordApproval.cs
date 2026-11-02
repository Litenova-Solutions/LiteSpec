namespace LiteSpec.Model;

/// <summary>One record's approval from the project approval table: who approves it and the rule that names them.</summary>
/// <param name="By">The approver the table names: one owner name, or the names for each risk class joined.</param>
/// <param name="Rule">The rule text the table gives for the record, one clause per risk class.</param>
sealed record RecordApproval(string By, string Rule);
