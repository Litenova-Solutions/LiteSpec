namespace LiteSpec.Model;

/// <summary>One Infrastructure class implementing a domain repository interface, through its declared base list.</summary>
sealed record CodeRepositoryImplementation(string Interface, string Class, string File, int Line);
